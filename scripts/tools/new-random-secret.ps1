param(
    [ValidateRange(16, 128)]
    [int]$Bytes = 32,

    [ValidateRange(1, 20)]
    [int]$Count = 1,

    [switch]$UrlSafe
)

$ErrorActionPreference = "Stop"

function New-RandomSecret {
    param(
        [int]$Length,
        [switch]$MakeUrlSafe
    )

    $buffer = [byte[]]::new($Length)
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($buffer)
    }
    finally {
        $rng.Dispose()
    }

    $secret = [Convert]::ToBase64String($buffer)
    if ($MakeUrlSafe) {
        $secret = $secret.Replace("+", "-").Replace("/", "_").TrimEnd("=")
    }

    return $secret
}

for ($index = 1; $index -le $Count; $index++) {
    $secret = New-RandomSecret -Length $Bytes -MakeUrlSafe:$UrlSafe
    Write-Output $secret
}
