import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const root = process.cwd();
const requireRuntimeSecrets =
  process.argv.includes('--require-runtime-secrets') ||
  process.env.PREFLIGHT_REQUIRE_RUNTIME_SECRETS === '1';

const failures = [];
const warnings = [];

function fail(message) {
  failures.push(message);
}

function warn(message) {
  warnings.push(message);
}

function read(relativePath) {
  return fs.readFileSync(path.join(root, relativePath), 'utf8');
}

function exists(relativePath) {
  return fs.existsSync(path.join(root, relativePath));
}

function splitYamlDocuments(text) {
  return text
    .split(/^---\s*$/m)
    .map(document => document.trim())
    .filter(Boolean);
}

function scalarAfter(document, key) {
  const match = document.match(new RegExp(`^\\s*${key}:\\s*(.+?)\\s*$`, 'm'));
  return match?.[1]?.replace(/^["']|["']$/g, '') ?? null;
}

function metadataName(document) {
  const lines = document.split(/\r?\n/);
  const metadataIndex = lines.findIndex(line => /^metadata:\s*$/.test(line));
  if (metadataIndex === -1) {
    return scalarAfter(document, 'name');
  }

  for (let index = metadataIndex + 1; index < lines.length; index += 1) {
    const line = lines[index];
    if (/^\S/.test(line)) {
      break;
    }

    const match = line.match(/^\s+name:\s*(.+?)\s*$/);
    if (match) {
      return match[1].replace(/^["']|["']$/g, '');
    }
  }

  return null;
}

function parseServices() {
  const services = new Map();
  const files = [
    'k8s/production/services.yaml',
    'k8s/production/statefulsets.yaml',
    'k8s/production/redis.yaml',
  ].filter(exists);

  for (const file of files) {
    for (const document of splitYamlDocuments(read(file))) {
      if (scalarAfter(document, 'kind') !== 'Service') {
        continue;
      }

      const name = metadataName(document);
      if (!name) {
        fail(`${file}: Service document is missing metadata.name.`);
        continue;
      }

      const ports = [...document.matchAll(/^\s*port:\s*(\d+)\s*$/gm)].map(match => Number(match[1]));
      services.set(name, { file, ports });
    }
  }

  return services;
}

function parseDeployments() {
  return splitYamlDocuments(read('k8s/production/deployments.yaml'))
    .filter(document => scalarAfter(document, 'kind') === 'Deployment')
    .map(document => {
      const name = metadataName(document);
      const image = document.match(/^\s*image:\s*(\S+)\s*$/m)?.[1] ?? null;
      const container = document.match(/^\s*-\s*name:\s*([a-z0-9-]+)\s*$/m)?.[1] ?? null;
      const env = [...document.matchAll(/^\s*-\s*name:\s*([A-Za-z0-9_]+)\s*\n(?:(?:\s+value:\s*(.+?)\s*$)|(?:\s+valueFrom:\s*$))/gm)]
        .map(match => ({ name: match[1], value: match[2]?.replace(/^["']|["']$/g, '') ?? null }));

      return {
        name,
        image,
        container,
        document,
        env,
        runAsNonRoot: /^\s*runAsNonRoot:\s*true\s*$/m.test(document),
      };
    });
}

function hostFromUrl(value, settingName) {
  try {
    return new URL(value).hostname;
  } catch {
    fail(`${settingName} must be a valid absolute URL: ${value}`);
    return null;
  }
}

function validateInternalUrl(value, settingName, services) {
  const host = hostFromUrl(value, settingName);
  if (!host) {
    return;
  }

  if (host.startsWith('norge360-') && !services.has(host)) {
    fail(`${settingName} references Kubernetes service '${host}', but no production Service with that name exists.`);
  }
}

function parseAmqpUri(value, settingName) {
  let uri;
  try {
    uri = new URL(value);
  } catch {
    fail(`${settingName} must be a valid amqp/amqps URI.`);
    return null;
  }

  if (!['amqp:', 'amqps:'].includes(uri.protocol)) {
    fail(`${settingName} must use amqp:// or amqps://.`);
  }

  if (!uri.username || !uri.password) {
    fail(`${settingName} must include broker credentials.`);
  }

  return uri;
}

function validateRabbitMqUri(value, settingName, services) {
  const uri = parseAmqpUri(value, settingName);
  if (!uri) {
    return;
  }

  const host = uri.hostname;
  const service = services.get(host);
  const isInternalService = Boolean(service);

  if (uri.protocol === 'amqp:') {
    fail(`${settingName} must use TLS amqps:// in production.`);
  }

  if (uri.protocol === 'amqps:' && isInternalService && !service.ports.includes(5671)) {
    fail(`${settingName} points to service '${host}', but that Service does not expose TLS port 5671.`);
  }

  if (uri.protocol === 'amqps:' && uri.port && uri.port !== '5671') {
    fail(`${settingName} must use port 5671 for amqps production RabbitMQ connections.`);
  }

  if (host === 'rabbitmq') {
    fail(`${settingName} points at 'rabbitmq'; production Service is 'norge360-rabbitmq'.`);
  }
}

function validateRedisConnection(value, settingName, services) {
  const host = value.split(',')[0]?.split(':')[0]?.trim();
  if (!host) {
    fail(`${settingName} must include a Redis host.`);
    return;
  }

  if (host.startsWith('norge360-') && !services.has(host)) {
    fail(`${settingName} references Kubernetes service '${host}', but no production Service with that name exists.`);
  }
}

function parseConnectionString(value) {
  return new Map(
    value
      .split(';')
      .map(part => part.trim())
      .filter(Boolean)
      .map(part => {
        const separator = part.indexOf('=');
        if (separator === -1) {
          return [part.toLowerCase(), ''];
        }

        return [
          part.slice(0, separator).trim().toLowerCase(),
          part.slice(separator + 1).trim(),
        ];
      }),
  );
}

function validatePostgresConnection(value, settingName) {
  const parts = parseConnectionString(value);
  const host = parts.get('host') ?? parts.get('server');
  const database = parts.get('database');
  const username = parts.get('username') ?? parts.get('user id') ?? parts.get('userid');
  const password = parts.get('password');
  const sslMode = parts.get('ssl mode') ?? parts.get('sslmode');

  if (!host) {
    fail(`${settingName} must include Host.`);
    return;
  }

  if (['localhost', '127.0.0.1', 'host.docker.internal'].includes(host.toLowerCase())) {
    fail(`${settingName} points at '${host}', which is not reachable from production Kubernetes pods.`);
  }

  if (!database) {
    fail(`${settingName} must include Database.`);
  }

  if (!username) {
    fail(`${settingName} must include Username.`);
  }

  if (!password) {
    fail(`${settingName} must include Password.`);
  } else if (settingName === 'ConnectionStrings__NotificationConnection' && (password.length < 16 || containsUnsafeMarker(password))) {
    fail(`${settingName} must use a strong production database password.`);
  } else if (password.length < 16 || containsUnsafeMarker(password)) {
    warn(`${settingName}: database password is accepted by the current service but should be rotated to at least 16 characters.`);
  }

  if (!sslMode || !['require', 'verifyfull', 'prefer'].includes(sslMode.toLowerCase())) {
    fail(`${settingName} must set SSL Mode=Require, VerifyFull, or Prefer.`);
  }
}

function containsUnsafeMarker(value) {
  const normalized = value.toLowerCase();
  return ['localhost', '127.0.0.1', '::1', 'change_me', 'replace', 'local', 'dev', 'test']
    .some(marker => normalized.includes(marker));
}

function validateProductionEnvironment(deployments) {
  for (const deployment of deployments) {
    for (const key of ['ASPNETCORE_ENVIRONMENT', 'DOTNET_ENVIRONMENT']) {
      const entry = deployment.env.find(item => item.name === key);
      if (entry && entry.value !== 'Production') {
        fail(`${deployment.name}: ${key} must be Production in k8s/production, found '${entry.value}'.`);
      }
    }
  }
}

function validateAppSettings() {
  const appSettings = [
    'src/services/auth/src/Norge360.Auth.API/appsettings.json',
    'src/services/accounts/src/Norge360.Accounts.API/appsettings.json',
    'src/services/community/src/Norge360.Community.API/appsettings.json',
    'src/services/discovery/src/Norge360.Discovery.API/appsettings.json',
    'src/services/search/src/Norge360.Search.API/appsettings.json',
    'src/platform/gateway/src/Norge360.ApiGateway/appsettings.json',
  ].filter(exists);

  for (const file of appSettings) {
    const text = read(file);
    if (/"Kestrel"\s*:\s*{[\s\S]*?"Https"\s*:/m.test(text)) {
      fail(`${file}: base appsettings.json must not define a Kestrel HTTPS endpoint; put local endpoints in appsettings.Development.json.`);
    }
  }
}

function validateDockerUsers(deployments) {
  const dockerfilesByImage = new Map([
    ['norge360-auth-api', 'src/services/auth/deploy/Norge360.Auth.API.Dockerfile'],
    ['norge360-api-gateway', 'src/platform/gateway/deploy/Norge360.ApiGateway.Dockerfile'],
    ['norge360-accounts-api', 'src/services/accounts/deploy/Norge360.Accounts.API.Dockerfile'],
    ['norge360-accounts-worker', 'src/services/accounts/deploy/Norge360.Accounts.Worker.Dockerfile'],
    ['norge360-community-api', 'src/services/community/deploy/Norge360.Community.API.Dockerfile'],
    ['norge360-community-worker', 'src/services/community/deploy/Norge360.Community.Worker.Dockerfile'],
    ['norge360-discovery-api', 'src/services/discovery/deploy/Norge360.Discovery.API.Dockerfile'],
    ['norge360-discovery-worker', 'src/services/discovery/deploy/Norge360.Discovery.Worker.Dockerfile'],
    ['norge360-messaging-api', 'src/services/messaging/deploy/Norge360.MessagingService.API.Dockerfile'],
    ['norge360-search-api', 'src/services/search/deploy/Norge360.Search.API.Dockerfile'],
    ['norge360-search-worker', 'src/services/search/deploy/Norge360.Search.Worker.Dockerfile'],
    ['norge360-notification-worker', 'src/services/notification/deploy/Norge360.Notification.Worker.Dockerfile'],
  ]);

  for (const deployment of deployments) {
    if (!deployment.image?.startsWith('ghcr.io/')) {
      continue;
    }

    const imageName = deployment.image.split('/').at(-1)?.split(':')[0];
    const dockerfile = dockerfilesByImage.get(imageName);
    if (!dockerfile) {
      warn(`${deployment.name}: no Dockerfile mapping is registered for image '${imageName}'.`);
      continue;
    }

    if (!exists(dockerfile)) {
      fail(`${deployment.name}: mapped Dockerfile does not exist: ${dockerfile}.`);
      continue;
    }

    const text = read(dockerfile);
    if (deployment.runAsNonRoot && !/^\s*USER\s+\$?APP_UID\s*$/m.test(text) && !/^\s*USER\s+[1-9][0-9]*\s*$/m.test(text)) {
      fail(`${deployment.name}: pod sets runAsNonRoot=true, but ${dockerfile} does not set a non-root USER.`);
    }
  }
}

function validateManifestReferences(deployments, services) {
  function probeBlock(document, probeName) {
    const lines = document.split(/\r?\n/);
    const start = lines.findIndex(line => line.trim() === `${probeName}:`);
    if (start === -1) {
      return '';
    }

    const indent = lines[start].match(/^\s*/)[0].length;
    const block = [lines[start]];
    for (let index = start + 1; index < lines.length; index += 1) {
      const line = lines[index];
      if (line.trim() && line.match(/^\s*/)[0].length <= indent) {
        break;
      }

      block.push(line);
    }

    return block.join('\n');
  }

  for (const deployment of deployments) {
    const allowedHosts = deployment.env.find(entry => entry.name === 'AllowedHosts')?.value
      ?.split(';')
      .map(host => host.trim().toLowerCase())
      .filter(Boolean);

    if (allowedHosts && !allowedHosts.includes('*')) {
      for (const probeName of ['startupProbe', 'livenessProbe', 'readinessProbe']) {
        const probeText = probeBlock(deployment.document, probeName);
        const hostHeader = probeText.match(/name:\s*Host\s*\n\s*value:\s*(.+?)\s*$/m)?.[1]?.replace(/^["']|["']$/g, '').toLowerCase();

        if (probeText.includes('httpGet:') && (!hostHeader || !allowedHosts.includes(hostHeader))) {
          fail(`${deployment.name}: ${probeName} must send a Host header allowed by AllowedHosts (${allowedHosts.join(';')}).`);
        }
      }
    }

    for (const entry of deployment.env) {
      if (!entry.value) {
        continue;
      }

      if (/^https?:\/\//i.test(entry.value)) {
        validateInternalUrl(entry.value, `${deployment.name}:${entry.name}`, services);
      }
    }

    if (!deployment.document.includes('/health/live') && deployment.document.includes('containerPort: 8080')) {
      warn(`${deployment.name}: exposes port 8080 but has no /health/live startup/liveness probe.`);
    }
  }
}

function validateMediaConfiguration(deployments) {
  for (const deployment of deployments) {
    const hasMediaStorage = deployment.env.some(entry => entry.name.startsWith('Media__CloudflareR2__'));
    if (!hasMediaStorage) {
      continue;
    }

    const scannerProvider = deployment.env.find(entry => entry.name === 'Media__SecurityScannerProvider')?.value;
    const requireScanner = deployment.env.find(entry => entry.name === 'Media__RequireSecurityScannerInProduction')?.value;

    if (!scannerProvider) {
      fail(`${deployment.name}: Media__SecurityScannerProvider must be explicit in production.`);
    }

    if (!requireScanner) {
      fail(`${deployment.name}: Media__RequireSecurityScannerInProduction must be explicit in production.`);
    }

    if (requireScanner?.toLowerCase() === 'true' && scannerProvider?.toLowerCase() === 'noop') {
      fail(`${deployment.name}: Media__SecurityScannerProvider cannot be Noop when Media__RequireSecurityScannerInProduction=true.`);
    }
  }
}

function validateJwtMetadataConfiguration(deployments) {
  for (const deployment of deployments) {
    for (const prefix of ['Authentication__Jwt', 'Authentication__JwtBearer']) {
      const metadataAddress = deployment.env.find(entry => entry.name === `${prefix}__MetadataAddress`)?.value;
      const authority = deployment.env.find(entry => entry.name === `${prefix}__Authority`)?.value;
      const requireHttpsMetadata = deployment.env.find(entry => entry.name === `${prefix}__RequireHttpsMetadata`)?.value;
      const usesInternalHttpMetadata =
        metadataAddress?.startsWith('http://') ||
        authority?.startsWith('http://');

      if (usesInternalHttpMetadata && requireHttpsMetadata?.toLowerCase() !== 'false') {
        fail(`${deployment.name}: ${prefix} uses internal HTTP metadata, so ${prefix}__RequireHttpsMetadata must be false.`);
      }
    }
  }
}

function validateAwsParameterStoreConfiguration(deployments) {
  for (const deployment of deployments) {
    const enabled = deployment.env.find(entry => entry.name === 'Infrastructure__AwsParameterStore__Enabled')?.value;
    const requireInProduction = deployment.env.find(entry => entry.name === 'Infrastructure__AwsParameterStore__RequireInProduction')?.value;

    if (enabled?.toLowerCase() === 'false' && requireInProduction?.toLowerCase() !== 'false') {
      fail(`${deployment.name}: Infrastructure__AwsParameterStore__RequireInProduction must be false when AWS Parameter Store is disabled in production.`);
    }
  }
}

function validateStatefulSetProbes() {
  const text = exists('k8s/production/statefulsets.yaml')
    ? read('k8s/production/statefulsets.yaml')
    : '';
  const rabbitDocument = splitYamlDocuments(text)
    .find(document => scalarAfter(document, 'kind') === 'StatefulSet' && metadataName(document) === 'norge360-rabbitmq');

  if (!rabbitDocument) {
    fail('k8s/production/statefulsets.yaml: norge360-rabbitmq StatefulSet is missing.');
    return;
  }

  const startupProbe = rabbitDocument.match(/startupProbe:[\s\S]*?(?=\n\s{10}livenessProbe:)/)?.[0] ?? '';
  const timeout = Number(startupProbe.match(/timeoutSeconds:\s*(\d+)/)?.[1] ?? 0);
  if (timeout < 10) {
    fail('norge360-rabbitmq: startupProbe timeoutSeconds must be at least 10; rabbitmq-diagnostics often exceeds Kubernetes default 1s during boot.');
  }
}

function validateRabbitMqTlsConfiguration(services, deployments) {
  const service = services.get('norge360-rabbitmq');
  if (!service) {
    fail('norge360-rabbitmq Service is missing.');
  } else {
    if (!service.ports.includes(5671)) {
      fail('norge360-rabbitmq Service must expose TLS port 5671.');
    }

    if (service.ports.includes(5672)) {
      fail('norge360-rabbitmq Service must not expose non-TLS port 5672 in production.');
    }
  }

  const statefulSets = exists('k8s/production/statefulsets.yaml')
    ? read('k8s/production/statefulsets.yaml')
    : '';
  const rabbitDocument = splitYamlDocuments(statefulSets)
    .find(document => scalarAfter(document, 'kind') === 'StatefulSet' && metadataName(document) === 'norge360-rabbitmq');

  if (rabbitDocument) {
    if (!rabbitDocument.includes('containerPort: 5671')) {
      fail('norge360-rabbitmq StatefulSet must expose containerPort 5671.');
    }

    if (rabbitDocument.includes('containerPort: 5672')) {
      fail('norge360-rabbitmq StatefulSet must not expose non-TLS containerPort 5672 in production.');
    }

    if (!rabbitDocument.includes('secretName: norge360-rabbitmq-tls')) {
      fail('norge360-rabbitmq StatefulSet must mount the norge360-rabbitmq-tls secret.');
    }

    if (!statefulSets.includes('listeners.ssl.default = 5671')) {
      fail('norge360-rabbitmq config must enable the TLS listener on port 5671.');
    }
  }

  const rabbitMqConsumers = deployments.filter(deployment =>
    deployment.env.some(entry => entry.name === 'Messaging__RabbitMq__Uri'));

  for (const deployment of rabbitMqConsumers) {
    const caPath = deployment.env.find(entry => entry.name === 'Messaging__RabbitMq__CaCertificatePath')?.value;
    if (caPath !== '/etc/norge360/rabbitmq-ca/ca.crt') {
      fail(`${deployment.name}: Messaging__RabbitMq__CaCertificatePath must point to the mounted RabbitMQ CA certificate.`);
    }

    if (!deployment.document.includes('secretName: norge360-rabbitmq-tls')) {
      fail(`${deployment.name}: must mount the norge360-rabbitmq-tls secret for RabbitMQ TLS verification.`);
    }
  }

  const notificationWorker = deployments.find(deployment => deployment.name === 'norge360-notification-worker');
  if (notificationWorker) {
    const useTls = notificationWorker.env.find(entry => entry.name === 'Notification__RabbitMq__UseTls')?.value;
    const port = notificationWorker.env.find(entry => entry.name === 'Notification__RabbitMq__Port')?.value;
    const caPath = notificationWorker.env.find(entry => entry.name === 'Notification__RabbitMq__CaCertificatePath')?.value;

    if (useTls?.toLowerCase() !== 'true') {
      fail('norge360-notification-worker: Notification__RabbitMq__UseTls must be true.');
    }

    if (port !== '5671') {
      fail('norge360-notification-worker: Notification__RabbitMq__Port must be 5671.');
    }

    if (caPath !== '/etc/norge360/rabbitmq-ca/ca.crt') {
      fail('norge360-notification-worker: Notification__RabbitMq__CaCertificatePath must point to the mounted RabbitMQ CA certificate.');
    }
  }
}

function validateTrustedGatewayHealthBypass() {
  const middlewareFiles = [
    'src/services/accounts/src/Norge360.Accounts.API/Middlewares/TrustedGatewayMiddleware.cs',
    'src/services/community/src/Norge360.Community.API/Middlewares/TrustedGatewayMiddleware.cs',
    'src/services/discovery/src/Norge360.Discovery.API/Middlewares/TrustedGatewayMiddleware.cs',
  ].filter(exists);

  for (const file of middlewareFiles) {
    const text = read(file);
    if (!text.includes('StartsWithSegments("/health")')) {
      fail(`${file}: TrustedGatewayMiddleware must bypass /health so Kubernetes probes can work.`);
    }
  }
}

function validateRuntimeSecrets(services) {
  const rabbitMqUri = process.env.MESSAGING_RABBITMQ_URI ?? process.env.Messaging__RabbitMq__Uri;
  const redisConnection = process.env.REDIS_CONNECTION ?? process.env.Infrastructure__DistributedCache__RedisConnectionString;
  const postgresConnections = new Map([
    ['ConnectionStrings__IdentityConnection', process.env.ConnectionStrings__IdentityConnection ?? process.env.IDENTITY_CONNECTION],
    ['ConnectionStrings__AccountsConnection', process.env.ConnectionStrings__AccountsConnection ?? process.env.ACCOUNTS_CONNECTION],
    ['ConnectionStrings__CommunityConnection', process.env.ConnectionStrings__CommunityConnection ?? process.env.COMMUNITY_CONNECTION],
    ['ConnectionStrings__DiscoveryConnection', process.env.ConnectionStrings__DiscoveryConnection ?? process.env.DISCOVERY_CONNECTION],
    ['ConnectionStrings__MessagingConnection', process.env.ConnectionStrings__MessagingConnection ?? process.env.MESSAGING_CONNECTION],
    ['ConnectionStrings__NotificationConnection', process.env.ConnectionStrings__NotificationConnection ?? process.env.NOTIFICATION_CONNECTION],
  ]);

  if (rabbitMqUri) {
    validateRabbitMqUri(rabbitMqUri, 'MESSAGING_RABBITMQ_URI', services);
  } else if (requireRuntimeSecrets) {
    fail('MESSAGING_RABBITMQ_URI is required for strict production preflight.');
  }

  if (redisConnection) {
    validateRedisConnection(redisConnection, 'REDIS_CONNECTION', services);
  } else if (requireRuntimeSecrets) {
    fail('REDIS_CONNECTION is required for strict production preflight.');
  }

  for (const [name, value] of postgresConnections) {
    if (value) {
      validatePostgresConnection(value, name);
    } else if (requireRuntimeSecrets) {
      fail(`${name} is required for strict production preflight.`);
    }
  }
}

function main() {
  const services = parseServices();
  const deployments = parseDeployments();

  if (deployments.length === 0) {
    fail('No production deployments were found.');
  }

  validateProductionEnvironment(deployments);
  validateAppSettings();
  validateDockerUsers(deployments);
  validateManifestReferences(deployments, services);
  validateMediaConfiguration(deployments);
  validateJwtMetadataConfiguration(deployments);
  validateAwsParameterStoreConfiguration(deployments);
  validateStatefulSetProbes();
  validateRabbitMqTlsConfiguration(services, deployments);
  validateTrustedGatewayHealthBypass();
  validateRuntimeSecrets(services);

  for (const message of warnings) {
    console.warn(`WARN ${message}`);
  }

  if (failures.length > 0) {
    console.error('Production preflight failed:');
    for (const message of failures) {
      console.error(`- ${message}`);
    }

    process.exitCode = 1;
    return;
  }

  console.log('Production preflight passed.');
}

main();
