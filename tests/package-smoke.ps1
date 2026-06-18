$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$version = "smoke"
$outputRoot = "artifacts/test-releases"
$packageOutput = & (Join-Path $repoRoot "scripts/package-windows.ps1") -Configuration Release -Version $version -OutputRoot $outputRoot
$packageOutput | ForEach-Object { Write-Host $_ }
$zipLine = $packageOutput | Where-Object { $_ -like "PACKAGE_PATH=*" } | Select-Object -Last 1
if (-not $zipLine) {
    throw "Package script did not report PACKAGE_PATH."
}
$zipPath = $zipLine.Substring("PACKAGE_PATH=".Length)
if (-not (Test-Path $zipPath)) {
    throw "Package ZIP was not created: $zipPath"
}

$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) "reva-package-smoke-$([Guid]::NewGuid().ToString('N'))"
Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force
$exePath = Join-Path $extractRoot "Reva.exe"
if (-not (Test-Path $exePath)) {
    throw "Reva.exe was not found in package."
}

$dbPath = Join-Path $env:LOCALAPPDATA "Reva\reva.db"
Remove-Item $dbPath -Force -ErrorAction SilentlyContinue

$outLog = Join-Path $extractRoot "smoke.out.log"
$errLog = Join-Path $extractRoot "smoke.err.log"
$process = Start-Process -FilePath $exePath -WorkingDirectory $extractRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput $outLog -RedirectStandardError $errLog
try {
    Start-Sleep -Seconds 5

    if ($process.HasExited) {
        Write-Host "--- stdout ---"
        Get-Content $outLog -ErrorAction SilentlyContinue
        Write-Host "--- stderr ---"
        Get-Content $errLog -ErrorAction SilentlyContinue
        throw "Reva.exe exited prematurely (exit code $($process.ExitCode))."
    }

    if (-not (Test-Path $dbPath)) {
        throw "reva.db was not created at $dbPath within 5 seconds."
    }

    Write-Host "PACKAGE_SMOKE_OK=process alive, reva.db created at $dbPath"
} finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
