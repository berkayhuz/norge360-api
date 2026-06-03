param(
    [string]$GatewayBaseUrl = $(if ($env:GATEWAY_BASE_URL) { $env:GATEWAY_BASE_URL } else { "http://localhost:5030" }),
    [string]$AuthToken = $env:AUTH_TOKEN,
    [string]$AuthEmail = $env:AUTH_EMAIL,
    [string]$AuthPassword = $env:AUTH_PASSWORD,
    [string]$SecondAuthToken = $env:SECOND_AUTH_TOKEN,
    [string]$SecondAuthEmail = $env:SECOND_AUTH_EMAIL,
    [string]$SecondAuthPassword = $env:SECOND_AUTH_PASSWORD,
    [string]$TestImagePath = $env:TEST_IMAGE_PATH,
    [switch]$RunMediaSmoke = [System.Convert]::ToBoolean($env:RUN_MEDIA_SMOKE),
    [switch]$RunSigningSmoke = [System.Convert]::ToBoolean($env:RUN_SIGNING_SMOKE)
)

$ErrorActionPreference = "Stop"

function Write-Result {
    param([string]$Name, [int]$StatusCode, [string]$Note)
    Write-Output "$Name|$StatusCode|$Note"
}

function Invoke-Smoke {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        [string]$Body,
        $WebSession
    )

    try {
        $args = @{ UseBasicParsing = $true; Method = $Method; Uri = $Url }
        if ($Headers) { $args.Headers = $Headers }
        if ($WebSession) { $args.WebSession = $WebSession }

        if (-not [string]::IsNullOrWhiteSpace($Body)) {
            $args.ContentType = "application/json"
            $args.Body = $Body
        }

        $response = Invoke-WebRequest @args
        Write-Result -Name $Name -StatusCode $response.StatusCode -Note "ok"
        return $response
    }
    catch {
        $status = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $status = [int]$_.Exception.Response.StatusCode
        }

        Write-Result -Name $Name -StatusCode $status -Note "failed"
        return $null
    }
}

function Invoke-SmokeForm {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        [hashtable]$Form,
        $WebSession
    )

    try {
        $args = @{ UseBasicParsing = $true; Method = $Method; Uri = $Url; Body = $Form }
        if ($Headers) { $args.Headers = $Headers }
        if ($WebSession) { $args.WebSession = $WebSession }
        $response = Invoke-WebRequest @args
        Write-Result -Name $Name -StatusCode $response.StatusCode -Note "ok"
        return $response
    }
    catch {
        $status = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $status = [int]$_.Exception.Response.StatusCode
        }

        Write-Result -Name $Name -StatusCode $status -Note "failed"
        return $null
    }
}

function New-LoginSession {
    param(
        [string]$Email,
        [string]$Password
    )

    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        return $null
    }

    try {
        $payload = @{ emailOrUserName = $Email; password = $Password; rememberMe = $false } | ConvertTo-Json
        $resp = Invoke-WebRequest -UseBasicParsing -Method POST -Uri "$GatewayBaseUrl/api/auth/login" -ContentType "application/json" -Body $payload -SessionVariable loginSession
        $json = $null
        try { $json = $resp.Content | ConvertFrom-Json } catch { }

        $token = $null
        if ($json -and $json.PSObject.Properties.Name -contains "accessToken") {
            $token = [string]$json.accessToken
        }

        return [PSCustomObject]@{
            Session = $loginSession
            AccessToken = $token
            HasCookie = ($loginSession.Cookies.Count -gt 0)
        }
    }
    catch {
        Write-Result -Name "auth.login" -StatusCode 0 -Note "failed"
        return $null
    }
}

$publicHeaders = @{}
Invoke-Smoke -Name "public.health" -Method "GET" -Url "$GatewayBaseUrl/api/community/health" -Headers $publicHeaders -Body $null -WebSession $null | Out-Null
Invoke-Smoke -Name "public.feed" -Method "GET" -Url "$GatewayBaseUrl/api/community/feed" -Headers $publicHeaders -Body $null -WebSession $null | Out-Null
Invoke-Smoke -Name "public.post.not-found" -Method "GET" -Url "$GatewayBaseUrl/api/community/posts/00000000-0000-0000-0000-000000000000" -Headers $publicHeaders -Body $null -WebSession $null | Out-Null
Invoke-SmokeForm -Name "unauth.post.create" -Method "POST" -Url "$GatewayBaseUrl/api/community/posts" -Headers $publicHeaders -Form @{ caption = "smoke" } -WebSession $null | Out-Null
Invoke-Smoke -Name "unauth.post.like" -Method "POST" -Url "$GatewayBaseUrl/api/community/posts/00000000-0000-0000-0000-000000000000/like" -Headers $publicHeaders -Body '{}' -WebSession $null | Out-Null

