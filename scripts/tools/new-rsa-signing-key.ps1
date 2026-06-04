param(
    [int]$Bits = 3072,
    [string]$OutputPath = (Join-Path $PWD 'auth-jwt-signing-key.pem'),
    [switch]$Clipboard
)

$ErrorActionPreference = 'Stop'

$opensslCandidates = @(
    'C:\Program Files\Git\usr\bin\openssl.exe',
    'C:\Program Files\Git\mingw64\bin\openssl.exe',
    'C:\Program Files\OpenSSL-Win64\bin\openssl.exe'
)

$openssl = $opensslCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $openssl) {
    throw 'openssl.exe was not found. Install Git for Windows or OpenSSL and make sure openssl.exe is on PATH.'
}

if (Test-Path $OutputPath) {
    Remove-Item -Force $OutputPath
}

$outputDir = Split-Path -Parent $OutputPath
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

& $openssl genpkey -algorithm RSA -pkeyopt "rsa_keygen_bits:$Bits" -out $OutputPath

$pem = Get-Content -Raw $OutputPath

if ($Clipboard) {
    Set-Clipboard -Value $pem
}

Write-Output $pem
