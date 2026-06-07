param(
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-OpenSslPath {
    $command = Get-Command openssl.exe -ErrorAction SilentlyContinue
    if ($command -and (Test-Path $command.Source)) {
        return $command.Source
    }

    foreach ($candidate in @(
        'C:\Program Files\Git\usr\bin\openssl.exe',
        'C:\Program Files\Git\mingw64\bin\openssl.exe',
        'C:\Program Files\OpenSSL-Win64\bin\openssl.exe'
    )) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw 'openssl.exe was not found. Install Git for Windows or OpenSSL and make sure openssl.exe is available.'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot '.local\rabbitmq-tls'
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$openssl = Get-OpenSslPath
$serverConfigPath = Join-Path $OutputDir 'server.cnf'
$rabbitConfigPath = Join-Path $OutputDir 'rabbitmq.conf'

@'
[req]
prompt = no
distinguished_name = req_distinguished_name
req_extensions = req_ext

[req_distinguished_name]
CN = norge360-rabbitmq

[req_ext]
subjectAltName = @alt_names

[alt_names]
DNS.1 = norge360-rabbitmq
DNS.2 = rabbitmq
DNS.3 = localhost
IP.1 = 127.0.0.1
'@ | Set-Content -Path $serverConfigPath -Encoding ascii

@'
loopback_users.guest = false
listeners.tcp = none
listeners.ssl.default = 5671
ssl_options.cacertfile = /etc/rabbitmq/tls/ca.crt
ssl_options.certfile = /etc/rabbitmq/tls/tls.crt
ssl_options.keyfile = /etc/rabbitmq/tls/tls.key
ssl_options.verify = verify_none
ssl_options.fail_if_no_peer_cert = false
management.tcp.port = 15672
'@ | Set-Content -Path $rabbitConfigPath -Encoding ascii

$caKeyPath = Join-Path $OutputDir 'ca.key'
$caCrtPath = Join-Path $OutputDir 'ca.crt'
$serverKeyPath = Join-Path $OutputDir 'tls.key'
$serverCsrPath = Join-Path $OutputDir 'server.csr'
$serverCrtPath = Join-Path $OutputDir 'tls.crt'

& $openssl genrsa -out $caKeyPath 4096 | Out-Null
& $openssl req `
    -x509 `
    -new `
    -nodes `
    -key $caKeyPath `
    -sha256 `
    -days 3650 `
    -out $caCrtPath `
    -subj '/CN=Norge360 RabbitMQ Local CA' | Out-Null

& $openssl genrsa -out $serverKeyPath 2048 | Out-Null
& $openssl req `
    -new `
    -key $serverKeyPath `
    -out $serverCsrPath `
    -config $serverConfigPath | Out-Null

& $openssl x509 `
    -req `
    -in $serverCsrPath `
    -CA $caCrtPath `
    -CAkey $caKeyPath `
    -CAcreateserial `
    -out $serverCrtPath `
    -days 825 `
    -sha256 `
    -extensions req_ext `
    -extfile $serverConfigPath | Out-Null

Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $OutputDir 'ca.srl')
Remove-Item -Force -ErrorAction SilentlyContinue $serverCsrPath
Remove-Item -Force -ErrorAction SilentlyContinue $serverConfigPath

Write-Host "RabbitMQ local TLS files created in $OutputDir"
Write-Host "Next: docker compose --env-file .env.secrets.local -f docker-compose.yml -f docker-compose.override.yml up -d --build"
