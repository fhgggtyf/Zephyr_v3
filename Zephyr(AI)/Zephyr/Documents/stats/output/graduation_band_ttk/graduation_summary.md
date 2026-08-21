# Graduation numeric-inflation analysis

No generic skill multiplier is present. Independently upgradeable percentages add inside their own stat.
The three named extra multipliers multiply each other, while upgrades within one named multiplier add to that multiplier.
Percent dual penetration is additive: 10% initially, then +10 percentage points per stack. Percentage penetration is applied before flat penetration.
TTK is continuous HP / expected DPS so a 0.5-second target is representable independently of individual attack frames.

## Raw-damage formula

`((Attack x (1 + attack bonus)) + TargetMaxHP x max-HP-damage%) x Extra1 x Extra2 x Extra3 x ExpectedCritFactor`

## Target tiers

| Starts at round | HP | Armor | Magic resistance | Shield |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 32.8985 | 500 | 500 | 0 |
| 6 | 107.5651 | 200 | 200 | 0 |
| 12 | 369.9741 | 50 | 50 | 0 |

## Requested checkpoints

| Checkpoint | Weighted P70 DPS | Maximum DPS | TTK at P70 cutoff | Top-30% TTK P50 | Requested |
| --- | ---: | ---: | ---: | ---: | --- |
| Round 1 | 65.8 | 67.8 | 0.500s | 0.485s | P70 50-100, TTK 0.5s |
| Round 6 | 215.1 | 257.3 | 0.500s | 0.483s | P70 200-300, TTK 0.5s |
| Round 12 | 739.9 | 1088.6 | 0.500s | 0.469s | P70 600-800, max 1200-1400, TTK 0.5s |

## Feasibility result

- The P70 bands and 0.5-second continuous TTK are simultaneously met at rounds 1, 6 and 12.
- The round-12 maximum is only 1088.6, below the requested 1200 minimum.
- With armor/MR at least 50, raising target HP enough for max-HP damage to push the maximum above 1200 also raises P70 beyond 800 and continuous TTK far above 0.5 seconds.
- The fitted defenses decrease from 500 to 200 to 50. Therefore this exact band/TTK fit is incompatible with the earlier requirement that every target tier's HP, armor and MR all grow by at least 25%.
- Quantitatively, keeping Round 1 P70 at or below 100 with a 0.5-second TTK requires initial armor/MR of roughly 303 or higher; the 25% rule then forces Round 6 armor/MR to roughly 379 or higher.
- At that Round 6 defense, a 0.5-second TTK fit produces only about 130 P70 DPS. Reaching 200 P70 DPS requires roughly 7,475 HP and raises TTK to about 37 seconds.

## If Round 1 DPS is unconstrained

- Removing only the Round 1 DPS band makes the first transition feasible, but it does not make the full chain feasible.
- The strongest legal Round 6 boundary is about 124.426 armor/MR, 150 HP and exactly 300 P70 DPS at a 0.5-second P70 TTK.
- The 25% rule then requires Round 12 armor/MR of at least 155.533. At the 0.5-second P70 TTK solution, Round 12 has about 222.717 HP, 445.434 P70 DPS and 653.779 maximum DPS.
- Increasing Round 12 armor/MR beyond that minimum only lowers both DPS values. Therefore Round 12 cannot reach either the 600-800 P70 band or the 1200-1400 extreme band.
