param(
    [int]$Port = 8765,
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'

$dashboardRoot = $PSScriptRoot
$serverScript = Join-Path $dashboardRoot 'art_dashboard_server.py'
$bundledPython = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'

$python = $null
foreach ($candidate in @('python.exe', 'python', $bundledPython)) {
    try {
        if ($candidate -eq $bundledPython -and -not (Test-Path -LiteralPath $candidate)) {
            continue
        }
        $version = & $candidate --version 2>$null
        if ($LASTEXITCODE -eq 0 -or $version) {
            $python = $candidate
            break
        }
    }
    catch {
    }
}

if (-not $python) {
    throw 'Python was not found. Install Python 3, or run this from Codex where the bundled Python runtime is available.'
}

$arguments = @($serverScript, '--port', $Port)
if ($NoOpen) {
    $arguments += '--no-open'
}

Write-Host "Starting DungeonFit Art Dashboard..."
Write-Host "Python: $python"
Write-Host "URL:    http://127.0.0.1:$Port/"
& $python @arguments
