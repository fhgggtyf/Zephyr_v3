# Numeric inflation analysis

Resources are excluded. Each round chooses one available offensive increment; crit multiplier is unavailable until crit chance has already been added.
The six increments are +10 flat attack, +1% target-max-HP damage, +10% skill multiplier, +0.1 attack frequency, +20% crit chance and +30% crit multiplier.

## Defensive target schedule

| Round | Max HP | Armor | Magic resistance | Shield |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 150 | 1 | 1 | 0 |
| 8 | 1000 | 2 | 2 | 0 |
| 11 | 3000 | 3 | 3 | 0 |
| 14 | 6250 | 5 | 5 | 0 |

Armor and magic resistance are equal. The fixed attacker is modeled as a 50/50 physical/magical split, so both defenses participate.
TTK is discrete by hit count but has no resource delays. DPS is expected post-mitigation damage per second before the target is defeated.

## Round distribution

| Round | DPS P50 | DPS P70 (top-30% cutoff) | DPS P90 | DPS max | Top-30% TTK P50 | Top-30% TTK range |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 99.0 | 99.0 | 99.0 | 99.0 | 2s | 2-2s |
| 1 | 108.9 | 108.9 | 108.9 | 108.9 | 2s | 1.82-2s |
| 2 | 118.8 | 119.8 | 119.8 | 119.8 | 1.82s | 1.82-2s |
| 3 | 126.3 | 130.7 | 131.8 | 131.8 | 1.82s | 1.67-2s |
| 4 | 133.6 | 142.6 | 143.8 | 146.5 | 1.82s | 1.67-2s |
| 5 | 145.7 | 152.5 | 156.8 | 164.4 | 1.67s | 1-1.82s |
| 6 | 158.8 | 163.4 | 172.5 | 186.1 | 1s | 0.909-1.82s |
| 7 | 172.0 | 175.1 | 186.9 | 209.9 | 0.909s | 0.833-1.67s |
| 8 | 199.3 | 201.9 | 207.5 | 235.3 | 5s | 4.55-5.45s |
| 9 | 217.4 | 220.2 | 229.8 | 264.7 | 5s | 4-5s |
| 10 | 235.5 | 240.6 | 253.9 | 294.1 | 4.55s | 3.64-5s |
| 11 | 321.4 | 341.4 | 373.0 | 456.3 | 8.57s | 7-9.38s |
| 12 | 353.7 | 378.6 | 413.8 | 514.6 | 8s | 6-8.57s |
| 13 | 389.8 | 418.0 | 458.6 | 582.5 | 7.27s | 5.45-8s |
| 14 | 580.2 | 654.9 | 762.8 | 1085.7 | 9.09s | 6-10s |
| 15 | 646.5 | 729.3 | 846.4 | 1228.6 | 8.18s | 5.45-9.23s |

## Target-band check

- Initial DPS: 99.0 (requested reference band: 50-100).
- Round 8 P70 DPS: 201.9 (requested reference band: 200-300).
- Round 15 P70 DPS: 729.3 (requested reference band: 600-800).
- Round 15 maximum DPS: 1228.6 (requested reference band: 1200-1400).
- Restricting the final sample to builds where all six attack attributes appear at least once gives a round-15 P70 of 722.8 DPS and a maximum of 1050.7 DPS.
- The unrestricted maximum is higher because a concentrated build can omit whichever increments are weak at the current target HP; this is the first sign that the increments are not mutually balanced.

## Top-30% TTK consistency

Top-30% means states at or above the weighted DPS P70 cutoff. TTK is measured on that same DPS-selected cohort, so this is not an independent TTK percentile.

| Anchor round | Target HP | Top-30% TTK P50 | Top-30% TTK range |
| ---: | ---: | ---: | ---: |
| 0 | 150 | 2s | 2-2s |
| 8 | 1000 | 5s | 4.55-5.45s |
| 11 | 3000 | 8.57s | 7-9.38s |
| 14 | 6250 | 9.09s | 6-10s |
- Across the four anchor rounds, the top-30% TTK median spans 2-9.09s (4.55x); it is not similar across the full progression.
- With fixed offensive increments, matching the late DPS and extreme-DPS bands requires enough target HP for max-HP damage to scale. That necessarily raises TTK from the 150-HP baseline; nonnegative armor/MR can only reduce DPS and cannot resolve this conflict.

## Marginal boundaries

- Flat attack versus max-HP damage: +1% max-HP damage is larger than +10 flat attack whenever TargetMaxHP > 1000. At the initial 150 HP it adds 1.5 raw damage and is weaker than +10 flat attack; at the final 6250 HP it adds 62.5 raw damage and remains stronger than +10 flat attack.
- Skill multiplier versus attack frequency: both are multiplicative and their next-stack value is approximately 0.1/current multiplier. Invest in the lower current multiplier.
- Crit chance versus crit multiplier: with an uncapped +20% chance step, crit multiplier is better when `0.3 x CritChance > 0.2 x (CritMultiplier - 1)`. Once chance is capped, only crit multiplier can continue to grow.
- At rounds 8, 11 and 14, the reported TTK gain includes the target defense/HP inflation, so a negative or unusually small gain means the defensive spike absorbed the offensive stack.

## Round-15 next-stack comparison

| Attribute | Mean TTK gain | Share of available paths where tied for best |
| --- | ---: | ---: |
| Flat attack | 4.08% | 0.44% |
| Max-HP damage | 24.86% | 98.23% |
| Skill multiplier | 7.92% | 7.75% |
| Attack frequency | 8.21% | 0.38% |
| Crit chance | 11.76% | 25.25% |
| Crit multiplier | 9.99% | 15.37% |

## Compatibility finding

- This rerun removes shield from the generic target and tunes only target HP, armor and magic resistance against the requested DPS bands.
- At the initial 150 HP, +1% max-HP damage adds only 1.5 raw damage while +10 flat attack adds 10. Percent damage is therefore an intentionally weak early choice in this run.
- At the final 6250 HP, +1% max-HP damage adds 62.5 raw damage while +10 flat attack still adds 10, so it is stronger in this tuned run.
- Keeping +1% max-HP damage and +10 flat attack equal requires a target at 1000 HP.
