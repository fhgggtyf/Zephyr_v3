# Shield coefficient context analysis

This scan uses the theory-only combat model with the current resource rules.
Low/medium/high target maximum HP are 500/1000/2000; low/medium/high shield are 250/500/1000.
Shield coefficient values are 0 to 2 in steps of 0.25.
Burst TTK starts from full resources; sustainable TTK uses the steady-state resource cycle.

## Low HP (500 HP) × Low shield (250 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 24s | 34s | +0.00% | +0.00% |
| 0.25 | 16s | 26s | +50.00% | +30.77% |
| 0.5 | 10s | 20s | +140.00% | +70.00% |
| 0.75 | 9s | 18s | +166.67% | +88.89% |
| 1 | 8s | 16s | +200.00% | +112.50% |
| 1.25 | 7s | 14s | +242.86% | +142.86% |
| 1.5 | 7s | 14s | +242.86% | +142.86% |
| 1.75 | 7s | 14s | +242.86% | +142.86% |
| 2 | 7s | 14s | +242.86% | +142.86% |

## Low HP (500 HP) × Medium shield (500 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 24s | 34s | +0.00% | +0.00% |
| 0.25 | 32s | 42s | -25.00% | -19.05% |
| 0.5 | 18s | 28s | +33.33% | +21.43% |
| 0.75 | 14s | 24s | +71.43% | +41.67% |
| 1 | 10s | 20s | +140.00% | +70.00% |
| 1.25 | 9s | 18s | +166.67% | +88.89% |
| 1.5 | 9s | 18s | +166.67% | +88.89% |
| 1.75 | 8s | 16s | +200.00% | +112.50% |
| 2 | 8s | 16s | +200.00% | +112.50% |

## Low HP (500 HP) × High shield (1000 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 24s | 34s | +0.00% | +0.00% |
| 0.25 | 36s | 46s | -33.33% | -26.09% |
| 0.5 | 34s | 44s | -29.41% | -22.73% |
| 0.75 | 26s | 36s | -7.69% | -5.56% |
| 1 | 20s | 30s | +20.00% | +13.33% |
| 1.25 | 16s | 26s | +50.00% | +30.77% |
| 1.5 | 14s | 24s | +71.43% | +41.67% |
| 1.75 | 12s | 22s | +100.00% | +54.55% |
| 2 | 10s | 20s | +140.00% | +70.00% |

## Medium HP (1000 HP) × Low shield (250 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 58s | 68s | +0.00% | +0.00% |
| 0.25 | 26s | 36s | +123.08% | +88.89% |
| 0.5 | 20s | 30s | +190.00% | +126.67% |
| 0.75 | 18s | 28s | +222.22% | +142.86% |
| 1 | 16s | 26s | +262.50% | +161.54% |
| 1.25 | 14s | 24s | +314.29% | +183.33% |
| 1.5 | 14s | 24s | +314.29% | +183.33% |
| 1.75 | 14s | 24s | +314.29% | +183.33% |
| 2 | 14s | 24s | +314.29% | +183.33% |

## Medium HP (1000 HP) × Medium shield (500 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 58s | 68s | +0.00% | +0.00% |
| 0.25 | 42s | 52s | +38.10% | +30.77% |
| 0.5 | 28s | 38s | +107.14% | +78.95% |
| 0.75 | 24s | 34s | +141.67% | +100.00% |
| 1 | 20s | 30s | +190.00% | +126.67% |
| 1.25 | 18s | 28s | +222.22% | +142.86% |
| 1.5 | 18s | 28s | +222.22% | +142.86% |
| 1.75 | 16s | 26s | +262.50% | +161.54% |
| 2 | 16s | 26s | +262.50% | +161.54% |

## Medium HP (1000 HP) × High shield (1000 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 58s | 68s | +0.00% | +0.00% |
| 0.25 | 72s | 82s | -19.44% | -17.07% |
| 0.5 | 44s | 54s | +31.82% | +25.93% |
| 0.75 | 36s | 46s | +61.11% | +47.83% |
| 1 | 30s | 40s | +93.33% | +70.00% |
| 1.25 | 26s | 36s | +123.08% | +88.89% |
| 1.5 | 24s | 34s | +141.67% | +100.00% |
| 1.75 | 22s | 32s | +163.64% | +112.50% |
| 2 | 20s | 30s | +190.00% | +126.67% |

## High HP (2000 HP) × Low shield (250 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 124s | 134s | +0.00% | +0.00% |
| 0.25 | 46s | 56s | +169.57% | +139.29% |
| 0.5 | 40s | 50s | +210.00% | +168.00% |
| 0.75 | 38s | 48s | +226.32% | +179.17% |
| 1 | 36s | 46s | +244.44% | +191.30% |
| 1.25 | 34s | 44s | +264.71% | +204.55% |
| 1.5 | 34s | 44s | +264.71% | +204.55% |
| 1.75 | 34s | 44s | +264.71% | +204.55% |
| 2 | 34s | 44s | +264.71% | +204.55% |

## High HP (2000 HP) × Medium shield (500 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 124s | 134s | +0.00% | +0.00% |
| 0.25 | 62s | 72s | +100.00% | +86.11% |
| 0.5 | 48s | 58s | +158.33% | +131.03% |
| 0.75 | 44s | 54s | +181.82% | +148.15% |
| 1 | 40s | 50s | +210.00% | +168.00% |
| 1.25 | 38s | 48s | +226.32% | +179.17% |
| 1.5 | 38s | 48s | +226.32% | +179.17% |
| 1.75 | 36s | 46s | +244.44% | +191.30% |
| 2 | 36s | 46s | +244.44% | +191.30% |

## High HP (2000 HP) × High shield (1000 shield)

| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 124s | 134s | +0.00% | +0.00% |
| 0.25 | 92s | 102s | +34.78% | +31.37% |
| 0.5 | 64s | 74s | +93.75% | +81.08% |
| 0.75 | 56s | 66s | +121.43% | +103.03% |
| 1 | 50s | 60s | +148.00% | +123.33% |
| 1.25 | 46s | 56s | +169.57% | +139.29% |
| 1.5 | 44s | 54s | +181.82% | +148.15% |
| 1.75 | 42s | 52s | +195.24% | +157.69% |
| 2 | 40s | 50s | +210.00% | +168.00% |

## Observations

- With low shield, increasing ShieldCoeff beyond 1 often has little effect because the shield is broken quickly.
- With high shield, higher ShieldCoeff produces a much larger TTK reduction, especially for high-HP targets.
- A coefficient below 1 can be worse than coefficient 0: it reduces HP leakage while only slowly damaging the shield.
- The result is not monotonic in every low-coefficient region, so ShieldAmount and ShieldCoeff must be tested together.
