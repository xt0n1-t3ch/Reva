param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.1.0",
    [string]$OutputRoot = "artifacts/releases"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "artifacts/publish/$Runtime"
$releaseDir = Join-Path $repoRoot $OutputRoot
$zipPath = Join-Path $releaseDir "Reva-v$Version-$Runtime.zip"

Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir,$releaseDir | Out-Null

dotnet publish (Join-Path $repoRoot "src/Reva.Web/Reva.Web.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$launcher = @"
@echo off
cd /d "%~dp0"
set ASPNETCORE_URLS=http://localhost:5187
Reva.exe
"@
Set-Content -Path (Join-Path $publishDir "Start-Reva.cmd") -Value $launcher -Encoding ASCII

$readme = @"
Reva $Version for Windows

Quick start:
1. Extract this ZIP.
2. Double-click Reva.exe.
3. Reva opens http://localhost:5187 automatically.

Direct mode:
- Double-click Reva.exe, or set ASPNETCORE_URLS=http://localhost:5187 before running Reva.exe when a fixed port is needed.
- Start-Reva.cmd is included as a fallback launcher.
- Use REVA_NO_OPEN=1 for headless smoke tests.

Notes:
- .NET is bundled in this self-contained package.
- Python is optional. Reva can parse TXT, Markdown, CSV, and visible binary text without Python.
- Installing Docling/Python enables the optional advanced parser worker path for richer PDF/image parsing.
"@
Set-Content -Path (Join-Path $publishDir "README-RUN.txt") -Value $readme -Encoding UTF8

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Output "PACKAGE_PATH=$zipPath"
