"""Theory-only sequential stat-stacking and numeric-inflation analysis.

Each round chooses one available offensive increment. The six increments are:
flat attack, max-HP damage, skill multiplier, attack frequency, crit chance and
crit multiplier. Crit multiplier is unavailable until crit chance has appeared.

Resources are deliberately excluded. The target's armor, magic resistance, HP and
shield are inflated at rounds 8, 11 and 14 so the resulting DPS bands can be
checked against the requested tuning targets.
"""

from __future__ import annotations

import csv
import math
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "output"

ATTR_KEYS = ("flat", "percent", "skill", "speed", "crit_chance", "crit_damage")
ATTR_LABELS = {
    "flat": "Flat attack",
    "percent": "Max-HP damage",
    "skill": "Skill multiplier",
    "speed": "Attack frequency",
    "crit_chance": "Crit chance",
    "crit_damage": "Crit multiplier",
}
STATE_ZERO = (0, 0, 0, 0, 0, 0)


@dataclass(frozen=True)
class TargetConfig:
    max_hp: float
    armor: float
    magic_resistance: float
    shield: float


# Tuned reference target. The three later rows are the requested defensive
# inflation points. Armor and MR are equal so the fixed attacker can be treated as
# a 50/50 physical/magical damage split without privileging one damage type.
TARGET_SCHEDULE: tuple[tuple[int, TargetConfig], ...] = (
    (0, TargetConfig(150.0, 1.0, 1.0, 100.0)),
    (8, TargetConfig(187.5, 15.0, 15.0, 200.0)),
    (11, TargetConfig(262.5, 19.0, 19.0, 300.0)),
    (14, TargetConfig(337.5, 25.0, 25.0, 400.0)),
)


def target_at(round_index: int) -> TargetConfig:
    active = TARGET_SCHEDULE[0][1]
    for start, config in TARGET_SCHEDULE:
        if start > round_index:
            break
        active = config
    return active


def available_attributes(state: tuple[int, ...]) -> tuple[int, ...]:
    # Crit multiplier is not a valid choice until at least one crit chance stack
    # exists, matching the requested conditional rule.
    if state[4] > 0:
        return tuple(range(6))
    return tuple(range(5))


def apply_increment(state: tuple[int, ...], attribute_index: int) -> tuple[int, ...]:
    values = list(state)
    values[attribute_index] += 1
    return tuple(values)


def state_stats(state: tuple[int, ...], target: TargetConfig) -> dict[str, float]:
    flat, percent, skill, speed, crit_chance, crit_damage = state
    flat_damage = 100.0 + flat * 10.0
    percent_damage = percent * 0.01
    skill_multiplier = 1.0 + skill * 0.10
    attack_rate = 1.0 + speed * 0.10
    chance = min(1.0, crit_chance * 0.20)
    multiplier = 1.50 + crit_damage * 0.30
    crit_factor = 1.0 + chance * (multiplier - 1.0)
    raw_damage = (flat_damage + target.max_hp * percent_damage) * skill_multiplier * crit_factor

    armor_factor = 100.0 / (100.0 + max(0.0, target.armor))
    magic_factor = 100.0 / (100.0 + max(0.0, target.magic_resistance))
    # The fixed attacker is half physical and half magical. Equal armor/MR keeps
    # this equivalent to one generic defense value while retaining both stats.
    mitigation = 0.5 * (armor_factor + magic_factor)
    post_mitigation_damage = raw_damage * mitigation
    expected_dps = post_mitigation_damage * attack_rate

    effective_health = target.max_hp + max(0.0, target.shield)
    if post_mitigation_damage <= 1e-12:
        ttk = math.inf
    else:
        hits = max(1, math.ceil(effective_health / post_mitigation_damage - 1e-9))
        ttk = hits / max(attack_rate, 1e-12)
    return {
        "raw_damage": raw_damage,
        "mitigation": mitigation,
        "post_mitigation_damage": post_mitigation_damage,
        "expected_dps": expected_dps,
        "ttk_seconds": ttk,
        "attack_rate": attack_rate,
    }


