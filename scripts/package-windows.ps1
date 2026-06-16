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

# Build the static UI and stage it into wwwroot so the published .exe serves it from the
# same origin as the API. No Node runtime is needed at run time — only at package time.
$webDir = Join-Path $repoRoot "web"
$wwwroot = Join-Path $repoRoot "src/Reva.Web/wwwroot"
Push-Location $webDir
try {
    pnpm install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { throw "pnpm install failed with exit code $LASTEXITCODE" }
    $env:NEXT_PUBLIC_API_BASE_URL = ""
    pnpm run build
    if ($LASTEXITCODE -ne 0) { throw "next build failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}
Get-ChildItem -Path $wwwroot -Force | Where-Object { $_.Name -ne ".gitkeep" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $webDir "out\*") -Destination $wwwroot -Recurse -Force
if (-not (Test-Path (Join-Path $wwwroot "index.html"))) { throw "static UI was not staged into wwwroot" }

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
2. Double-click Reva.exe (or Start-Reva.cmd).
3. Reva opens http://localhost:5187 automatically. Upload your documents to begin.

Optional:
- The workspace starts empty and works only on the real documents you upload.
- Pass --seed-demo (or set REVA_SEED_DEMO=1) to load a few sample documents for evaluation.
- Use REVA_NO_OPEN=1 for headless smoke tests.

Notes:
- This is a single self-contained app: the .NET runtime, the web UI, and the OCR engine are all bundled. No Node.js or web server is required.
- The assistant chat is optional and runs a local model. Install Ollama (https://ollama.com) and run "ollama pull qwen3-vl:8b"; Reva starts it automatically when present. Without it, every other feature still works fully — extraction and reconciliation are deterministic.
- Python is optional. Reva parses TXT, Markdown, CSV, PDF, Office, and email without Python; installing Docling/Python enables a richer optional parser path.
"@
Set-Content -Path (Join-Path $publishDir "README-RUN.txt") -Value $readme -Encoding UTF8

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Output "PACKAGE_PATH=$zipPath"
