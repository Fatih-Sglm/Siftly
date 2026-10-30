param(
    [string[]]$BenchmarkArgs = @()
)

$ErrorActionPreference = "Stop"

$ArtifactsDir = Join-Path $PSScriptRoot "BenchmarkDotNet.Artifacts"
$ReportPath = Join-Path $ArtifactsDir "results/Siftly.Benchmark.FilterBenchmark-report-github.md"
$ReadmePath = Join-Path $PSScriptRoot "README.md"
$StartMarker = "<!-- BENCHMARK:START -->"
$EndMarker = "<!-- BENCHMARK:END -->"

Write-Host "=== Siftly Benchmark -> README ===" -ForegroundColor Cyan

Write-Host "`n[1/2] Running benchmarks (Release)..." -ForegroundColor Yellow
if (Test-Path $ReportPath) {
    Remove-Item $ReportPath -Force
}

dotnet run -c Release --project (Join-Path $PSScriptRoot "Siftly.Benchmark") -- --artifacts $ArtifactsDir @BenchmarkArgs

if ($LASTEXITCODE -ne 0 -or -not (Test-Path $ReportPath)) {
    Write-Host "Benchmark run failed, README not changed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n[2/2] Updating README..." -ForegroundColor Yellow
$readme = [System.IO.File]::ReadAllText($ReadmePath)
$start = $readme.IndexOf($StartMarker)
$end = $readme.IndexOf($EndMarker)

if ($start -lt 0 -or $end -lt $start) {
    Write-Host "Markers $StartMarker / $EndMarker not found in README!" -ForegroundColor Red
    exit 1
}

$newLine = if ($readme.Contains("`r`n")) { "`r`n" } else { "`n" }
$report = [System.IO.File]::ReadAllText($ReportPath).Trim() -replace "\r?\n", $newLine
$before = $readme.Substring(0, $start + $StartMarker.Length)
$after = $readme.Substring($end)
[System.IO.File]::WriteAllText($ReadmePath, $before + $newLine + $report + $newLine + $after)

Write-Host "`n=== README benchmark section updated ===" -ForegroundColor Green
