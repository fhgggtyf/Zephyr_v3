# Zephyr theoretical stats sensitivity analysis

This folder contains a theory-only combat model. It deliberately does not load or reference
any game weapon, enemy, prefab, ScriptableObject, scene or save data.

## Run

From the repository root:

```powershell
python .\Documents\stats\theoretical_sensitivity.py
```

The script uses only the Python standard library. Generated files are placed in
`Documents/stats/output`:

- `sensitivity_data.csv`: every one-variable sweep and calculated result.
- `sensitivity_curves.svg`: normalized earnings/minute sensitivity curves.
- `context_slices.svg`: one-variable curves repeated under low/medium/high values of one
  interacting context variable.
- `interaction_heatmaps.svg`: selected within-group two-variable relationships.
- `sensitivity_summary.md`: assumptions, baseline results and sensitivity ranking.
- `results_analysis.md`: human-readable conclusions from the current results.
- `equal_ttk_increments.csv`: dense scans for the increment nearest a 10% TTK benefit.
- `equal_ttk_summary.md`: readable burst and sustainable equal-TTK comparison tables.
- `shield_coeff_context.csv`: low/medium/high HP and shield coefficient scan data.
- `shield_coeff_context.md`: readable TTK comparisons for the nine HP/shield contexts.
- `equal_survival_increments.csv`: defensive increments for approximately 10% survival gain.
- `equal_survival_summary.md`: continuous, burst and sustainable survival comparison.
- `numeric_inflation.py`: resource-free sequential stacking and path-distribution analysis.
- `numeric_inflation_rounds.csv`: DPS/TTK distributions for rounds 0-15.
- `numeric_inflation_marginals.csv`: per-round next-attribute TTK gains and best-choice shares.
- `numeric_inflation_final_states.csv`: final stack compositions and their DPS/TTK.
- `numeric_inflation_summary.md`: tuned defense schedule, target-band check and boundary rules.

## Baseline model

The normalized attacker deals 100 flat damage per hit:

```text
100 flat damage + 0% of target maximum HP
```

The normalized target has 1,000 maximum HP and is evaluated from full health, with no defence
and no shield. The attacker
performs one attack per second with 100% uptime. One kill grants one normalized reward unit.

Percentage damage is a fraction of target maximum HP. Fixed penetration subtracts defence
first; percentage penetration then scales the remaining defence. Resource regeneration starts
only after one second without a resource spend.

The AOE approximation uses only an upper semicircle (`0.5 * pi * radius^2`), excludes
underground targets, and keeps ground and airborne densities separate. It is reference-only:
room geometry and target placement are not controlled by this model.

The baseline therefore produces:

```text
100 damage/hit
100 burst DPS
10 second burst TTK
50 sustained DPS after the one-second recovery delay is included
3 normalized reward units/minute
```

The sustained rate is calculated from the steady-state resource cycle rather than
from a finite simulation window, so the initial full-resource burst cannot inflate
long-run results.

All baseline assumptions and sweep ranges are declared near the top of
`theoretical_sensitivity.py` and can be edited without touching the calculation code.

## Testing rule

Each sensitivity curve changes one value while keeping every other model value at baseline.
Heatmaps are reserved for selected pairs that have a direct mathematical interaction, such as
critical chance × critical multiplier or defence × fixed/percentage penetration. They are not
a full Cartesian product of every combat variable.
