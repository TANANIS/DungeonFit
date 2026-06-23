param(
    [int]$Port = 8765
)

$ErrorActionPreference = 'Stop'

$dashboardRoot = $PSScriptRoot
$pidPath = Join-Path $dashboardRoot 'art_dashboard_server.pid'
$url = "http://127.0.0.1:$Port/api/shutdown"

function Stop-FromPidFile {
    if (-not (Test-Path -LiteralPath $pidPath)) {
        Write-Host "No dashboard server PID file found."
        return $false
    }

    $pidInfo = Get-Content -LiteralPath $pidPath -Raw | ConvertFrom-Json
    $serverPid = [int]$pidInfo.pid
    $process = Get-Process -Id $serverPid -ErrorAction SilentlyContinue
    if (-not $process) {
        Remove-Item -LiteralPath $pidPath -Force -ErrorAction SilentlyContinue
        Write-Host "Dashboard server PID file was stale and has been removed."
        return $false
    }

    if ($process.ProcessName -notlike 'python*') {
        throw "PID $serverPid is not a Python dashboard process; refusing to stop $($process.ProcessName)."
    }

    Stop-Process -Id $serverPid -Force
    Remove-Item -LiteralPath $pidPath -Force -ErrorAction SilentlyContinue
    Write-Host "Stopped DungeonFit Art Dashboard server by PID $serverPid."
    return $true
}

try {
    Invoke-RestMethod -Uri $url -Method Post -TimeoutSec 2 | Out-Null
    Start-Sleep -Milliseconds 500
    if (Test-Path -LiteralPath $pidPath) {
        Remove-Item -LiteralPath $pidPath -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Stopped DungeonFit Art Dashboard server on port $Port."
}
catch {
    if (-not (Stop-FromPidFile)) {
        Write-Host "DungeonFit Art Dashboard server is not running on port $Port."
    }
}
