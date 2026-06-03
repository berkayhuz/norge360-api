import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { cwd, exit, platform } from 'node:process';

const DEFAULT_IMAGE = 'ghcr.io/gitleaks/gitleaks:v8.30.1';
const image = process.env.GITLEAKS_IMAGE ?? DEFAULT_IMAGE;
const baselinePath = process.env.GITLEAKS_BASELINE_PATH ?? '.gitleaks.baseline.json';
const trackedPathspecs = [
  'docker-compose.yml',
  'docker-compose.prod.yml',
  '.env.example',
  '.github',
  'scripts',
  'docs',
  'eng',
  'Directory.Build.props',
  'Directory.Build.targets',
  'Directory.Packages.props',
  'global.json',
  'Norge360.slnx',
  'nuget.temp.config',
  ':(glob)src/**/appsettings*.json',
  ':(glob)src/**/launchSettings.json',
  ':(glob)src/**/Dockerfile*',
];

const fallbackDirTargets = [
  'docker-compose.yml',
  'docker-compose.prod.yml',
  '.env.example',
  '.github',
  'scripts',
  'docs',
  'eng',
  'Directory.Build.props',
  'Directory.Build.targets',
  'Directory.Packages.props',
  'global.json',
  'Norge360.slnx',
  'nuget.temp.config',
];

const [mode = 'dir', ...scanArgs] = process.argv.slice(2);

if (!['dir', 'git', 'stdin'].includes(mode)) {
  console.error(`Unsupported gitleaks mode: ${mode}`);
  exit(2);
}

const resolvedScanArgs =
  scanArgs.length > 0
    ? scanArgs
    : mode === 'dir'
      ? (() => {
          const tracked = spawnSync('git', ['ls-files', '-z', '--', ...trackedPathspecs], {
            encoding: 'buffer',
          });

          if (tracked.status === 0 && tracked.stdout && tracked.stdout.length > 0) {
            return tracked.stdout
              .toString('utf8')
              .split('\0')
              .filter(Boolean);
          }

          return fallbackDirTargets;
        })()
      : ['.'];

const mountPath = platform === 'win32' ? cwd().replaceAll('\\', '/') : cwd();
const baselineArgs = existsSync(baselinePath) ? ['--baseline-path', baselinePath] : [];
const dockerArgs = [
  'run',
  '--rm',
  '-v',
  `${mountPath}:/repo`,
  '-w',
  '/repo',
  image,
  mode,
  '--no-banner',
  ...baselineArgs,
  ...resolvedScanArgs,
];

const result = spawnSync('docker', dockerArgs, {
  stdio: 'inherit',
  windowsHide: true,
});

if (result.error) {
  console.error(`Failed to start docker: ${result.error.message}`);
  exit(1);
}

exit(result.status ?? 1);
