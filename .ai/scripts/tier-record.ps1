# Append one task row to .ai/ledger/tier-metrics.jsonl.
# Used by the orchestrator after every task; keep it cheap and deterministic.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('0', '1', '2')][string]$Tier,
    [Parameter(Mandatory = $true)][string]$Task,
    [ValidateSet('pass', 'fail', 'blocked')][string]$Gate = 'pass',
    [int]$Rework = 0,
    [ValidateSet('none', 'revert', 'fix-forward', 'reopen', 'post-commit-regression', 'user-rejected')][string]$Rollback = 'none',
    [switch]$GptReview,
    [switch]$GptPlan,
    [string]$Notes = '',
    [string]$Root = 'D:\GitHub\Zephyr_v3'
)

$ErrorActionPreference = 'Continue'
$ledgerDir = Join-Path $Root '.ai\ledger'
$path = Join-Path $ledgerDir 'tier-metrics.jsonl'
New-Item -ItemType Directory -Force -Path $ledgerDir | Out-Null

$row = [ordered]@{
    ts         = (Get-Date).ToString('o')
    task       = $Task
    tier       = [int]$Tier
    gate       = $Gate
    rework     = $Rework
    rollback   = $Rollback
    gptReview  = [bool]$GptReview
    gptPlan    = [bool]$GptPlan
    notes      = $Notes
}

$json = $row | ConvertTo-Json -Compress
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::AppendAllText($path, $json + "`n", $utf8NoBom)
Write-Host "recorded: $json"
