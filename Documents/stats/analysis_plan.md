# Theoretical combat sensitivity groups

This plan is content-agnostic. Every value represents an abstract design parameter rather
than an existing weapon, enemy or game asset.

## Analysis method

1. One-variable sweep: change one parameter and keep every other value at baseline.
2. Context slice: repeat the same one-variable sweep under several fixed values of one direct
   interaction partner.
3. Pair heatmap: visualize only selected within-group interactions; do not calculate the full
   Cartesian product of all combat parameters.
4. Record raw damage, HP damage/hit, sustainable DPS, TTK, kills/minute and normalized
   reward/minute.

## Groups

| Group | Variables | Strong interactions |
| --- | --- | --- |
| Raw hit | Flat damage, target-max-HP damage, skill multiplier | Flat vs max-HP damage; raw damage × every later multiplier |
| Throughput | Attack frequency, duration, cooldown, uptime | Damage/hit × attack frequency; frequency × resources |
| Critical | Crit chance, crit multiplier, attack count | Crit chance × crit multiplier; crit stability × attack count |
| Backstab | Backstab uptime, backstab multiplier, target turning | Uptime × multiplier; backstab × crit |
| Extra multipliers | Category, element, rarity or other bonus groups | Same-category addition; cross-category multiplication |
| Mitigation | Defence, damage type, flat penetration, percentage penetration | Defence × fixed penetration; defence × percentage penetration |
| Shield | Shield amount, shield coefficient, damage/hit, attack frequency | Shield amount × coefficient; break threshold × hit size |
| Multi-target | Upper-semicircle AOE range, ground density, air density, target cap | Range × each density; density × target cap |
| Encounter | Target max HP/count, spawn rate, screen cap, movement time | Player DPS × max HP; spawn rate × screen cap; density × AOE |
| Resources | Cost, capacity, regeneration, one-second recovery delay | Cost × frequency; delay × sustainable DPS |
| Variance | Damage spread, crit randomness, sample count | Variance × hit count; crit chance × result distribution |

## Selected pair plots

- Flat damage × target-max-HP damage
- Crit chance × crit multiplier
- Defence × fixed penetration
- Defence × percentage penetration
- Attack frequency × resource cost
- AOE range × ground density (reference)
- AOE range × air density (reference)
- Shield amount × shield coefficient
- Backstab uptime × backstab multiplier

The AOE plots are explicitly reference-only. They use an upper semicircle, exclude underground
targets, and keep ground and airborne density separate; actual room geometry can make these
curves diverge substantially.

Reward/minute is a normalized translation of kill efficiency: one target grants one normalized
reward unit. It is not an economy model and can later be replaced by authored payout and drop
rules without changing the combat formulas.
