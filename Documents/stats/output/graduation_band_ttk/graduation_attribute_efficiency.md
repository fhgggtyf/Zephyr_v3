# Graduation attribute efficiency

This comparison uses the corrected graduation formula and the strongest legal
25%-growth boundary targets:

- Round 6: 150 HP, 124.426 armor/MR.
- Round 12: 222.717 HP, 155.533 armor/MR.

Values are weighted over all legal upgrade paths. "Top-30% gain" measures the
same next upgrade only inside builds whose current DPS is at or above weighted
P70. "Best share" is the share of top-30% path weight where that upgrade ties for
the highest next-stack DPS gain.

| Upgrade | R6 mean gain | R12 mean gain | R12 top-30% gain | R12 top-30% best share |
| --- | ---: | ---: | ---: | ---: |
| +10% attack | 9.40% | 8.79% | 8.97% | 0.00% |
| +1% target max-HP damage | 1.41% | 1.96% | 2.00% | 0.00% |
| +0.2 attack speed | 12.44% | 11.65% | 11.31% | 4.75% |
| +20% crit chance | 14.41% | 14.49% | 12.68% | 35.04% |
| +30% crit multiplier | 7.98% | 9.13% | 10.45% | 10.20% |
| +20 flat dual penetration | 14.12% | 13.51% | 13.91% | 50.70% |
| +10% percent dual penetration | 8.36% | 10.25% | 10.65% | 1.17% |
| +10% extra multiplier 1 | 7.96% | 7.62% | 7.74% | 0.00% |
| +10% extra multiplier 2 | 7.96% | 7.62% | 7.74% | 0.00% |
| +10% extra multiplier 3 | 7.96% | 7.62% | 7.74% | 0.00% |

## Top-30% round-12 stack allocation

With ten symmetric choices, the uncontrolled average is 1.2 stacks per option.
The weighted top-30% builds instead average:

| Upgrade | All builds | Top-30% builds |
| --- | ---: | ---: |
| Attack percent | 1.200 | 1.137 |
| Max-HP damage | 1.200 | 0.432 |
| Attack speed | 1.200 | 1.461 |
| Crit chance | 1.200 | 1.796 |
| Crit multiplier | 1.200 | 1.245 |
| Flat dual penetration | 1.200 | 1.684 |
| Percent dual penetration | 1.200 | 1.289 |
| Extra multiplier 1 | 1.200 | 0.985 |
| Extra multiplier 2 | 1.200 | 0.985 |
| Extra multiplier 3 | 1.200 | 0.985 |

## Finding

- The severe underperformer is +1% target max-HP damage. The 0.5-second target
  forces target HP low, so one stack adds only 1.5-2.23 raw damage at the measured
  tiers. It contributes about one fifth of a normal upgrade's marginal value.
- Each extra multiplier upgrade is mildly under budget at roughly 7.6-8.0%.
  Because the three multipliers occupy three separate choices, together they
  materially dilute the P70 distribution even though none is individually awful.
- +10% attack is slightly below the 10-12% middle group and never becomes the
  best marginal choice in this target context.
- Flat penetration and crit chance dominate. They account for about 85.7% of
  top-30% best-next-choice weight at round 12. Attack speed is the next strongest
  general option.
- Percent penetration, crit multiplier and attack percent are broadly usable;
  they are not the main cause of the missing growth.
