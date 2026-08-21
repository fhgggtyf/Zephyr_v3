# Equal-survival defensive increments

Target gain: `survival_after / survival_before - 1 = 10%`.
The fixed attacker deals 100 damage before target defense once per second; baseline defense is 0, so baseline post-mitigation damage is also 100. Percentage damage, crit and backstab are disabled.
ShieldCoeff is not valued as an attribute; ShieldCoeff=1 is used only for the shield-amount comparison.
The generic defense row represents armor against a physical attacker or magic resistance against a magical attacker.

| Defensive attribute | Baseline | Test value | Increment | Continuous gain | Burst TTK gain | Sustainable TTK gain |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Target max HP | 1000 | 1100 | +100 | 10.00% | 10s → 12s (+20.00%) | 20s → 22s (+10.00%) |
| Relevant defense (armor or MR) | 0 | 10 | +10 | 10.00% | 10s → 12s (+20.00%) | 20s → 22s (+10.00%) |
| Shield amount at ShieldCoeff=1 | 0 | 100 | +100 | 10.00% | 10s → 12s (+20.00%) | 20s → 22s (+10.00%) |

## Interpretation

- +100 maximum HP, +10 relevant defense, and +100 shield at coefficient 1 are equal at +10% continuous effective survival.
- Sustainable TTK also rises by exactly 10% because it is based on the steady 0.5 attacks/s resource cycle.
- Burst TTK rises from 10s to 12s (+20%), not 11s, because the eleventh attack occurs only after the resource recovery delay.
