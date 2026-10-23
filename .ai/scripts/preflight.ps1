# Preflight: check repo/Unity/MCP state and snapshot every local change.
# Safe: reads git, copies untracked small files, writes only under .ai/snapshots/.
[CmdletBinding()]
param(
    [string]$Root = 'D:\GitHub\Zephyr_v3',
    [string]$Label = 'manual'
)

$ErrorActionPreference = 'Continue'

if (-not (Test-Path -LiteralPath $Root)) { throw "Root not found: $Root" }
$Root = (Resolve-Path -LiteralPath $Root).Path

$Unity = Join-Path $Root 'Zephyr(AI)\Zephyr'
$Ai = Join-Path $Root '.ai'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$snap = Join-Path $Ai "snapshots\$stamp-$Label"
New-Item -ItemType Directory -Force -Path $snap | Out-Null

function Write-Step([string]$Message) {
    Write-Host "== $Message" -ForegroundColor Cyan
}

function Test-Port([int]$Port) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $ar = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        if ($ar.AsyncWaitHandle.WaitOne(800)) {
            $client.EndConnect($ar)
            return $true
        }
        return $false
    }
    catch { return $false }
    finally { $client.Close() }
}

Write-Step 'git state'
$branch = (& git -C $Root branch --show-current 2>$null)
$head = (& git -C $Root rev-parse --short HEAD 2>$null)
$status = @(& git -C $Root status --porcelain=v1 2>$null)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines((Join-Path $snap 'git-status.txt'), [string[]]$status, $utf8NoBom)

$diff = @(& git -C $Root diff --binary 2>$null)
[System.IO.File]::WriteAllLines((Join-Path $snap 'git-diff.patch'), [string[]]$diff, $utf8NoBom)

$cached = @(& git -C $Root diff --cached --binary 2>$null)
[System.IO.File]::WriteAllLines((Join-Path $snap 'git-diff-cached.patch'), [string[]]$cached, $utf8NoBom)

Write-Step 'copy untracked files'
$untracked = @()
foreach ($line in $status) {
    if ($line -match '^\?\?\s+(.+)$') {
        $untracked += $Matches[1].Trim('"')
    }
}

$copied = 0
$skipped = 0
foreach ($rel in $untracked) {
    if ($rel -match '(^|/)(Library|Temp|Logs|obj|\.git)/') { $skipped++; continue }
    if ($rel -match '^\.ai/snapshots/') { $skipped++; continue }
    $src = Join-Path $Root ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $src -PathType Leaf)) { $skipped++; continue }
    $item = Get-Item -LiteralPath $src
    if ($item.Length -gt 50MB) { Write-Warning "skip large untracked: $rel"; $skipped++; continue }
    $dst = Join-Path $snap ($rel -replace '/', '\')
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    Copy-Item -LiteralPath $src -Destination $dst -Force
    $copied++
}

Write-Step 'Unity project'
$versionFile = Join-Path $Unity 'ProjectSettings\ProjectVersion.txt'
$unityVersion = if (Test-Path -LiteralPath $versionFile) {
    (Get-Content -LiteralPath $versionFile | Select-Object -First 1)
} else {
    'MISSING'
}
$unityExists = Test-Path -LiteralPath $Unity

Write-Step 'MCP ports'
$unityMcp = Test-Port 8080
$unrealMcp = Test-Port 8000

$manifest = [ordered]@{
    timestamp       = (Get-Date).ToString('o')
    label           = $Label
    root            = $Root
    branch          = ($branch | Out-String).Trim()
    head            = ($head | Out-String).Trim()
    statusLines     = $status.Count
    untracked       = $untracked.Count
    copiedUntracked = $copied
    skippedUntracked = $skipped
    unityProject    = $Unity
    unityExists     = $unityExists
    unityVersion    = ($unityVersion | Out-String).Trim()
    unityMcp8080    = $unityMcp
    unrealMcp8000   = $unrealMcp
}
$manifest | ConvertTo-Json -Depth 4 | ForEach-Object {
    [System.IO.File]::WriteAllText((Join-Path $snap 'manifest.json'), $_, $utf8NoBom)
}

Write-Host ''
Write-Host "Snapshot : $snap"
Write-Host "Branch   : $($manifest.branch) @ $($manifest.head)"
Write-Host "Changes  : $($manifest.statusLines) status lines, $($manifest.untracked) untracked, $($manifest.copiedUntracked) copied"
Write-Host "Unity    : $($manifest.unityVersion)  exists=$unityExists"
Write-Host "MCP      : unity(8080)=$unityMcp  unreal(8000)=$unrealMcp"
Write-Host ''

if (-not $unityExists) {
    Write-Warning "Unity project not found: $Unity"
    exit 2
}
exit 0

