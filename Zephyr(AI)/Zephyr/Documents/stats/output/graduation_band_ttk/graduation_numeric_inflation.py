"""Graduation-build numeric inflation analysis.

This model intentionally has no generic skill multiplier. All independently
upgradeable percentages stack additively inside their own stat, while the three
named extra multipliers remain three separate multiplicative factors.
"""

from __future__ import annotations

import csv
import math
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "output" / "graduation_band_ttk"

ATTR_KEYS = (
    "attack_percent",
    "max_hp_damage",
    "attack_speed",
    "crit_chance",
    "crit_damage",
    "flat_pen",
    "percent_pen",
    "extra_1",
    "extra_2",
    "extra_3",
)
ATTR_LABELS = {
    "attack_percent": "+10% attack",
    "max_hp_damage": "+1% target max-HP damage",
    "attack_speed": "+0.2 attack speed",
    "crit_chance": "+20% crit chance",
    "crit_damage": "+30% crit multiplier",
    "flat_pen": "+20 flat dual penetration",
    "percent_pen": "+10% percent dual penetration",
    "extra_1": "+10% extra multiplier 1",
    "extra_2": "+10% extra multiplier 2",
    "extra_3": "+10% extra multiplier 3",
}
STATE_ZERO = (0,) * len(ATTR_KEYS)


@dataclass(frozen=True)
class TargetConfig:
    max_hp: float
    armor: float
    magic_resistance: float
    shield: float = 0.0


# These target values solve HP / weighted-P70-DPS = 0.5 at rounds 1, 6 and 12.
# They satisfy the three P70 bands, but the round-12 maximum remains below the
# requested 1200-1400 extreme band. The generated report records that conflict.
TARGET_SCHEDULE: tuple[tuple[int, TargetConfig], ...] = (
    (0, TargetConfig(32.8984615385, 500.0, 500.0)),
    (6, TargetConfig(107.56512, 200.0, 200.0)),
    (12, TargetConfig(369.9741298183, 50.0, 50.0)),
)


def target_at(round_index: int) -> TargetConfig:
    active = TARGET_SCHEDULE[0][1]
    for start, target in TARGET_SCHEDULE:
        if start > round_index:
            break
        active = target
    return active


def apply_increment(state: tuple[int, ...], attribute_index: int) -> tuple[int, ...]:
    values = list(state)
    values[attribute_index] += 1
    return tuple(values)


def expand_states(states: dict[tuple[int, ...], int]) -> dict[tuple[int, ...], int]:
    expanded: defaultdict[tuple[int, ...], int] = defaultdict(int)
    for state, path_count in states.items():
        for attribute_index in range(len(ATTR_KEYS)):
            expanded[apply_increment(state, attribute_index)] += path_count
    return dict(expanded)


def state_stats(state: tuple[int, ...], target: TargetConfig) -> dict[str, float]:
    attack_pct, hp_pct, speed, crit_chance, crit_damage, flat_pen, pct_pen, extra_1, extra_2, extra_3 = state

    attack = 100.0 * (1.0 + 0.10 * attack_pct)
    max_hp_damage = target.max_hp * 0.01 * hp_pct
    extra_multiplier_1 = 1.20 + 0.10 * extra_1
    extra_multiplier_2 = 1.20 + 0.10 * extra_2
    extra_multiplier_3 = 1.20 + 0.10 * extra_3
    chance = min(1.0, 0.25 + 0.20 * crit_chance)
    crit_multiplier = 1.80 + 0.30 * crit_damage
    expected_crit_factor = 1.0 + chance * (crit_multiplier - 1.0)

    raw_damage = (
        (attack + max_hp_damage)
        * extra_multiplier_1
        * extra_multiplier_2
        * extra_multiplier_3
        * expected_crit_factor
    )

    flat_dual_pen = 30.0 + 20.0 * flat_pen
    percent_dual_pen = min(1.0, 0.10 + 0.10 * pct_pen)
    effective_armor = max(0.0, target.armor * (1.0 - percent_dual_pen) - flat_dual_pen)
    effective_magic_resistance = max(
        0.0,
        target.magic_resistance * (1.0 - percent_dual_pen) - flat_dual_pen,
    )
    armor_factor = 100.0 / (100.0 + effective_armor)
    magic_factor = 100.0 / (100.0 + effective_magic_resistance)
    mitigation = 0.5 * (armor_factor + magic_factor)

    post_mitigation_damage = raw_damage * mitigation
    attack_rate = 1.50 + 0.20 * speed
    expected_dps = post_mitigation_damage * attack_rate
    continuous_ttk = target.max_hp / expected_dps if expected_dps > 1e-12 else math.inf

    return {
        "attack": attack,
        "max_hp_damage": max_hp_damage,
        "raw_damage": raw_damage,
        "post_mitigation_damage": post_mitigation_damage,
        "attack_rate": attack_rate,
        "expected_dps": expected_dps,
        "ttk_seconds": continuous_ttk,
        "flat_dual_pen": flat_dual_pen,
        "percent_dual_pen": percent_dual_pen,
        "effective_armor": effective_armor,
        "effective_magic_resistance": effective_magic_resistance,
    }


