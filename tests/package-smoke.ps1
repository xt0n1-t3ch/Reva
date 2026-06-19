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

$dbPath = Join-Path $extractRoot "data/reva.db"
Remove-Item $dbPath -Force -ErrorAction SilentlyContinue

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$baseUrl = "http://127.0.0.1:$port"

$outLog = Join-Path $extractRoot "smoke.out.log"
$errLog = Join-Path $extractRoot "smoke.err.log"
$startInfo = [System.Diagnostics.ProcessStartInfo]::new($exePath)
$startInfo.WorkingDirectory = $extractRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.Environment["ASPNETCORE_URLS"] = $baseUrl
$startInfo.Environment["REVA_NO_OPEN"] = "1"
$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$null = $process.Start()
try {
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    $health = $null

    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            break
        }

        try {
            $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 2
            break
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if ($null -eq $health) {
        throw "Reva.exe did not respond on $baseUrl/health within 30 seconds."
    }

    if (-not (Test-Path $dbPath)) {
        throw "reva.db was not created at $dbPath within 30 seconds."
    }

    $documents = Invoke-RestMethod -Uri "$baseUrl/api/documents" -TimeoutSec 5
    if ($null -eq $documents) {
        throw "GET /api/documents returned an empty response."
    }

    $homeResponse = Invoke-WebRequest -Uri "$baseUrl/" -TimeoutSec 5
    if ($homeResponse.StatusCode -ne 200 -or $homeResponse.Content -notmatch "<html") {
        throw "Packaged UI did not return HTML from /."
    }

    Write-Host "PACKAGE_SMOKE_OK=$baseUrl, health=$($health.status), reva.db=$dbPath"
} finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $null = $process.WaitForExit(5000)
    }
    [System.IO.File]::WriteAllText($outLog, $stdoutTask.GetAwaiter().GetResult())
    [System.IO.File]::WriteAllText($errLog, $stderrTask.GetAwaiter().GetResult())
}
