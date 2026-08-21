# Locked-target comparison

This comparison keeps the revised upgrade model and fixes the three targets:

| Checkpoint | Target HP | Armor | Magic resistance | Shield |
| --- | ---: | ---: | ---: | ---: |
| R1 / round 1 | 150 | 50 | 50 | 0 |
| R2 / round 6 | 225 | 65 | 65 | 0 |
| R3 / round 12 | 360 | 80 | 80 | 0 |

The reported percentiles are weighted by ordered upgrade paths. P70 is the
cutoff for the best-performing 30% of paths. TTK is continuous `HP / DPS`, not
an attack-count or animation-frame TTK.

## Upgrade model

- +15% attack per attack stack.
- +2% target maximum-HP damage per max-HP-damage stack.
- +0.2 attacks per second per attack-speed stack.
- +20 percentage points crit chance per crit-chance stack.
- +0.3 crit multiplier per crit-damage stack.
- +20 flat dual penetration per flat-penetration stack.
- +10 percentage points percent dual penetration per percent-penetration stack.
- +0.1 inside the selected independent extra multiplier per extra-multiplier stack.

## Graduation initial configuration

Initial values: 100 attack, 25% crit chance, 1.8x crit multiplier,
`1.2 x 1.2 x 1.2` extra multipliers, 1.5 attacks/s, 30 flat dual
penetration and 10% percent dual penetration.

| Target | Weighted P50 DPS | Weighted P70 DPS | Weighted P90 DPS | Maximum DPS | TTK at P70 | TTK at maximum |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| R1 | 293.01 | 306.53 | 311.04 | 311.04 | 0.489s | 0.482s |
| R2 | 422.93 | 443.41 | 473.30 | 535.00 | 0.507s | 0.421s |
| R3 | 675.51 | 715.84 | 775.52 | 1023.10 | 0.503s | 0.352s |

## Reduced initial configuration

Initial values: 50 attack, 0% crit chance, 1.5x crit multiplier,
`1.0 x 1.0 x 1.0` extra multipliers, 1.0 attacks/s, no flat dual
penetration and no percent dual penetration.

| Target | Weighted P50 DPS | Weighted P70 DPS | Weighted P90 DPS | Maximum DPS | TTK at P70 | TTK at maximum |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| R1 | 36.67 | 36.67 | 38.46 | 40.00 | 4.091s | 3.750s |
| R2 | 54.50 | 57.50 | 61.57 | 76.67 | 3.913s | 2.935s |
| R3 | 93.64 | 99.64 | 107.88 | 145.00 | 3.613s | 2.483s |

## No-upgrade reference

These values apply each initial configuration directly to each target with no
round upgrades. They are references only; they are not part of the weighted
round distributions above.

| Initial configuration | Target | DPS | Continuous TTK |
| --- | --- | ---: | ---: |
| Graduation | R1 | 270.47 | 0.555s |
| Graduation | R2 | 242.05 | 0.930s |
| Graduation | R3 | 219.04 | 1.644s |
| Reduced | R1 | 33.33 | 4.500s |
| Reduced | R2 | 30.30 | 7.425s |
| Reduced | R3 | 27.78 | 12.960s |

## Maximum-DPS stack allocations

Stack order is: attack, max-HP damage, attack speed, crit chance, crit damage,
flat penetration, percent penetration, extra 1, extra 2, extra 3.

| Initial configuration | Target | One maximum-DPS allocation |
| --- | --- | --- |
| Graduation | R1 | `(1, 0, 0, 0, 0, 0, 0, 0, 0, 0)`; one flat-penetration stack ties it |
| Graduation | R2 | `(2, 0, 2, 1, 0, 1, 0, 0, 0, 0)` |
| Graduation | R3 | `(2, 0, 2, 3, 3, 2, 0, 0, 0, 0)` |
| Reduced | R1 | `(0, 0, 1, 0, 0, 0, 0, 0, 0, 0)` |
| Reduced | R2 | `(1, 0, 2, 0, 0, 3, 0, 0, 0, 0)` |
| Reduced | R3 | `(3, 0, 5, 0, 0, 4, 0, 0, 0, 0)` |
