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
$previousUrls = $env:ASPNETCORE_URLS
$previousNoOpen = $env:REVA_NO_OPEN
$env:ASPNETCORE_URLS = "http://localhost:$port"
$env:REVA_NO_OPEN = "1"
$process = Start-Process -FilePath $exePath -WorkingDirectory $extractRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput $outLog -RedirectStandardError $errLog
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

    # The single .exe must serve the bundled UI from the same origin as the API.
    $homePage = Invoke-WebRequest -Uri "http://localhost:$port/" -TimeoutSec 5 -UseBasicParsing
    if ($homePage.StatusCode -ne 200 -or $homePage.Content -notmatch "Reve Intelligence") {
        throw "Packaged Reva.exe did not serve the static UI at /."
    }

    # The native chat agent must be wired and degrade gracefully when no local model is present.
    $agentStatus = Invoke-WebRequest -Uri "http://localhost:$port/api/agent/status" -TimeoutSec 5 -UseBasicParsing
    if ($agentStatus.StatusCode -ne 200) {
        throw "Packaged Reva.exe did not expose /api/agent/status."
    }

    $agentBody = '{"id":"smoke","messages":[{"id":"m1","role":"user","parts":[{"type":"text","text":"hello"}]}]}'
    $agentResponse = Invoke-WebRequest -Uri "http://localhost:$port/api/agent" -Method Post -ContentType "application/json" -Body $agentBody -TimeoutSec 30 -UseBasicParsing
    if ($agentResponse.Headers["Content-Type"] -notmatch "text/event-stream" -or $agentResponse.Content -notmatch "data:") {
        throw "Packaged /api/agent did not return a UI-message-stream response."
    }

    Write-Host "PACKAGE_SMOKE_OK=http://localhost:$port"
} finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
    $env:ASPNETCORE_URLS = $previousUrls
    $env:REVA_NO_OPEN = $previousNoOpen
}
