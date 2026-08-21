# Theoretical sensitivity analysis

This report uses only abstract parameters. No game asset, weapon, enemy or scene is referenced.

## Baseline assumptions

- Raw damage: 100 flat damage + 0% target-max-HP damage.
- Target: 1,000 maximum HP, evaluated from full health, zero defense, zero shield.
- Attack rate: 1 hit/s, 100% uptime, one target.
- Crit: 0% chance, 150% multiplier; backstab uptime 0%, 150% multiplier.
- Resource: 10 cost, 100 capacity, 10 regeneration/s, 1 s delay after the last spend.
- Reward: one normalized unit per kill.

Baseline raw damage: **100.00**
Baseline sustainable DPS: **50.00**
Baseline burst TTK: **10.00s**
Baseline sustainable target TTK: **20.00s**
Baseline earnings/min: **3.00 normalized units**

## Interpretation

- Curves report earnings/minute relative to the baseline. This is a normalized kill-efficiency metric, not a game economy value.
- Each curve changes exactly one variable. The heatmaps are limited to selected within-group interactions.
- Critical and backstab results use expected multipliers; randomness and variance are intentionally excluded from this first deterministic pass.
- A zero baseline sensitivity means that the variable's interaction partner is inactive (for example, crit multiplier at 0% crit chance). Use context_slices.svg and interaction_heatmaps.svg for those variables.
- AOE curves are flat in the one-target baseline by design. AOE context slices and heatmaps use a fixed 20-target reference encounter and must not be read as room-accurate predictions.
- Sustainable attack rate uses the steady-state cycle: min(input rate, 1 / (recovery delay + resource cost / regeneration)).

## Largest median and maximum sensitivities

| Variable | Median absolute earnings delta | Maximum absolute earnings delta |
| --- | ---: | ---: |
| Flat damage | 42.9% | 100.0% |
| Max-HP damage | 42.9% | 100.0% |
| Skill multiplier | 66.7% | 150.0% |
| Attack frequency | 0.0% | 50.0% |
| Crit chance | 25.0% | 42.9% |
| Crit multiplier | 0.0% | 0.0% |
| Backstab uptime | 25.0% | 42.9% |
| Backstab multiplier | 0.0% | 0.0% |
| Target defense | 50.0% | 66.7% |
| Flat penetration | 0.0% | 0.0% |
| Percent penetration | 0.0% | 0.0% |
| Shield amount | 44.4% | 60.0% |
| Shield coefficient | 0.0% | 0.0% |
| Resource cost | 42.9% | 100.0% |
| Target max HP | 52.4% | 233.3% |
| Ground density (reference) | 0.0% | 0.0% |
| Air density (reference) | 0.0% | 0.0% |
| AOE range | 0.0% | 0.0% |
| Reward per kill | 400.0% | 900.0% |

## Next analysis pass

1. Add stochastic Monte Carlo runs for crit and damage variance.
2. Add resource burst versus sustained-output plots.
3. Add a separate encounter model for spawn rate, density and reward-per-kill.
