# Graduation numeric-inflation analysis

No generic skill multiplier is present. Independently upgradeable percentages add inside their own stat.
Attack upgrades are +15% each and max-HP damage upgrades are +2% each.
The three named extra multipliers multiply each other, while upgrades within one named multiplier add to that multiplier.
Percent dual penetration is additive: 10% initially, then +10 percentage points per stack. Percentage penetration is applied before flat penetration.
TTK is continuous HP / expected DPS so a 0.5-second target is representable independently of individual attack frames.

## Raw-damage formula

`((Attack x (1 + attack bonus)) + TargetMaxHP x max-HP-damage%) x Extra1 x Extra2 x Extra3 x ExpectedCritFactor`

## Target tiers

| Starts at round | HP | Armor | Magic resistance | Shield |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 153.2660 | 50 | 50 | 0 |
| 6 | 225.7920 | 62.5 | 62.5 | 0 |
| 12 | 361.2230 | 78.125 | 78.125 | 0 |

## Maximum under locked-TTK constraints

| Checkpoint | Weighted P70 DPS | Maximum DPS | TTK at P70 cutoff | Top-30% TTK P50 | Requested |
| --- | ---: | ---: | ---: | ---: | --- |
| Round 1 | 306.5 | 311.0 | 0.500s | 0.493s | maximum with TTK 0.5s |
| Round 6 | 451.6 | 546.3 | 0.500s | 0.478s | maximum with TTK 0.5s |
| Round 12 | 722.4 | 1040.3 | 0.500s | 0.473s | maximum with TTK 0.5s |

## Result

- Each tier uses the lowest legal armor/MR (50, 62.5, 78.125) because any higher defense lowers DPS.
- The resulting P70 DPS maxima are approximately 306.5, 451.6 and 722.4 at rounds 1, 6 and 12; every P70 cutoff has continuous TTK 0.5s.
- The round-12 maximum is 1040.3, so the requested 1200-1400 extreme band is still unreachable under the locked-TTK and 25%-growth constraints.
- The maximum is finite in this search: the 2% max-HP damage increment does not create a runaway fixed point at these legal target defenses.
