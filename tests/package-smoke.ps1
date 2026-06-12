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

$port = 5199
$outLog = Join-Path $extractRoot "smoke.out.log"
$errLog = Join-Path $extractRoot "smoke.err.log"
$process = Start-Process -FilePath $exePath -ArgumentList @("--urls", "http://localhost:$port") -WorkingDirectory $extractRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput $outLog -RedirectStandardError $errLog
try {
    $healthy = $false
    for ($i = 1; $i -le 45; $i++) {
        Start-Sleep -Milliseconds 1000
        if ($process.HasExited) {
            break
        }

        try {
            $health = Invoke-RestMethod -Uri "http://localhost:$port/health" -TimeoutSec 2
            $documents = Invoke-RestMethod -Uri "http://localhost:$port/api/documents/" -TimeoutSec 2
            if ($health.status -eq "ok" -and $health.service -eq "Reva" -and $documents.Count -eq 0) {
                $healthy = $true
                break
            }
        } catch {
        }
    }

    if (-not $healthy) {
        Write-Host "--- package stdout ---"
        Get-Content $outLog -Tail 120 -ErrorAction SilentlyContinue
        Write-Host "--- package stderr ---"
        Get-Content $errLog -Tail 120 -ErrorAction SilentlyContinue
        throw "Packaged Reva.exe did not pass HTTP smoke checks."
    }

    Write-Host "PACKAGE_SMOKE_OK=http://localhost:$port"
} finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}