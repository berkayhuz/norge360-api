param(
    [string]$InputDir = (Join-Path $PSScriptRoot "..\..\BenchmarkDotNet.Artifacts\results"),
    [double]$MaxMeanNs = 1000000.0,
    [double]$MaxAllocatedB = 16384.0
)

$ErrorActionPreference = "Stop"

$resolvedInput = (Resolve-Path $InputDir).Path
$reports = Get-ChildItem -Path $resolvedInput -Filter "*-report.csv" -File | Sort-Object Name
if (-not $reports -or $reports.Count -eq 0) {
    throw "No benchmark report CSV files found in '$resolvedInput'."
}

foreach ($report in $reports) {
    $rows = Import-Csv -Path $report.FullName -Delimiter ";"
    foreach ($row in $rows) {
        $meanNs = [double]([regex]::Replace($row.Mean, '[^0-9.]', ''))
        if ($meanNs -gt $MaxMeanNs) {
            throw "Benchmark '$($row.Method)' in '$($report.Name)' exceeded mean threshold. Mean(ns)=$meanNs Threshold(ns)=$MaxMeanNs"
        }

        $allocatedB = $null
        if (-not [string]::IsNullOrWhiteSpace($row.Allocated) -and $row.Allocated -ne "-") {
            $allocatedB = [double]([regex]::Replace($row.Allocated, '[^0-9.]', ''))
            if ($allocatedB -gt $MaxAllocatedB) {
                throw "Benchmark '$($row.Method)' in '$($report.Name)' exceeded allocation threshold. Allocated(B)=$allocatedB Threshold(B)=$MaxAllocatedB"
            }
        }

        $message = "$($report.Name) :: $($row.Method) :: Mean(ns)=$meanNs"
        if ($null -ne $allocatedB) {
            $message += " Allocated(B)=$allocatedB"
        }

        Write-Host $message
    }
}

Write-Host "Package benchmark thresholds passed."