def weighted_quantile(values: list[tuple[float, int]], quantile: float) -> float:
    ordered = sorted(values, key=lambda item: item[0])
    total = sum(weight for _, weight in ordered)
    threshold = total * quantile
    accumulated = 0
    for value, weight in ordered:
        accumulated += weight
        if accumulated >= threshold:
            return value
    return ordered[-1][0]


def build_states() -> dict[int, dict[tuple[int, ...], int]]:
    states_by_round = {0: {STATE_ZERO: 1}}
    for round_index in range(1, 13):
        states_by_round[round_index] = expand_states(states_by_round[round_index - 1])
    return states_by_round


def write_outputs(states_by_round: dict[int, dict[tuple[int, ...], int]]) -> None:
    round_rows: list[dict[str, object]] = []
    for round_index, states in states_by_round.items():
        target = target_at(round_index)
        samples = [
            (state_stats(state, target)["expected_dps"], state_stats(state, target)["ttk_seconds"], weight)
            for state, weight in states.items()
        ]
        dps_p70 = weighted_quantile([(dps, weight) for dps, _, weight in samples], 0.70)
        top_samples = [(ttk, weight) for dps, ttk, weight in samples if dps >= dps_p70 - 1e-12]
        round_rows.append({
            "round": round_index,
            "target_max_hp": target.max_hp,
            "target_armor": target.armor,
            "target_magic_resistance": target.magic_resistance,
            "target_shield": target.shield,
            "path_count": sum(states.values()),
            "state_count": len(states),
            "dps_p50": weighted_quantile([(dps, weight) for dps, _, weight in samples], 0.50),
            "dps_p70": dps_p70,
            "dps_p90": weighted_quantile([(dps, weight) for dps, _, weight in samples], 0.90),
            "dps_max": max(dps for dps, _, _ in samples),
            "ttk_at_dps_p70": target.max_hp / dps_p70,
            "top_30_ttk_p50": weighted_quantile(top_samples, 0.50),
            "top_30_ttk_min": min(ttk for ttk, _ in top_samples),
            "top_30_ttk_max": max(ttk for ttk, _ in top_samples),
        })

    with (OUTPUT / "graduation_rounds.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(round_rows[0].keys()))
        writer.writeheader()
        writer.writerows(round_rows)

    final_rows: list[dict[str, object]] = []
    final_target = target_at(12)
    for state, path_count in states_by_round[12].items():
        stats = state_stats(state, final_target)
        row = {f"{key}_stacks": state[index] for index, key in enumerate(ATTR_KEYS)}
        row.update({
            "path_count": path_count,
            "expected_dps": stats["expected_dps"],
            "ttk_seconds": stats["ttk_seconds"],
        })
        final_rows.append(row)
    with (OUTPUT / "graduation_final_states.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(final_rows[0].keys()))
        writer.writeheader()
        writer.writerows(final_rows)

    selected = {int(row["round"]): row for row in round_rows}
    lines = [
        "# Graduation numeric-inflation analysis",
        "",
        "No generic skill multiplier is present. Independently upgradeable percentages add inside their own stat.",
        "The three named extra multipliers multiply each other, while upgrades within one named multiplier add to that multiplier.",
        "Percent dual penetration is additive: 10% initially, then +10 percentage points per stack. Percentage penetration is applied before flat penetration.",
        "TTK is continuous HP / expected DPS so a 0.5-second target is representable independently of individual attack frames.",
        "",
        "## Raw-damage formula",
        "",
        "`((Attack x (1 + attack bonus)) + TargetMaxHP x max-HP-damage%) x Extra1 x Extra2 x Extra3 x ExpectedCritFactor`",
        "",
        "## Target tiers",
        "",
        "| Starts at round | HP | Armor | Magic resistance | Shield |",
        "| ---: | ---: | ---: | ---: | ---: |",
    ]
    for start, target in TARGET_SCHEDULE:
        lines.append(f"| {start} | {target.max_hp:.4f} | {target.armor:g} | {target.magic_resistance:g} | 0 |")
    lines.extend([
        "",
        "## Requested checkpoints",
        "",
        "| Checkpoint | Weighted P70 DPS | Maximum DPS | TTK at P70 cutoff | Top-30% TTK P50 | Requested |",
        "| --- | ---: | ---: | ---: | ---: | --- |",
        f"| Round 1 | {float(selected[1]['dps_p70']):.1f} | {float(selected[1]['dps_max']):.1f} | {float(selected[1]['ttk_at_dps_p70']):.3f}s | {float(selected[1]['top_30_ttk_p50']):.3f}s | P70 50-100, TTK 0.5s |",
        f"| Round 6 | {float(selected[6]['dps_p70']):.1f} | {float(selected[6]['dps_max']):.1f} | {float(selected[6]['ttk_at_dps_p70']):.3f}s | {float(selected[6]['top_30_ttk_p50']):.3f}s | P70 200-300, TTK 0.5s |",
        f"| Round 12 | {float(selected[12]['dps_p70']):.1f} | {float(selected[12]['dps_max']):.1f} | {float(selected[12]['ttk_at_dps_p70']):.3f}s | {float(selected[12]['top_30_ttk_p50']):.3f}s | P70 600-800, max 1200-1400, TTK 0.5s |",
        "",
        "## Feasibility result",
        "",
        "- The P70 bands and 0.5-second continuous TTK are simultaneously met at rounds 1, 6 and 12.",
        f"- The round-12 maximum is only {float(selected[12]['dps_max']):.1f}, below the requested 1200 minimum.",
        "- With armor/MR at least 50, raising target HP enough for max-HP damage to push the maximum above 1200 also raises P70 beyond 800 and continuous TTK far above 0.5 seconds.",
        "- The fitted defenses decrease from 500 to 200 to 50. Therefore this exact band/TTK fit is incompatible with the earlier requirement that every target tier's HP, armor and MR all grow by at least 25%.",
        "- Quantitatively, keeping Round 1 P70 at or below 100 with a 0.5-second TTK requires initial armor/MR of roughly 303 or higher; the 25% rule then forces Round 6 armor/MR to roughly 379 or higher.",
        "- At that Round 6 defense, a 0.5-second TTK fit produces only about 130 P70 DPS. Reaching 200 P70 DPS requires roughly 7,475 HP and raises TTK to about 37 seconds.",
        "",
        "## If Round 1 DPS is unconstrained",
        "",
        "- Removing only the Round 1 DPS band makes the first transition feasible, but it does not make the full chain feasible.",
        "- The strongest legal Round 6 boundary is about 124.426 armor/MR, 150 HP and exactly 300 P70 DPS at a 0.5-second P70 TTK.",
        "- The 25% rule then requires Round 12 armor/MR of at least 155.533. At the 0.5-second P70 TTK solution, Round 12 has about 222.717 HP, 445.434 P70 DPS and 653.779 maximum DPS.",
        "- Increasing Round 12 armor/MR beyond that minimum only lowers both DPS values. Therefore Round 12 cannot reach either the 600-800 P70 band or the 1200-1400 extreme band.",
    ])
    (OUTPUT / "graduation_summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    global OUTPUT
    if len(sys.argv) > 1:
        OUTPUT = Path(sys.argv[1]).resolve()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    states_by_round = build_states()
    write_outputs(states_by_round)
    print(f"Wrote graduation analysis to {OUTPUT}")


if __name__ == "__main__":
    main()
