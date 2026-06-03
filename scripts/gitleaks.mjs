import { spawnSync } from 'node:child_process';
import { cwd, exit, platform } from 'node:process';

const DEFAULT_IMAGE = 'ghcr.io/gitleaks/gitleaks:v8.30.1';
const image = process.env.GITLEAKS_IMAGE ?? DEFAULT_IMAGE;
const baselinePath = process.env.GITLEAKS_BASELINE_PATH ?? '.gitleaks.baseline.json';
const defaultDirTargets = [
  'docker-compose.yml',
  'docker-compose.prod.yml',
  '.github',
  'docs',
  'eng',
  'scripts',
  'src',
  '.env.example',
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
      ? defaultDirTargets
      : ['.'];

const mountPath = platform === 'win32' ? cwd().replaceAll('\\', '/') : cwd();
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
  '--baseline-path',
  baselinePath,
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
