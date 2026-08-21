# Equal-TTK increment analysis

Target benefit: `TTK_before / TTK_after - 1 = 10%`.

- Burst TTK starts with a full resource pool and is the primary target-dummy comparison.
- Sustainable TTK excludes the initial full-resource burst and uses the steady-state resource cycle.
- Discrete hit counts mean many damage variables can only reach 11.11%, not exactly 10%.
- A missing match means the variable cannot improve that TTK under the current baseline and scan range.

## Burst TTK

| Variable | Baseline | Test value | Delta | Delta % | TTK before | TTK after | Benefit | Status |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Flat damage | 100 | 111.12 | +11.12 | 11.120% | 10s | 9s | 11.111% | matched |
| Max-HP damage | 0 | 0.011115 | +0.011115 | n/a | 10s | 9s | 11.111% | matched |
| Skill multiplier | 1 | 1.11112 | +0.111125 | 11.112% | 10s | 9s | 11.111% | matched |
| Attack frequency | 1 | 1.10003 | +0.100025 | 10.003% | 10s | 9.091s | 10.003% | matched |
| Crit chance | 0 | 0.22225 | +0.22225 | n/a | 10s | 9s | 11.111% | matched |
| Crit multiplier | 1.5 | — | — | — | 10s | — | — | no positive gain |
| Backstab uptime | 0 | 0.22225 | +0.22225 | n/a | 10s | 9s | 11.111% | matched |
| Backstab multiplier | 1.5 | — | — | — | 10s | — | — | no positive gain |
| Target defense | 0 | — | — | — | 10s | — | — | no positive gain |
| Flat penetration | 0 | — | — | — | 10s | — | — | no positive gain |
| Percent penetration | 0 | — | — | — | 10s | — | — | no positive gain |
| Shield amount | 0 | — | — | — | 10s | — | — | no positive gain |
| Shield coefficient | 1 | — | — | — | 10s | — | — | no positive gain |
| Resource cost | 10 | — | — | — | 10s | — | — | no positive gain |
| Target max HP | 1000 | 899.962 | -100.038 | -10.004% | 10s | 9s | 11.111% | matched |
| Ground density (reference) | 1 | — | — | — | 10s | — | — | no positive gain |
| Air density (reference) | 0.35 | — | — | — | 10s | — | — | no positive gain |
| AOE range | 1 | — | — | — | 10s | — | — | no positive gain |
| Reward per kill | 1 | — | — | — | 10s | — | — | no positive gain |

## Sustainable TTK

| Variable | Baseline | Test value | Delta | Delta % | TTK before | TTK after | Benefit | Status |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Flat damage | 100 | 111.12 | +11.12 | 11.120% | 20s | 18s | 11.111% | matched |
| Max-HP damage | 0 | 0.011115 | +0.011115 | n/a | 20s | 18s | 11.111% | matched |
| Skill multiplier | 1 | 1.11112 | +0.111125 | 11.112% | 20s | 18s | 11.111% | matched |
| Attack frequency | 1 | — | — | — | 20s | — | — | no positive gain |
| Crit chance | 0 | 0.22225 | +0.22225 | n/a | 20s | 18s | 11.111% | matched |
| Crit multiplier | 1.5 | — | — | — | 20s | — | — | no positive gain |
| Backstab uptime | 0 | 0.22225 | +0.22225 | n/a | 20s | 18s | 11.111% | matched |
| Backstab multiplier | 1.5 | — | — | — | 20s | — | — | no positive gain |
| Target defense | 0 | — | — | — | 20s | — | — | no positive gain |
| Flat penetration | 0 | — | — | — | 20s | — | — | no positive gain |
| Percent penetration | 0 | — | — | — | 20s | — | — | no positive gain |
| Shield amount | 0 | — | — | — | 20s | — | — | no positive gain |
| Shield coefficient | 1 | — | — | — | 20s | — | — | no positive gain |
| Resource cost | 10 | 8.182 | -1.818 | -18.180% | 20s | 18.18s | 9.999% | matched |
| Target max HP | 1000 | 899.962 | -100.038 | -10.004% | 20s | 18s | 11.111% | matched |
| Ground density (reference) | 1 | — | — | — | 20s | — | — | no positive gain |
| Air density (reference) | 0.35 | — | — | — | 20s | — | — | no positive gain |
| AOE range | 1 | — | — | — | 20s | — | — | no positive gain |
| Reward per kill | 1 | — | — | — | 20s | — | — | no positive gain |