def expand_states(round_index: int, states: dict[tuple[int, ...], int]) -> dict[tuple[int, ...], int]:
    expanded: defaultdict[tuple[int, ...], int] = defaultdict(int)
    for state, path_count in states.items():
        for attribute_index in available_attributes(state):
            expanded[apply_increment(state, attribute_index)] += path_count
    return dict(expanded)


def weighted_quantile(values: Iterable[tuple[float, int]], quantile: float) -> float:
    ordered = sorted(values, key=lambda item: item[0])
    total = sum(weight for _, weight in ordered)
    target = total * quantile
    accumulated = 0
    for value, weight in ordered:
        accumulated += weight
        if accumulated >= target:
            return value
    return ordered[-1][0]


def write_round_outputs() -> tuple[dict[int, dict[tuple[int, ...], int]], list[dict[str, object]]]:
    states_by_round: dict[int, dict[tuple[int, ...], int]] = {0: {STATE_ZERO: 1}}
    for round_index in range(1, 16):
        states_by_round[round_index] = expand_states(round_index, states_by_round[round_index - 1])

    round_rows: list[dict[str, object]] = []
    for round_index, states in states_by_round.items():
        target = target_at(round_index)
        dps_values = [(state_stats(state, target)["expected_dps"], count) for state, count in states.items()]
        ttk_values = [(state_stats(state, target)["ttk_seconds"], count) for state, count in states.items()]
        dps_p70 = weighted_quantile(dps_values, 0.70)
        top_30_ttk_values = [
            (state_stats(state, target)["ttk_seconds"], count)
            for state, count in states.items()
            if state_stats(state, target)["expected_dps"] >= dps_p70 - 1e-12
        ]
        row = {
            "round": round_index,
            "target_max_hp": target.max_hp,
            "target_armor": target.armor,
            "target_magic_resistance": target.magic_resistance,
            "target_shield": target.shield,
            "path_count": sum(states.values()),
            "state_count": len(states),
            "dps_p50": weighted_quantile(dps_values, 0.50),
            "dps_p70": dps_p70,
            "dps_p90": weighted_quantile(dps_values, 0.90),
            "dps_max": max(value for value, _ in dps_values),
            "ttk_p50": weighted_quantile(ttk_values, 0.50),
            "ttk_p70": weighted_quantile(ttk_values, 0.70),
            "ttk_p90": weighted_quantile(ttk_values, 0.90),
            "ttk_min": min(value for value, _ in ttk_values),
            "top_30_ttk_p50": weighted_quantile(top_30_ttk_values, 0.50),
            "top_30_ttk_p70": weighted_quantile(top_30_ttk_values, 0.70),
            "top_30_ttk_min": min(value for value, _ in top_30_ttk_values),
            "top_30_ttk_max": max(value for value, _ in top_30_ttk_values),
        }
        round_rows.append(row)

    with (OUTPUT / "numeric_inflation_rounds.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(round_rows[0].keys()))
        writer.writeheader()
        writer.writerows(round_rows)
    return states_by_round, round_rows


def write_marginal_outputs(states_by_round: dict[int, dict[tuple[int, ...], int]]) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for round_index in range(1, 16):
        before_target = target_at(round_index - 1)
        after_target = target_at(round_index)
        accum: dict[int, dict[str, float]] = {
            index: {"weight": 0.0, "gain_sum": 0.0, "best_weight": 0.0}
            for index in range(6)
        }
        for state, path_count in states_by_round[round_index - 1].items():
            before = state_stats(state, before_target)["ttk_seconds"]
            candidates: list[tuple[int, float]] = []
            for attribute_index in available_attributes(state):
                after_state = apply_increment(state, attribute_index)
                after = state_stats(after_state, after_target)["ttk_seconds"]
                gain = before / after - 1.0 if math.isfinite(before) and math.isfinite(after) and after > 0 else -1.0
                candidates.append((attribute_index, gain))
                entry = accum[attribute_index]
                entry["weight"] += path_count
                entry["gain_sum"] += gain * path_count
            best_gain = max(gain for _, gain in candidates)
            for attribute_index, gain in candidates:
                if abs(gain - best_gain) <= 1e-12:
                    accum[attribute_index]["best_weight"] += path_count

        for attribute_index, entry in accum.items():
            if entry["weight"] <= 0:
                continue
            rows.append({
                "round": round_index,
                "attribute": ATTR_KEYS[attribute_index],
                "label": ATTR_LABELS[ATTR_KEYS[attribute_index]],
                "mean_ttk_gain_percent": entry["gain_sum"] / entry["weight"] * 100.0,
                "best_choice_share_percent": entry["best_weight"] / entry["weight"] * 100.0,
                "available_path_weight": entry["weight"],
            })

    with (OUTPUT / "numeric_inflation_marginals.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    return rows


def write_final_states(states_by_round: dict[int, dict[tuple[int, ...], int]]) -> None:
    target = target_at(15)
    rows: list[dict[str, object]] = []
    for state, path_count in states_by_round[15].items():
        stats = state_stats(state, target)
        rows.append({
            "flat_stacks": state[0],
            "percent_stacks": state[1],
            "skill_stacks": state[2],
            "speed_stacks": state[3],
            "crit_chance_stacks": state[4],
            "crit_damage_stacks": state[5],
            "all_attributes_present": min(state) > 0,
            "path_count": path_count,
            "expected_dps": stats["expected_dps"],
            "ttk_seconds": stats["ttk_seconds"],
        })
    with (OUTPUT / "numeric_inflation_final_states.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)


def write_summary(round_rows: list[dict[str, object]], states_by_round: dict[int, dict[tuple[int, ...], int]],
                  marginal_rows: list[dict[str, object]]) -> None:
    initial_target = target_at(0)
    final_target = target_at(15)
    all_attribute_values = [
        (state_stats(state, final_target)["expected_dps"], count)
        for state, count in states_by_round[15].items()
        if min(state) > 0
    ]
    all_attribute_p70 = weighted_quantile(all_attribute_values, 0.70)
    all_attribute_max = max(value for value, _ in all_attribute_values)
    final_marginals = [row for row in marginal_rows if row["round"] == 15]
    anchor_rows = [next(row for row in round_rows if row["round"] == anchor) for anchor in (0, 8, 11, 14)]
    anchor_ttk = [float(row["top_30_ttk_p50"]) for row in anchor_rows]
    final_percent_increment = final_target.max_hp * 0.01
    final_percent_relation = "stronger" if final_percent_increment > 10.0 else "weaker"
    lines = [
        "# Numeric inflation analysis",
        "",
        "Resources are excluded. Each round chooses one available offensive increment; crit multiplier is unavailable until crit chance has already been added.",
        "The six increments are +10 flat attack, +1% target-max-HP damage, +10% skill multiplier, +0.1 attack frequency, +20% crit chance and +30% crit multiplier.",
        "",
        "## Defensive target schedule",
        "",
        "| Round | Max HP | Armor | Magic resistance | Shield |",
        "| ---: | ---: | ---: | ---: | ---: |",
    ]
    for start, target in TARGET_SCHEDULE:
        lines.append(f"| {start} | {target.max_hp:g} | {target.armor:g} | {target.magic_resistance:g} | {target.shield:g} |")
    lines.extend([
        "",
        "Armor and magic resistance are equal. The fixed attacker is modeled as a 50/50 physical/magical split, so both defenses participate.",
        "TTK is discrete by hit count but has no resource delays. DPS is expected post-mitigation damage per second before the target is defeated.",
        "",
        "## Round distribution",
        "",
        "| Round | DPS P50 | DPS P70 (top-30% cutoff) | DPS P90 | DPS max | Top-30% TTK P50 | Top-30% TTK range |",
        "| ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
    ])
    for row in round_rows:
        lines.append(
            f"| {row['round']} | {float(row['dps_p50']):.1f} | {float(row['dps_p70']):.1f} | "
            f"{float(row['dps_p90']):.1f} | {float(row['dps_max']):.1f} | "
            f"{float(row['top_30_ttk_p50']):.3g}s | "
            f"{float(row['top_30_ttk_min']):.3g}-{float(row['top_30_ttk_max']):.3g}s |"
        )
    final = next(row for row in round_rows if row["round"] == 15)
    lines.extend([
        "",
        "## Target-band check",
        "",
        f"- Initial DPS: {float(round_rows[0]['dps_p50']):.1f} (requested reference band: 50-100).",
        f"- Round 8 P70 DPS: {float(next(row for row in round_rows if row['round'] == 8)['dps_p70']):.1f} (requested reference band: 200-300).",
        f"- Round 15 P70 DPS: {float(final['dps_p70']):.1f} (requested reference band: 600-800).",
        f"- Round 15 maximum DPS: {float(final['dps_max']):.1f} (requested reference band: 1200-1400).",
        f"- Restricting the final sample to builds where all six attack attributes appear at least once gives a round-15 P70 of {all_attribute_p70:.1f} DPS and a maximum of {all_attribute_max:.1f} DPS.",
        "- The unrestricted maximum is higher because a concentrated build can omit whichever increments are weak at the current target HP; this is the first sign that the increments are not mutually balanced.",
        "",
        "## Top-30% TTK consistency",
        "",
        "Top-30% means states at or above the weighted DPS P70 cutoff. TTK is measured on that same DPS-selected cohort, so this is not an independent TTK percentile.",
        "",
        "| Anchor round | Target HP | Top-30% TTK P50 | Top-30% TTK range |",
        "| ---: | ---: | ---: | ---: |",
    ])
    for row in anchor_rows:
        lines.append(
            f"| {row['round']} | {float(row['target_max_hp']):g} | {float(row['top_30_ttk_p50']):.3g}s | "
            f"{float(row['top_30_ttk_min']):.3g}-{float(row['top_30_ttk_max']):.3g}s |"
        )
    lines.extend([
        f"- Across the four anchor rounds, the top-30% TTK median spans {min(anchor_ttk):.3g}-{max(anchor_ttk):.3g}s.",
        "",
        "## Marginal boundaries",
        "",
        "- Flat attack versus max-HP damage: +1% max-HP damage is larger than +10 flat attack whenever TargetMaxHP > 1000. "
        f"At the initial {initial_target.max_hp:g} HP it adds {initial_target.max_hp * 0.01:g} raw damage and is weaker than +10 flat attack; "
        f"at the final {final_target.max_hp:g} HP it adds {final_percent_increment:g} raw damage and remains {final_percent_relation} than +10 flat attack.",
        "- Skill multiplier versus attack frequency: both are multiplicative and their next-stack value is approximately 0.1/current multiplier. Invest in the lower current multiplier.",
        "- Crit chance versus crit multiplier: with an uncapped +20% chance step, crit multiplier is better when `0.3 x CritChance > 0.2 x (CritMultiplier - 1)`. Once chance is capped, only crit multiplier can continue to grow.",
        "- At rounds 8, 11 and 14, the reported TTK gain includes the target defense/HP/shield inflation, so a negative or unusually small gain means the defensive spike absorbed the offensive stack.",
        "",
        "## Round-15 next-stack comparison",
        "",
        "| Attribute | Mean TTK gain | Share of available paths where tied for best |",
        "| --- | ---: | ---: |",
    ])
    for row in final_marginals:
        lines.append(
            f"| {row['label']} | {float(row['mean_ttk_gain_percent']):.2f}% | "
            f"{float(row['best_choice_share_percent']):.2f}% |"
        )
    lines.extend([
        "",
        "## Compatibility finding",
        "",
        "- This HP-scaled rerun is a controlled comparison. It does not retune the offensive increments or defenses to force the previous absolute DPS bands.",
        f"- At the initial {initial_target.max_hp:g} HP, +1% max-HP damage adds only {initial_target.max_hp * 0.01:g} raw damage while +10 flat attack adds 10. Percent damage is therefore an intentionally weak early choice in this run.",
        f"- At the final {final_target.max_hp:g} HP, +1% max-HP damage adds {final_percent_increment:g} raw damage while +10 flat attack still adds 10, so it is {final_percent_relation} in this scaled run.",
        "- Keeping +1% max-HP damage and +10 flat attack equal requires a target at 1000 HP.",
    ])
    (OUTPUT / "numeric_inflation_summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    global OUTPUT
    if len(sys.argv) > 1:
        OUTPUT = Path(sys.argv[1]).resolve()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    states_by_round, round_rows = write_round_outputs()
    marginal_rows = write_marginal_outputs(states_by_round)
    write_final_states(states_by_round)
    write_summary(round_rows, states_by_round, marginal_rows)
    print(f"Wrote numeric inflation outputs to {OUTPUT}")


if __name__ == "__main__":
    main()
