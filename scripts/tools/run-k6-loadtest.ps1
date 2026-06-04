param(
    [string]$BaseUrl = $env:K6_BASE_URL,
    [string]$TargetPath = $env:K6_TARGET_PATH,
    [int]$Vus = 5,
    [string]$Duration = "30s",
    [string]$Image = "grafana/k6:0.53.0"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    $BaseUrl = "http://localhost:8080"
}

if ([string]::IsNullOrWhiteSpace($TargetPath)) {
    $TargetPath = "/.well-known/openid-configuration"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$artifactsDir = Join-Path $repoRoot ".artifacts\k6"
$scriptPath = Join-Path $repoRoot "loadtests\k6\auth-openid.js"

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

Get-Content -Raw -Path $scriptPath | docker run --rm -i `
    -e BASE_URL=$BaseUrl `
    -e TARGET_PATH=$TargetPath `
    -e K6_VUS=$Vus `
    -e K6_DURATION=$Duration `
    -v "${artifactsDir}:/artifacts" `
    $Image `
    run --summary-export=/artifacts/summary.json -
