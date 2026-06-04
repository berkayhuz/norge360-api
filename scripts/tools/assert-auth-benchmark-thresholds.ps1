param(
    [string]$InputDir = (Join-Path $PSScriptRoot "..\..\BenchmarkDotNet.Artifacts\results")
)

$ErrorActionPreference = "Stop"

function Get-FirstMetricValue {
    param(
        [string]$ReportFile,
        [string]$MethodName,
        [ValidateSet("Mean","Allocated")]
        [string]$MetricName
    )

    $rows = Import-Csv -Path $ReportFile -Delimiter ";"
    $row = $rows | Where-Object { $_.Method -eq $MethodName } | Select-Object -First 1
    if (-not $row) {
        throw "Method '$MethodName' not found in '$ReportFile'."
    }

    $value = $row.$MetricName
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Metric '$MetricName' is empty for '$MethodName' in '$ReportFile'."
    }

    return [double]([regex]::Replace($value, '[^0-9.]', ''))
}

$resolvedInput = (Resolve-Path $InputDir).Path

$rules = @(
    @{
        File = "Norge360.Auth.Benchmarks.CookieOriginProtectionMiddlewareBenchmarks-report.csv"
        Method = "Allow_UnsafeCookieRequest_With_AllowedOrigin"
        MeanNs = 2000.0
        AllocatedB = 800.0
    },
    @{
        File = "Norge360.Auth.Benchmarks.CookieOriginProtectionMiddlewareBenchmarks-report.csv"
        Method = "Reject_UnsafeCookieRequest_With_DisallowedOrigin"
        MeanNs = 6000.0
        AllocatedB = 3000.0
    },
    @{
        File = "Norge360.Auth.Benchmarks.PermissionAuthorizationHandlerBenchmarks-report.csv"
        Method = "Allow_WithMatchingPermission"
        MeanNs = 1500.0
        AllocatedB = 800.0
    },
    @{
        File = "Norge360.Auth.Benchmarks.PermissionAuthorizationHandlerBenchmarks-report.csv"
        Method = "Allow_WithWildcardPermission"
        MeanNs = 1500.0
        AllocatedB = 800.0
    },
    @{
        File = "Norge360.Auth.Benchmarks.PermissionAuthorizationHandlerBenchmarks-report.csv"
        Method = "Deny_WithoutMatchingPermission"
        MeanNs = 1500.0
        AllocatedB = 800.0
    },
    @{
        File = "Norge360.Auth.Benchmarks.TrustedInternalSourceAuthorizationHandlerBenchmarks-report.csv"
        Method = "Allow_WithTrustedGatewaySource"
        MeanNs = 5000.0
        AllocatedB = 3000.0
    },
    @{
        File = "Norge360.Auth.Benchmarks.TrustedInternalSourceAuthorizationHandlerBenchmarks-report.csv"
        Method = "Deny_WithUntrustedGatewaySource"
        MeanNs = 5000.0
        AllocatedB = 3000.0
    }
)

foreach ($rule in $rules) {
    $reportFile = Join-Path $resolvedInput $rule.File
    if (-not (Test-Path $reportFile)) {
        throw "Benchmark report not found: $reportFile"
    }

    $meanNs = Get-FirstMetricValue -ReportFile $reportFile -MethodName $rule.Method -MetricName Mean
    $allocatedB = Get-FirstMetricValue -ReportFile $reportFile -MethodName $rule.Method -MetricName Allocated

    Write-Host "$($rule.Method): Mean(ns)=$meanNs Allocated(B)=$allocatedB"

    if ($meanNs -gt $rule.MeanNs) {
        throw "Benchmark '$($rule.Method)' exceeded mean threshold. Mean(ns)=$meanNs Threshold(ns)=$($rule.MeanNs)"
    }

    if ($allocatedB -gt $rule.AllocatedB) {
        throw "Benchmark '$($rule.Method)' exceeded allocation threshold. Allocated(B)=$allocatedB Threshold(B)=$($rule.AllocatedB)"
    }
}

Write-Host "Auth benchmark thresholds passed."
