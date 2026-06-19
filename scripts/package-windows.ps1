param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.1.0",
    [string]$OutputRoot = "artifacts/releases"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$webRoot = Join-Path $repoRoot "web"
$publishDir = Join-Path $repoRoot "artifacts/publish/$Runtime"
$releaseDir = Join-Path $repoRoot $OutputRoot
$zipPath = Join-Path $releaseDir "Reva-v$Version-$Runtime.zip"

Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir, $releaseDir | Out-Null

Push-Location $webRoot
try {
    pnpm install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) {
        throw "pnpm install failed with exit code $LASTEXITCODE"
    }

    pnpm build
    if ($LASTEXITCODE -ne 0) {
        throw "pnpm build failed with exit code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

dotnet publish (Join-Path $repoRoot "src/Reva.Web/Reva.Web.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $publishDir "Reva.exe"
if (-not (Test-Path $exePath)) {
    throw "Reva.exe was not produced in $publishDir"
}

$publishedWebRoot = Join-Path $publishDir "wwwroot"
Remove-Item $publishedWebRoot -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $webRoot "out") -Destination $publishedWebRoot -Recurse -Force

$launcher = @'
@echo off
cd /d "%~dp0"
start "" Reva.exe
'@
Set-Content -Path (Join-Path $publishDir "Start-Reva.cmd") -Value $launcher -Encoding ASCII

$readme = @"
Reva $Version for Windows

Quick start:
1. Extract this ZIP.
2. Double-click Reva.exe (or Start-Reva.cmd).
3. Reva opens in your browser with the packaged UI and local API.

Notes:
- This is a single self-contained executable. The .NET runtime, API, and static web UI are bundled. No installation is required.
- The assistant chat is optional and can use a local model or configured cloud provider. Without model access, document intake, deterministic extraction, review, reconciliation, and export still work.
- Python is optional. Reva parses TXT, Markdown, CSV, PDF, Office, and email without Python; installing Docling enables a richer optional parser path.
"@
Set-Content -Path (Join-Path $publishDir "README-RUN.txt") -Value $readme -Encoding UTF8

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Output "PACKAGE_PATH=$zipPath"
