import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import process from 'node:process';
import { execFileSync } from 'node:child_process';

const namespace = process.env.K8S_NAMESPACE ?? 'norge360-production';
const namespaceManifest = path.resolve(process.cwd(), 'k8s/production/namespace.yaml');
const dryRun = process.argv.includes('--dry-run');
const kubectlTimeoutMs = 120_000;

function requireEnv(name, { optional = false, fallback = undefined } = {}) {
  const value = process.env[name] ?? fallback;
  if (!optional && (value === undefined || value === null || value === '')) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

function runKubectl(args, { input } = {}) {
  const startedAt = Date.now();
  console.log(`> kubectl ${args.join(' ')}`);

  try {
    return execFileSync('kubectl', args, {
      encoding: 'utf8',
      input,
      maxBuffer: 10 * 1024 * 1024,
      timeout: kubectlTimeoutMs,
    });
  } catch (error) {
    const elapsedMs = Date.now() - startedAt;
    console.error(`kubectl failed after ${elapsedMs}ms`);
    throw error;
  } finally {
    const elapsedMs = Date.now() - startedAt;
    console.log(`< kubectl completed in ${elapsedMs}ms`);
  }
}

function applyYaml(yaml) {
  if (dryRun) {
    console.log(yaml.trimEnd());
    console.log('---');
    return;
  }

  runKubectl(['apply', '-f', '-'], { input: yaml });
}

function createSecretYaml(args) {
  return runKubectl(args);
}

async function main() {
  await fs.access(namespaceManifest);
  console.log(`[1/4] Applying namespace manifest: ${namespaceManifest}`);
  if (!dryRun) {
    runKubectl(['apply', '--request-timeout=30s', '-f', namespaceManifest]);
  } else {
    console.log(`# would apply namespace: ${namespaceManifest}`);
  }

  console.log('[2/4] Syncing production secrets');
  const productionSecrets = [
    ['identity-connection', requireEnv('IDENTITY_CONNECTION')],
    ['accounts-connection', requireEnv('ACCOUNTS_CONNECTION')],
    ['community-connection', requireEnv('COMMUNITY_CONNECTION')],
    ['discovery-connection', requireEnv('DISCOVERY_CONNECTION')],
    ['notification-connection', requireEnv('NOTIFICATION_CONNECTION')],
    ['messaging-rabbitmq-uri', requireEnv('MESSAGING_RABBITMQ_URI')],
    ['redis-connection', requireEnv('REDIS_CONNECTION', { fallback: 'norge360-redis:6379,abortConnect=false' })],
    ['discovery-internal-token', requireEnv('DISCOVERY_INTERNAL_TOKEN')],
    ['gateway-trusted-secret', requireEnv('GATEWAY_TRUSTED_SECRET')],
    ['account-internal-secret', requireEnv('ACCOUNT_INTERNAL_SECRET')],
    ['turnstile-secret', requireEnv('TURNSTILE_SECRET')],
    ['meilisearch-api-key', requireEnv('MEILISEARCH_API_KEY')],
    ['notification-rabbitmq-username', requireEnv('NOTIFICATION_RABBITMQ_USERNAME')],
    ['notification-rabbitmq-password', requireEnv('NOTIFICATION_RABBITMQ_PASSWORD')],
    ['notification-smtp-host', requireEnv('NOTIFICATION_SMTP_HOST', { fallback: 'email-smtp.eu-west-1.amazonaws.com' })],
    ['notification-smtp-port', requireEnv('NOTIFICATION_SMTP_PORT', { fallback: '587' })],
    ['notification-smtp-username', requireEnv('NOTIFICATION_SMTP_USERNAME')],
    ['notification-smtp-password', requireEnv('NOTIFICATION_SMTP_PASSWORD')],
    ['notification-smtp-from-address', requireEnv('NOTIFICATION_SMTP_FROM_ADDRESS', { fallback: 'notifications@norge360.com' })],
    ['notification-smtp-from-name', requireEnv('NOTIFICATION_SMTP_FROM_NAME', { fallback: 'Norge360 Notifications' })],
    ['r2-account-id', requireEnv('R2_ACCOUNT_ID')],
    ['r2-bucket-name', requireEnv('R2_BUCKET_NAME', { fallback: 'norge360-cdn' })],
    ['r2-access-key-id', requireEnv('R2_ACCESS_KEY_ID')],
    ['r2-secret-access-key', requireEnv('R2_SECRET_ACCESS_KEY')],
  ];

  const secretArgs = [
    'create',
    'secret',
    'generic',
    'norge360-production-secrets',
    '-n',
    namespace,
  ];

  for (const [key, value] of productionSecrets) {
    secretArgs.push('--from-literal', `${key}=${value}`);
  }

  secretArgs.push('--dry-run=client', '-o', 'yaml');
  const productionSecretYaml = createSecretYaml(secretArgs);
  applyYaml(productionSecretYaml);

  console.log('[3/4] Syncing auth signing key secret');
  const authSigningKey = requireEnv('AUTH_JWT_SIGNING_PRIVATE_KEY_PEM');
  const authKeyPath = path.join(os.tmpdir(), `norge360-auth-jwt-${Date.now()}.pem`);
  await fs.writeFile(authKeyPath, authSigningKey, 'utf8');

  try {
    const authKeyArgs = [
      'create',
      'secret',
      'generic',
      'norge360-auth-signing-key',
      '-n',
      namespace,
      '--from-file',
      `auth-jwt-signing.pem=${authKeyPath}`,
      '--dry-run=client',
      '-o',
      'yaml',
    ];

    const authSecretYaml = createSecretYaml(authKeyArgs);
    applyYaml(authSecretYaml);
  } finally {
    await fs.rm(authKeyPath, { force: true });
  }

  const ghcrUser = requireEnv('GHCR_READ_USER');
  const ghcrToken = requireEnv('GHCR_READ_TOKEN');

  console.log('[4/4] Syncing GHCR pull secret');
  const ghcrArgs = [
    'create',
    'secret',
    'docker-registry',
    'ghcr-pull-secret',
    '-n',
    namespace,
    '--docker-server=ghcr.io',
    `--docker-username=${ghcrUser}`,
    `--docker-password=${ghcrToken}`,
    '--dry-run=client',
    '-o',
    'yaml',
  ];

  const ghcrSecretYaml = createSecretYaml(ghcrArgs);
  applyYaml(ghcrSecretYaml);

  console.log('All Kubernetes secrets synced successfully.');
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
});
