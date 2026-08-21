# Numeric inflation analysis

Resources are excluded. Each round chooses one available offensive increment; crit multiplier is unavailable until crit chance has already been added.
The six increments are +10 flat attack, +1% target-max-HP damage, +10% skill multiplier, +0.1 attack frequency, +20% crit chance and +30% crit multiplier.

## Defensive target schedule

| Round | Max HP | Armor | Magic resistance | Shield |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 150 | 1 | 1 | 100 |
| 8 | 187.5 | 15 | 15 | 200 |
| 11 | 262.5 | 19 | 19 | 300 |
| 14 | 337.5 | 25 | 25 | 400 |

Armor and magic resistance are equal. The fixed attacker is modeled as a 50/50 physical/magical split, so both defenses participate.
TTK is discrete by hit count but has no resource delays. DPS is expected post-mitigation damage per second before the target is defeated.

## Round distribution

| Round | DPS P50 | DPS P70 (top-30% cutoff) | DPS P90 | DPS max | Top-30% TTK P50 | Top-30% TTK range |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 99.0 | 99.0 | 99.0 | 99.0 | 3s | 3-3s |
| 1 | 108.9 | 108.9 | 108.9 | 108.9 | 3s | 2.73-3s |
| 2 | 118.8 | 119.8 | 119.8 | 119.8 | 2.73s | 2.73-3s |
| 3 | 126.3 | 130.7 | 131.8 | 131.8 | 2.5s | 2-2.73s |
| 4 | 133.6 | 142.6 | 143.8 | 146.5 | 2s | 1.82-2.5s |
| 5 | 145.7 | 152.5 | 156.8 | 164.4 | 1.82s | 1.67-2.31s |
| 6 | 158.8 | 163.4 | 172.5 | 186.1 | 1.82s | 1.54-2.14s |
| 7 | 172.0 | 175.1 | 186.9 | 209.9 | 1.82s | 1.43-2s |
| 8 | 162.5 | 168.1 | 179.1 | 208.7 | 2.73s | 2-3s |
| 9 | 175.2 | 182.4 | 195.3 | 234.8 | 2.5s | 1.82-3s |
| 10 | 188.0 | 198.4 | 213.2 | 260.9 | 2.5s | 1.67-2.73s |
| 11 | 198.7 | 209.9 | 225.7 | 277.3 | 2.86s | 2.31-3.33s |
| 12 | 215.2 | 228.1 | 246.3 | 305.0 | 2.73s | 2-3.08s |
| 13 | 232.9 | 247.6 | 270.6 | 335.5 | 2.5s | 1.82-3s |
| 14 | 242.7 | 258.4 | 282.7 | 351.4 | 3.08s | 2.31-3.64s |
| 15 | 262.8 | 279.9 | 307.8 | 383.3 | 2.86s | 2-3.33s |

## Target-band check

- Initial DPS: 99.0 (requested reference band: 50-100).
- Round 8 P70 DPS: 168.1 (requested reference band: 200-300).
- Round 15 P70 DPS: 279.9 (requested reference band: 600-800).
- Round 15 maximum DPS: 383.3 (requested reference band: 1200-1400).
- Restricting the final sample to builds where all six attack attributes appear at least once gives a round-15 P70 of 279.9 DPS and a maximum of 362.2 DPS.
- The unrestricted maximum is higher because a concentrated build can omit whichever increments are weak at the current target HP; this is the first sign that the increments are not mutually balanced.

## Top-30% TTK consistency

Top-30% means states at or above the weighted DPS P70 cutoff. TTK is measured on that same DPS-selected cohort, so this is not an independent TTK percentile.

| Anchor round | Target HP | Top-30% TTK P50 | Top-30% TTK range |
| ---: | ---: | ---: | ---: |
| 0 | 150 | 3s | 3-3s |
| 8 | 187.5 | 2.73s | 2-3s |
| 11 | 262.5 | 2.86s | 2.31-3.33s |
| 14 | 337.5 | 3.08s | 2.31-3.64s |
- Across the four anchor rounds, the top-30% TTK median spans 2.73-3.08s.

## Marginal boundaries

- Flat attack versus max-HP damage: +1% max-HP damage is larger than +10 flat attack whenever TargetMaxHP > 1000. At the initial 150 HP it adds 1.5 raw damage and is weaker than +10 flat attack; at the final 337.5 HP it adds 3.375 raw damage and remains weaker than +10 flat attack.
- Skill multiplier versus attack frequency: both are multiplicative and their next-stack value is approximately 0.1/current multiplier. Invest in the lower current multiplier.
- Crit chance versus crit multiplier: with an uncapped +20% chance step, crit multiplier is better when `0.3 x CritChance > 0.2 x (CritMultiplier - 1)`. Once chance is capped, only crit multiplier can continue to grow.
- At rounds 8, 11 and 14, the reported TTK gain includes the target defense/HP/shield inflation, so a negative or unusually small gain means the defensive spike absorbed the offensive stack.

## Round-15 next-stack comparison

| Attribute | Mean TTK gain | Share of available paths where tied for best |
| --- | ---: | ---: |
| Flat attack | 7.48% | 26.94% |
| Max-HP damage | 2.56% | 9.43% |
| Skill multiplier | 7.92% | 28.63% |
| Attack frequency | 8.21% | 50.90% |
| Crit chance | 11.58% | 40.96% |
| Crit multiplier | 9.93% | 34.55% |

## Compatibility finding

- This HP-scaled rerun is a controlled comparison. It does not retune the offensive increments or defenses to force the previous absolute DPS bands.
- At the initial 150 HP, +1% max-HP damage adds only 1.5 raw damage while +10 flat attack adds 10. Percent damage is therefore an intentionally weak early choice in this run.
- At the final 337.5 HP, +1% max-HP damage adds 3.375 raw damage while +10 flat attack still adds 10, so it is weaker in this scaled run.
- Keeping +1% max-HP damage and +10 flat attack equal requires a target at 1000 HP.
