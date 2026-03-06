param(
    [Parameter(Mandatory=$true)]
    [string]$Token,

    [string]$HostUrl = "http://localhost:9000",
    [string]$ProjectKey = "Siftly"
)

$ErrorActionPreference = "Stop"

Write-Host "=== SonarQube Analysis with Coverage ===" -ForegroundColor Cyan

# 1. Clean previous results
Write-Host "`n[1/5] Cleaning previous test results..." -ForegroundColor Yellow
if (Test-Path "./TestResults") {
    Remove-Item -Recurse -Force "./TestResults"
}

# 2. SonarScanner Begin
Write-Host "`n[2/5] Starting SonarScanner..." -ForegroundColor Yellow
dotnet sonarscanner begin `
    /k:"$ProjectKey" `
    /d:sonar.host.url="$HostUrl" `
    /d:sonar.token="$Token" `
    /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"

if ($LASTEXITCODE -ne 0) {
    Write-Host "SonarScanner begin failed!" -ForegroundColor Red
    exit 1
}

# 3. Build
Write-Host "`n[3/5] Building solution..." -ForegroundColor Yellow
dotnet build --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# 4. Run tests with coverage
Write-Host "`n[4/5] Running tests with coverage..." -ForegroundColor Yellow
dotnet test --no-build `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

# 5. SonarScanner End
Write-Host "`n[5/5] Finishing SonarScanner analysis..." -ForegroundColor Yellow
dotnet sonarscanner end /d:sonar.token="$Token"

if ($LASTEXITCODE -ne 0) {
    Write-Host "SonarScanner end failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Analysis complete! Check results at $HostUrl/dashboard?id=$ProjectKey ===" -ForegroundColor Green
