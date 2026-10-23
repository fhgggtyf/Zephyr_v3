# Workflow integrity guard.
# pre: snapshot the canonical workflow + hash.
# post: if the file changed during the run, restore the snapshot and exit 1.
# restore: force-restore the snapshot.
[CmdletBinding()]
param(
    [ValidateSet('pre', 'post', 'restore')][string]$Mode = 'post',
    [string]$Root = 'D:\GitHub\Zephyr_v3'
)

$ErrorActionPreference = 'Continue'
$target = Join-Path $Root '.ai\workflows\unity-task.workflow.js'
$stateDir = Join-Path $Root '.ai\state'
$backup = Join-Path $stateDir 'workflow.guard.bak'
$meta = Join-Path $stateDir 'workflow.guard.json'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not (Test-Path -LiteralPath $target)) {
    throw "Workflow not found: $target"
}

function Get-WorkflowHash {
    (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
}

switch ($Mode) {
    'pre' {
        New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
        Copy-Item -LiteralPath $target -Destination $backup -Force
        $hash = Get-WorkflowHash
        $payload = [ordered]@{
            hash    = $hash
            savedAt = (Get-Date).ToString('o')
            source  = $target
        } | ConvertTo-Json -Compress
        [System.IO.File]::WriteAllText($meta, $payload, $utf8NoBom)
        Write-Host "workflow-guard: pre saved $hash"
        exit 0
    }
    'restore' {
        if (-not (Test-Path -LiteralPath $backup)) {
            Write-Warning "no snapshot: $backup"
            exit 2
        }
        Copy-Item -LiteralPath $backup -Destination $target -Force
        Write-Host 'workflow-guard: restored from snapshot'
        exit 0
    }
    'post' {
        if (-not (Test-Path -LiteralPath $backup) -or -not (Test-Path -LiteralPath $meta)) {
            Write-Warning 'no baseline; run -Mode pre before the workflow'
            exit 2
        }
        $saved = (Get-Content -LiteralPath $meta -Raw | ConvertFrom-Json).hash
        $current = Get-WorkflowHash
        if ($current -ne $saved) {
            Copy-Item -LiteralPath $backup -Destination $target -Force
            Write-Host "workflow-guard: CHANGED during run -> restored" -ForegroundColor Red
            Write-Host "  before: $saved"
            Write-Host "  after : $current"
            exit 1
        }
        Write-Host 'workflow-guard: unchanged' -ForegroundColor Green
        exit 0
    }
}
