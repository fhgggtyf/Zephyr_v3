# Numeric inflation analysis

Calibration mode: `dps_bands`.

Resources are excluded. Each round chooses one available offensive increment; crit multiplier is unavailable until crit chance has already been added.
The eight increments are +20 flat attack, +1% target-max-HP damage, +10% skill multiplier, +0.1 attack frequency, +20% crit chance, +30% crit multiplier, +20 flat dual penetration and +10% multiplicative dual penetration (capped at three stacks).

## Defensive target schedule

| Round | Max HP | Armor | Magic resistance | Shield |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 100 | 50 | 50 | 0 |
| 8 | 150 | 130 | 130 | 0 |
| 11 | 400 | 50 | 50 | 0 |
| 14 | 9300 | 50 | 50 | 0 |

Armor and magic resistance are equal. The fixed attacker is modeled as a 50/50 physical/magical split, so both defenses participate.
TTK is discrete by hit count but has no resource delays. DPS is expected post-mitigation damage per second before the target is defeated.

## Round distribution

| Round | DPS P50 | DPS P70 (top-30% cutoff) | DPS P90 | DPS max | Top-30% TTK P50 | Top-30% TTK range |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 66.7 | 66.7 | 66.7 | 66.7 | 2s | 2-2s |
| 1 | 73.3 | 73.3 | 80.0 | 80.0 | 2s | 1.82-2s |
| 2 | 80.7 | 84.6 | 88.0 | 93.3 | 2s | 1.82-2s |
| 3 | 88.7 | 93.1 | 101.5 | 109.1 | 1.82s | 1-2s |
| 4 | 98.2 | 105.6 | 111.7 | 127.3 | 1s | 0.909-1.82s |
| 5 | 110.9 | 116.2 | 123.2 | 145.5 | 1s | 0.833-1.67s |
| 6 | 121.8 | 127.8 | 137.4 | 163.6 | 0.909s | 0.769-1.43s |
| 7 | 133.3 | 142.5 | 155.1 | 181.8 | 0.909s | 0.714-1s |
| 8 | 91.1 | 96.8 | 104.0 | 127.3 | 1.82s | 1.43-2.31s |
| 9 | 99.7 | 106.1 | 114.6 | 145.5 | 1.82s | 1.33-2s |
| 10 | 109.2 | 116.2 | 125.8 | 163.6 | 1.82s | 1-2s |
| 11 | 198.6 | 211.6 | 230.0 | 273.5 | 2s | 1.54-2.73s |
| 12 | 218.2 | 232.8 | 253.0 | 308.4 | 1.82s | 1.43-2.5s |
| 13 | 239.5 | 255.7 | 279.1 | 349.1 | 1.82s | 1.33-2.31s |
| 14 | 541.0 | 624.8 | 750.3 | 1135.6 | 13.6s | 8.33-15.5s |
| 15 | 612.1 | 700.6 | 835.4 | 1283.5 | 12s | 7.69-14s |

## Target-band check

- Initial DPS: 66.7.
- Round 8 P70 DPS: 96.8 (requested band: 50-100).
- Round 11 P70 DPS: 211.6 (requested band: 200-300).
- Round 14 P70 DPS: 624.8 (requested band: 600-800).
- Round 15 maximum DPS: 1283.5 (requested reference band: 1200-1400).
- Restricting the final sample to builds where all eight attack attributes appear at least once gives a round-15 P70 of 687.3 DPS and a maximum of 1082.9 DPS.
- The unrestricted maximum is higher because a concentrated build can omit whichever increments are weak at the current target HP; this is the first sign that the increments are not mutually balanced.

## Top-30% TTK consistency

Top-30% means states at or above the weighted DPS P70 cutoff. TTK is measured on that same DPS-selected cohort, so this is not an independent TTK percentile.

| Anchor round | Target HP | Top-30% TTK P50 | Top-30% TTK range |
| ---: | ---: | ---: | ---: |
| 0 | 100 | 2s | 2-2s |
| 8 | 150 | 1.82s | 1.43-2.31s |
| 11 | 400 | 2s | 1.54-2.73s |
| 14 | 9300 | 13.6s | 8.33-15.5s |
- Across the four anchor rounds, the top-30% TTK median spans 1.82-13.6s (7.50x) and does not stay within the requested roughly-two-second window.
- With fixed offensive increments, matching the late DPS and extreme-DPS bands requires enough target HP for max-HP damage to scale. That necessarily raises TTK from the 150-HP baseline; nonnegative armor/MR can only reduce DPS and cannot resolve this conflict.

## Marginal boundaries

- Flat attack versus max-HP damage: +1% max-HP damage is larger than +20 flat attack whenever TargetMaxHP > 2000. At the initial 100 HP it adds 1 raw damage and is weaker than +20 flat attack; at the final 9300 HP it adds 93 raw damage and remains stronger than +20 flat attack.
- Skill multiplier versus attack frequency: both are multiplicative and their next-stack value is approximately 0.1/current multiplier. Invest in the lower current multiplier.
- Crit chance versus crit multiplier: with an uncapped +20% chance step, crit multiplier is better when `0.3 x CritChance > 0.2 x (CritMultiplier - 1)`. Once chance is capped, only crit multiplier can continue to grow.
- At rounds 8, 11 and 14, the reported TTK gain includes the target defense/HP inflation, so a negative or unusually small gain means the defensive spike absorbed the offensive stack.

## Round-15 next-stack comparison

| Attribute | Mean TTK gain | Share of available paths where tied for best |
| --- | ---: | ---: |
| +20 flat attack | 7.62% | 0.01% |
| +1% max-HP damage | 35.10% | 99.32% |
| +10% skill multiplier | 8.37% | 0.80% |
| +0.1 attack frequency | 8.57% | 0.61% |
| +20% crit chance | 11.85% | 5.47% |
| +30% crit multiplier | 9.27% | 2.09% |
| +20 flat dual penetration | 10.34% | 8.45% |
| +10% multiplicative dual penetration | 9.79% | 1.66% |

## Compatibility finding

- This `dps_bands` rerun removes shield and keeps target armor/magic resistance at or above 50.
- At the initial 100 HP, +1% max-HP damage adds only 1 raw damage while +20 flat attack adds 20. Percent damage is therefore an intentionally weak early choice in this run.
- At the final 9300 HP, +1% max-HP damage adds 93 raw damage while +20 flat attack still adds 20, so it is stronger in this tuned run.
- Keeping +1% max-HP damage and +20 flat attack equal requires a target at 2000 HP.
- Fixed dual penetration subtracts 20 from both defenses per stack. Percent dual penetration is modeled as an independent 1.10x multiplier per stack, capped at three stacks, following the requested 1.1 x 1.1 x 1.1 rule.
