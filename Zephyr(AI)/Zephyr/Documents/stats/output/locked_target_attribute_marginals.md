# Locked-target attribute marginals

Targets are fixed at R1 = 150 HP / 50 defense, R2 = 225 HP / 65 defense,
and R3 = 360 HP / 80 defense. Armor and magic resistance are equal, and no
shield is present.

Marginal gain uses the established definition:

`gain = TTK_before / TTK_after - 1 = DPS_after / DPS_before - 1`

The percentage is therefore a DPS-equivalent gain. The actual elapsed-TTK
reduction is `gain / (1 + gain)` and is smaller; for example, a 15% marginal
gain reduces elapsed TTK by 13.04%.

## Pure one-stack marginal from the graduation initial configuration

The reference has 100 attack, 25% crit chance, 1.8x crit multiplier,
`1.2 x 1.2 x 1.2` extra multipliers, 1.5 attacks/s, 30 flat penetration and
10% percent penetration. Each row adds exactly one upgrade stack and changes
nothing else.

| Added upgrade | R1 gain | R2 gain | R3 gain |
| --- | ---: | ---: | ---: |
| +15% attack | 15.00% | 15.00% | 15.00% |
| +2% target max-HP damage | 3.00% | 4.50% | 7.20% |
| +0.2 attacks/s | 13.33% | 13.33% | 13.33% |
| +20 percentage points crit chance | 13.33% | 13.33% | 13.33% |
| +0.3 crit multiplier | 6.25% | 6.25% | 6.25% |
| +20 flat dual penetration | 15.00% | 18.43% | 16.39% |
| +10 percentage points percent dual penetration | 4.55% | 5.33% | 5.97% |
| +0.1 extra multiplier 1 | 8.33% | 8.33% | 8.33% |
| +0.1 extra multiplier 2 | 8.33% | 8.33% | 8.33% |
| +0.1 extra multiplier 3 | 8.33% | 8.33% | 8.33% |

## Pure one-stack marginal from the reduced initial configuration

The reference has 50 attack, 0% crit chance, 1.5x crit multiplier,
`1.0 x 1.0 x 1.0` extra multipliers, 1.0 attacks/s and no penetration.

| Added upgrade | R1 gain | R2 gain | R3 gain |
| --- | ---: | ---: | ---: |
| +15% attack | 15.00% | 15.00% | 15.00% |
| +2% target max-HP damage | 6.00% | 9.00% | 14.40% |
| +0.2 attacks/s | 20.00% | 20.00% | 20.00% |
| +20 percentage points crit chance | 10.00% | 10.00% | 10.00% |
| +0.3 crit multiplier | 0.00% | 0.00% | 0.00% |
| +20 flat dual penetration | 15.38% | 13.79% | 12.50% |
| +10 percentage points percent dual penetration | 3.45% | 4.10% | 4.65% |
| +0.1 extra multiplier 1 | 10.00% | 10.00% | 10.00% |
| +0.1 extra multiplier 2 | 10.00% | 10.00% | 10.00% |
| +0.1 extra multiplier 3 | 10.00% | 10.00% | 10.00% |

Crit multiplier has zero initial value here because crit chance is zero. It
only gains value after at least one crit-chance upgrade exists.

## Weighted mean next-stack marginal at each checkpoint

These tables start from every state already reachable at the checkpoint,
weight states by their ordered path counts, add one more stack of the selected
attribute, and average the resulting relative gain. They show dilution and
attribute interactions during progression rather than a clean initial-state
sensitivity.

### Graduation initial configuration

| Next upgrade | After R1 paths | After R6 paths | After R12 paths |
| --- | ---: | ---: | ---: |
| +15% attack | 14.76% | 13.55% | 12.04% |
| +2% target max-HP damage | 2.95% | 4.07% | 5.78% |
| +0.2 attacks/s | 13.18% | 12.44% | 11.65% |
| +20 percentage points crit chance | 13.57% | 14.41% | 14.49% |
| +0.3 crit multiplier | 6.58% | 7.98% | 9.13% |
| +20 flat dual penetration | 13.00% | 11.65% | 9.57% |
| +10 percentage points percent dual penetration | 4.11% | 4.44% | 4.29% |
| +0.1 extra multiplier 1 | 8.27% | 7.96% | 7.62% |
| +0.1 extra multiplier 2 | 8.27% | 7.96% | 7.62% |
| +0.1 extra multiplier 3 | 8.27% | 7.96% | 7.62% |

### Reduced initial configuration

| Next upgrade | After R1 paths | After R6 paths | After R12 paths |
| --- | ---: | ---: | ---: |
| +15% attack | 14.72% | 13.25% | 11.34% |
| +2% target max-HP damage | 5.89% | 7.95% | 10.89% |
| +0.2 attacks/s | 19.67% | 18.14% | 16.56% |
| +20 percentage points crit chance | 10.51% | 12.68% | 14.46% |
| +0.3 crit multiplier | 0.55% | 3.04% | 5.47% |
| +20 flat dual penetration | 15.73% | 15.27% | 14.67% |
| +10 percentage points percent dual penetration | 3.52% | 4.56% | 5.56% |
| +0.1 extra multiplier 1 | 9.91% | 9.48% | 9.00% |
| +0.1 extra multiplier 2 | 9.91% | 9.48% | 9.00% |
| +0.1 extra multiplier 3 | 9.91% | 9.48% | 9.00% |

## Reading the result

- Graduation configuration: flat penetration is strongest against the R2
  initial target, but loses mean value later as more paths already reduce
  effective defense to zero. Crit chance becomes the strongest average next
  upgrade by R12 because it interacts with accumulated crit multiplier.
- Reduced configuration: attack speed is the strongest clean initial upgrade.
  At R12 its average next-stack value remains 16.56%, followed by flat
  penetration at 14.67% and crit chance at 14.46%.
- Max-HP damage scales with target HP relative to the attack term. It grows
  from 3.00% to 7.20% in the graduation initial configuration and from 6.00%
  to 14.40% in the reduced initial configuration.
- Percentage penetration is weak in these targets because percentage
  penetration is applied before flat penetration. Existing flat penetration
  removes much of the defense on which percentage penetration could operate.
- Each named extra multiplier has identical value here. Its marginal value
  declines when more upgrades are added to the same multiplier because the
  +0.1 increment is additive inside that multiplier.