$authHeaders = @{}
$authSession = $null
$authMode = $null

if (-not [string]::IsNullOrWhiteSpace($AuthToken)) {
    $authHeaders.Authorization = "Bearer $AuthToken"
    $authMode = "provided-token"
}
else {
    $login = New-LoginSession -Email $AuthEmail -Password $AuthPassword
    if ($login) {
        $authSession = $login.Session
        if (-not [string]::IsNullOrWhiteSpace($login.AccessToken)) {
            $authHeaders.Authorization = "Bearer $($login.AccessToken)"
            $authMode = "login-token"
        }
        elseif ($login.HasCookie) {
            $authMode = "login-cookie"
        }
    }
}

if ([string]::IsNullOrWhiteSpace($authMode)) {
    Write-Result -Name "auth.flow" -StatusCode 0 -Note "skipped (AUTH_TOKEN or AUTH_EMAIL/AUTH_PASSWORD missing)"
}
else {
    Write-Result -Name "auth.transport" -StatusCode 0 -Note $authMode

    $createResponse = Invoke-SmokeForm -Name "auth.post.create" -Method "POST" -Url "$GatewayBaseUrl/api/community/posts" -Headers $authHeaders -Form @{ caption = "smoke auth" } -WebSession $authSession

    if ($createResponse -and $createResponse.Content) {
        try {
            $post = $createResponse.Content | ConvertFrom-Json
            if ($post.id) {
                $postId = $post.id
                Invoke-Smoke -Name "auth.post.details" -Method "GET" -Url "$GatewayBaseUrl/api/community/posts/$postId" -Headers $authHeaders -Body $null -WebSession $authSession | Out-Null
                Invoke-Smoke -Name "auth.post.like.add" -Method "POST" -Url "$GatewayBaseUrl/api/community/posts/$postId/like" -Headers $authHeaders -Body '{}' -WebSession $authSession | Out-Null
                Invoke-Smoke -Name "auth.post.like.remove" -Method "DELETE" -Url "$GatewayBaseUrl/api/community/posts/$postId/like" -Headers $authHeaders -Body $null -WebSession $authSession | Out-Null
                Invoke-Smoke -Name "auth.post.delete" -Method "DELETE" -Url "$GatewayBaseUrl/api/community/posts/$postId" -Headers $authHeaders -Body $null -WebSession $authSession | Out-Null
            }
        }
        catch {
            Write-Result -Name "auth.post.parse" -StatusCode 0 -Note "failed"
        }
    }
}

if ($RunMediaSmoke.IsPresent) {
    if ([string]::IsNullOrWhiteSpace($TestImagePath)) {
        Write-Result -Name "media.flow" -StatusCode 0 -Note "skipped (TEST_IMAGE_PATH missing)"
    }
    else {
        Write-Result -Name "media.flow" -StatusCode 0 -Note "manual multipart smoke required"
    }
}

if ($RunSigningSmoke.IsPresent) {
    Write-Result -Name "signing.flow" -StatusCode 0 -Note "run with signing-enabled compose profile"
}

if (-not [string]::IsNullOrWhiteSpace($SecondAuthToken)) {
    Write-Result -Name "auth.second.transport" -StatusCode 0 -Note "provided-token"
}
elseif (-not [string]::IsNullOrWhiteSpace($SecondAuthEmail) -and -not [string]::IsNullOrWhiteSpace($SecondAuthPassword)) {
    $secondLogin = New-LoginSession -Email $SecondAuthEmail -Password $SecondAuthPassword
    if ($secondLogin) {
        if (-not [string]::IsNullOrWhiteSpace($secondLogin.AccessToken)) {
            Write-Result -Name "auth.second.transport" -StatusCode 0 -Note "login-token"
        }
        elseif ($secondLogin.HasCookie) {
            Write-Result -Name "auth.second.transport" -StatusCode 0 -Note "login-cookie"
        }
    }
}
