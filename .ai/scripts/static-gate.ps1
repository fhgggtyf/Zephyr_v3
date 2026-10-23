# Static gate for the current working tree.
# Scans only ADDED lines of changed C# files; does not blame historical debt.
# Exit 1 on FAIL, 0 on PASS/WARN.
[CmdletBinding()]
param(
    [string]$Root = 'D:\GitHub\Zephyr_v3',
    [string[]]$Files = @(),
    [string]$Baseline = ''
)

$ErrorActionPreference = 'Continue'
if (-not (Test-Path -LiteralPath $Root)) { throw "Root not found: $Root" }
$Root = (Resolve-Path -LiteralPath $Root).Path
$Unity = Join-Path $Root 'Zephyr(AI)\Zephyr'

$status = @(& git -C $Root status --porcelain=v1 2>$null)
$changed = @()

if ($Files.Count -gt 0) {
    # Task-scoped mode: only the files the orchestrator says this task touched.
    $rootNorm = ($Root -replace '\\', '/').TrimEnd('/')
    $changed = @($Files | ForEach-Object {
        $r = ($_.Trim().Trim('"') -replace '\\', '/')
        if ($r.StartsWith($rootNorm + '/')) { $r = $r.Substring($rootNorm.Length + 1) }
        $r
    } | Where-Object { $_ } | Sort-Object -Unique)
}
else {
    foreach ($line in $status) {
        if ($line.Length -lt 4) { continue }
        $rel = $line.Substring(3).Trim()
        if ($rel -match ' -> ') { $rel = ($rel -split ' -> ')[-1].Trim('"') }
        $rel = $rel.Trim('"')
        if ($rel -match '(^|/)(Library|Temp|Logs|obj|\.git)/') { continue }
        if ($rel -match '^\.ai/snapshots/') { continue }
        $changed += $rel
    }
    $changed = @($changed | Sort-Object -Unique)

    # Optional baseline: exclude files that were already dirty before the task.
    if ($Baseline) {
        $baseFile = Join-Path $Baseline 'git-status.txt'
        if (Test-Path -LiteralPath $baseFile) {
            $basePaths = @()
            foreach ($line in Get-Content -LiteralPath $baseFile) {
                $line = $line.TrimStart([char]0xFEFF)
                if ($line.Length -lt 4) { continue }
                $p = $line.Substring(3).Trim()
                if ($p -match ' -> ') { $p = ($p -split ' -> ')[-1].Trim('"') }
                $basePaths += ($p.Trim('"') -replace '\\', '/')
            }
            $changed = @($changed | Where-Object { $basePaths -notcontains $_ })
        }
    }
}

$findings = @()
function Add-Finding([string]$Level, [string]$File, [string]$Pattern, [string]$Line) {
    $script:findings += [pscustomobject]@{
        Level   = $Level
        File    = $File
        Pattern = $Pattern
        Line    = $Line
    }
}

$rules = @(
    @{ Name = 'SceneManager.Load/Unload'; Regex = 'SceneManager\.(LoadScene|UnloadScene)'; Level = 'FAIL' },
    @{ Name = 'Resources.Load';            Regex = 'Resources\.Load';                          Level = 'FAIL' },
    @{ Name = 'GameObject.Find';           Regex = 'GameObject\.Find';                         Level = 'FAIL' },
    @{ Name = 'FindObjectOfType';          Regex = 'Find(Object|AnyObject)OfType';             Level = 'FAIL' },
    @{ Name = 'DontDestroyOnLoad';         Regex = 'DontDestroyOnLoad';                        Level = 'FAIL' },
    @{ Name = 'Zephyr.Core dependency';    Regex = '(using\s+Zephyr\.Core\b|"Zephyr\.Core")'; Level = 'FAIL' },
    @{ Name = 'direct singleton .Instance'; Regex = '\.Instance\b';                            Level = 'WARN' }
)

foreach ($rel in $changed) {
    if ($rel -notmatch '\.cs$') { continue }
    $abs = Join-Path $Root ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $abs -PathType Leaf)) { continue }

    & git -C $Root ls-files --error-unmatch -- $rel *> $null
    $isTracked = ($LASTEXITCODE -eq 0)

    $lines = @()
    if ($isTracked) {
        $diff = & git -C $Root diff -U0 -- $rel 2>$null
        $lines = @($diff | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' } | ForEach-Object { $_.Substring(1) })
    } else {
        $lines = @(Get-Content -LiteralPath $abs)
    }

    foreach ($line in $lines) {
        $trim = $line.Trim()
        $isComment = $trim.StartsWith('//') -or $trim.StartsWith('*') -or $trim.StartsWith('/*')
        foreach ($rule in $rules) {
            if ($line -match $rule.Regex) {
                $level = $rule.Level
                if ($isComment) { $level = 'WARN' }
                Add-Finding $level $rel $rule.Name $trim
            }
        }
    }
}

# asmdef changes require a policy update
$asmdefChanged = @($changed | Where-Object { $_ -match '\.asmdef$' })
$policyChanged = @($changed | Where-Object { $_ -match 'ArchitecturePolicy\.json$' })
if ($asmdefChanged.Count -gt 0 -and $policyChanged.Count -eq 0) {
    Add-Finding 'FAIL' 'ArchitecturePolicy.json' 'asmdef changed without policy update' ($asmdefChanged -join ', ')
}

# Unity serialized assets: can only warn (must confirm they went through Unity MCP)
$serialized = @($changed | Where-Object { $_ -match '\.(unity|prefab|asset|controller|anim)$' })
foreach ($rel in $serialized) {
    Add-Finding 'WARN' $rel 'Unity serialized asset changed' 'confirm it was edited via mcp__unity_mcp__*'
}

# New assets should have a .meta
$newAssets = @($changed | Where-Object { $_ -match '^Zephyr\(AI\)/Zephyr/Assets/' -and $_ -notmatch '\.meta$' })
foreach ($rel in $newAssets) {
    $abs = Join-Path $Root ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $abs -PathType Leaf)) { continue }
    & git -C $Root ls-files --error-unmatch -- $rel *> $null
    if ($LASTEXITCODE -ne 0 -and -not (Test-Path -LiteralPath ($abs + '.meta'))) {
        Add-Finding 'WARN' $rel 'missing .meta' 'Unity will generate it on import; verify GUID'
    }
}

$failures = @($findings | Where-Object { $_.Level -eq 'FAIL' })
$warnings = @($findings | Where-Object { $_.Level -eq 'WARN' })

Write-Host ''
Write-Host "Changed files: $($changed.Count)"
Write-Host "FAIL: $($failures.Count)  WARN: $($warnings.Count)"
Write-Host ''

if ($findings.Count -gt 0) {
    $findings | Sort-Object Level, File, Pattern | Format-Table -AutoSize -Wrap | Out-String | Write-Host
}

if ($failures.Count -gt 0) {
    Write-Host 'STATIC GATE: FAIL' -ForegroundColor Red
    exit 1
}

Write-Host 'STATIC GATE: PASS (warnings are advisory)' -ForegroundColor Green
exit 0
