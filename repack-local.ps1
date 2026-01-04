# repack-local.ps1
param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$OutputDir = "./nupkgs"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Siftly Local Repack & Cache Clear" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Eski paketleri klasörden sil
if (Test-Path $OutputDir) {
    Write-Host "🧹 Cleaning nupkgs folder..." -ForegroundColor Yellow
    Remove-Item "$OutputDir\*.nupkg" -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# 2. Paketleri oluştur
Write-Host "📦 Building and packing..." -ForegroundColor Green
dotnet pack -c Release -o $OutputDir /p:Version=$Version

# 3. Cache temizle (Aynı versiyonun yenilenmesi için şart)
Write-Host "✨ Clearing NuGet local cache..." -ForegroundColor Cyan
dotnet nuget locals all --clear

Write-Host ""
Write-Host "✅ Done! Packages are ready in $OutputDir" -ForegroundColor Green
Write-Host "🚀 Run 'dotnet restore --force-evaluate' in your test project." -ForegroundColor Yellow
