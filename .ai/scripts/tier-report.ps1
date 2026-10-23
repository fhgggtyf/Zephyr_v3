# Summarize .ai/ledger/tier-metrics.jsonl and flag Tier 1 boundary drift.
# Lagging signal: rollback rate. Leading signals: gate failure rate, rework rounds, GPT audit findings.
[CmdletBinding()]
param(
    [string]$Root = 'D:\GitHub\Zephyr_v3',
    [int]$Window = 10
)

$ErrorActionPreference = 'Continue'
$path = Join-Path $Root '.ai\ledger\tier-metrics.jsonl'
if (-not (Test-Path -LiteralPath $path)) {
    Write-Host 'No tier metrics yet.'
    exit 0
}

$rows = @()
foreach ($line in Get-Content -LiteralPath $path) {
    $line = $line.Trim()
    if (-not $line) { continue }
    try { $rows += ($line | ConvertFrom-Json) } catch { Write-Warning "skip malformed row: $line" }
}
if ($rows.Count -eq 0) {
    Write-Host 'No tier metrics yet.'
    exit 0
}

$rows = @($rows | Sort-Object { [datetime]$_.ts })
$recent = @($rows | Select-Object -Last $Window)

$tier1Rows = @($rows | Where-Object { [int]$_.tier -eq 1 })
$sinceAudit = 0
for ($i = $tier1Rows.Count - 1; $i -ge 0; $i--) {
    if ($tier1Rows[$i].gptReview) { break }
    $sinceAudit++
}

$stats = @()
$warnings = @()

foreach ($tier in @(0, 1, 2)) {
    $all = @($rows | Where-Object { [int]$_.tier -eq $tier })
    $win = @($recent | Where-Object { [int]$_.tier -eq $tier })
    if ($all.Count -eq 0) { continue }

    $n = $win.Count
    $gateFail = @($win | Where-Object { $_.gate -eq 'fail' }).Count
    $rollback = @($win | Where-Object { $_.rollback -ne 'none' }).Count
    $reworkTotal = 0
    foreach ($r in $win) { $reworkTotal += [int]$r.rework }
    $gptReview = @($win | Where-Object { $_.gptReview }).Count

    $gateRate = if ($n -gt 0) { [math]::Round(100.0 * $gateFail / $n, 1) } else { 0 }
    $rollbackRate = if ($n -gt 0) { [math]::Round(100.0 * $rollback / $n, 1) } else { 0 }
    $avgRework = if ($n -gt 0) { [math]::Round($reworkTotal / $n, 2) } else { 0 }

    $stats += [pscustomobject]@{
        Tier        = $tier
        Window      = $n
        Total       = $all.Count
        GateFailPct = $gateRate
        RollbackPct = $rollbackRate
        AvgRework   = $avgRework
        GptReview   = $gptReview
        SinceAudit  = if ($tier -eq 1) { $sinceAudit } else { 0 }
    }

    # Tier 1 is the lean path; these thresholds decide whether to tighten it.
    if ($tier -eq 1 -and $n -ge 5) {
        if ($gateRate -gt 20) {
            $warnings += "Tier 1 gate failure ${gateRate}% > 20% (last $n). Tighten Tier 1 criteria; move this pattern to Tier 2."
        }
        if ($rollbackRate -gt 10) {
            $warnings += "Tier 1 rollback ${rollbackRate}% > 10% (last $n). Tighten Tier 1; do not let lean trade quality."
        }
        if ($avgRework -gt 1.5) {
            $warnings += "Tier 1 average rework ${avgRework} > 1.5 (last $n). Boundary likely too loose."
        }
    }
    if ($tier -eq 1 -and $sinceAudit -ge 5) {
        $warnings += "Tier 1 audit due: $sinceAudit Tier 1 tasks since the last gptReview; run one with gptReview:true as a blind audit."
    }
}

Write-Host ''
Write-Host "Tier metrics (last $Window rows, total $($rows.Count))"
$stats | Format-Table -AutoSize | Out-String | Write-Host

if ($warnings.Count -gt 0) {
    Write-Host 'WARNINGS:' -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host " - $_" -ForegroundColor Yellow }
    Write-Host ''
    Write-Host 'Action: update .ai/checklists/tiers.md, move the drifting pattern to Tier 2, and record why.' -ForegroundColor Yellow
    exit 1
}

Write-Host 'No Tier 1 boundary warning.' -ForegroundColor Green
exit 0
