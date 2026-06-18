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
New-Item -ItemType Directory -Force -Path $publishDir, $releaseDir | Out-Null

dotnet publish (Join-Path $repoRoot "src/Reva.App/Reva.App.csproj") `
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
3. The Reva window will open.

Notes:
- This is a single self-contained executable. The .NET runtime and all libraries are bundled. No installation is required.
- The assistant chat is optional and runs a local model. Install Ollama (https://ollama.com) and run "ollama pull qwen3-vl:8b"; Reva uses it automatically when present. Without it, every other feature still works fully.
- Python is optional. Reva parses TXT, Markdown, CSV, PDF, Office, and email without Python; installing Docling enables a richer optional parser path.
"@
Set-Content -Path (Join-Path $publishDir "README-RUN.txt") -Value $readme -Encoding UTF8

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Output "PACKAGE_PATH=$zipPath"
