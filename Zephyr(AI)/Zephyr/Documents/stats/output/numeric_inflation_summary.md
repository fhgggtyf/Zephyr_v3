# Numeric inflation analysis

Resources are excluded. Each round chooses one available offensive increment; crit multiplier is unavailable until crit chance has already been added.
The six increments are +10 flat attack, +1% target-max-HP damage, +10% skill multiplier, +0.1 attack frequency, +20% crit chance and +30% crit multiplier.

## Defensive target schedule

| Round | Max HP | Armor | Magic resistance | Shield |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 4000 | 1 | 1 | 100 |
| 8 | 5000 | 15 | 15 | 200 |
| 11 | 7000 | 19 | 19 | 300 |
| 14 | 9000 | 25 | 25 | 400 |

Armor and magic resistance are equal. The fixed attacker is modeled as a 50/50 physical/magical split, so both defenses participate.
TTK is discrete by hit count but has no resource delays. DPS is expected post-mitigation damage per second before the target is defeated.

## Round distribution

| Round | DPS P50 | DPS P70 (top-30% cutoff) | DPS P90 | DPS max | TTK P70 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 99.0 | 99.0 | 99.0 | 99.0 | 42s |
| 1 | 108.9 | 108.9 | 138.6 | 138.6 | 38s |
| 2 | 119.8 | 148.5 | 152.5 | 178.2 | 35s |
| 3 | 131.8 | 163.4 | 167.7 | 217.8 | 31.8s |
| 4 | 174.3 | 180.2 | 206.9 | 257.4 | 29.1s |
| 5 | 191.7 | 199.6 | 235.2 | 297.0 | 26.4s |
| 6 | 212.2 | 219.8 | 258.8 | 336.6 | 23.6s |
| 7 | 232.9 | 261.4 | 285.1 | 376.2 | 19.2s |
| 8 | 241.0 | 277.8 | 317.6 | 434.8 | 23.6s |
| 9 | 271.8 | 310.7 | 358.1 | 478.3 | 21.5s |
| 10 | 308.1 | 344.7 | 393.9 | 526.1 | 19.2s |
| 11 | 390.3 | 447.2 | 528.7 | 742.3 | 22.5s |
| 12 | 437.4 | 498.7 | 585.7 | 823.0 | 20s |
| 13 | 489.6 | 559.1 | 656.7 | 932.1 | 18s |
| 14 | 602.3 | 696.7 | 833.8 | 1238.1 | 19.2s |
| 15 | 673.3 | 777.2 | 924.8 | 1401.6 | 17s |

## Target-band check

- Initial DPS: 99.0, inside the requested 50-100 band.
- Round 8 P70 DPS: 277.8, inside the requested 200-300 mid-game band.
- Round 15 P70 DPS: 777.2, inside the requested 600-800 late-game band.
- Round 15 maximum DPS: 1401.6, near the requested 1200-1400 extreme band.
- Restricting the final sample to builds where all six attack attributes appear at least once gives a round-15 P70 of 764.9 DPS and a maximum of 1189.1 DPS.
- The unrestricted maximum is higher because a concentrated max-HP/crit path can omit weaker attributes; this is the first sign that the current increments are not mutually balanced.

## Marginal boundaries

- Flat attack versus max-HP damage: +1% max-HP damage is larger than +10 flat attack whenever TargetMaxHP > 1000. The tuned target is far above that boundary, so percent damage is usually the stronger next choice.
- Skill multiplier versus attack frequency: both are multiplicative and their next-stack value is approximately 0.1/current multiplier. Invest in the lower current multiplier.
- Crit chance versus crit multiplier: with an uncapped +20% chance step, crit multiplier is better when `0.3 × CritChance > 0.2 × (CritMultiplier - 1)`. Once chance is capped, only crit multiplier can continue to grow.
- At rounds 8, 11 and 14, the reported TTK gain includes the target defense/HP/shield inflation, so a negative or unusually small gain means the defensive spike absorbed the offensive stack.

## Round-15 next-stack comparison

| Attribute | Mean TTK gain | Share of available paths where tied for best |
| --- | ---: | ---: |
| Flat attack | 3.46% | 0.01% |
| Max-HP damage | 30.72% | 98.54% |
| Skill multiplier | 8.03% | 1.72% |
| Attack frequency | 8.21% | 0.92% |
| Crit chance | 11.94% | 13.03% |
| Crit multiplier | 10.11% | 5.51% |

## Compatibility finding

- The requested DPS bands can be reached with the tuned target schedule, but the six increments are not balanced at that target HP.
- At 9000 HP, +1% max-HP damage adds 90 raw damage while +10 flat attack adds only 10. This makes the percent stack roughly nine times larger before later multipliers.
- Keeping a 9000-HP late target would require about +0.11% max-HP damage per stack to match +10 flat attack, or about +90 flat attack to match the current +1% stack.
- Keeping +1% and +10 balanced requires a target near 1000 HP, but with only 15 single-stat stacks that lower HP cannot produce the requested 1200-1400 extreme DPS without another growth source.
