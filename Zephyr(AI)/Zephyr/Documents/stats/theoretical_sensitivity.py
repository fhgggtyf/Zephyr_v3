"""Theory-only combat sensitivity analysis.

This module intentionally does not import or reference any game asset.  It models a
generic attacker, a generic target and a fixed reward-per-kill so that the resulting
curves describe relationships between design variables rather than current content.

Run with the bundled Python runtime:
    python theoretical_sensitivity.py

Outputs are written to Documents/stats/output/:
    sensitivity_data.csv
    sensitivity_curves.svg
    context_slices.svg
    interaction_heatmaps.svg
    sensitivity_summary.md
"""

from __future__ import annotations

import csv
import html
import math
from dataclasses import asdict, dataclass, replace
from pathlib import Path
from typing import Callable, Iterable


ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / "output"


@dataclass(frozen=True)
class Model:
    # Raw-hit construction. Percent damage is a fraction of target maximum HP.
    # The baseline is 100 flat damage and 0% max-HP damage.
    flat_damage: float = 100.0
    percent_damage: float = 0.0
    skill_multiplier: float = 1.0
    attack_multiplier: float = 1.0

    # Throughput and positional uptime.
    attacks_per_second: float = 1.0
    attack_uptime: float = 1.0
    backstab_uptime: float = 0.0

    # Expected critical/backstab factors.
    crit_chance: float = 0.0
    crit_multiplier: float = 1.50
    backstab_multiplier: float = 1.50

    # Additional multiplicative categories.  Same-category bonuses have already
    # been summed; different categories multiply.
    category_bonus: float = 0.0
    element_bonus: float = 0.0
    rarity_bonus: float = 0.0

    # Defence and shield.
    target_defense: float = 0.0
    flat_penetration: float = 0.0
    percent_penetration: float = 0.0
    shield_amount: float = 0.0
    shield_coeff: float = 1.0

    # Encounter and reward assumptions.
    target_max_hp: float = 1000.0
    target_count: int = 1
    ground_density: float = 1.0
    air_density: float = 0.35
    aoe_range: float = 1.0
    reward_per_kill: float = 1.0

    # Resource-limited sustained output.  The baseline is deliberately sustainable:
    # 10 resource/attack, 100 capacity, 10 resource/second regeneration.
    resource_cost: float = 10.0
    max_resource: float = 100.0
    resource_regen: float = 10.0
    resource_regen_delay: float = 1.0


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def effective_targets(model: Model) -> float:
    """Reference-only 2D side-view target coverage.

    Only the upper semicircle is considered; underground targets are excluded. Ground
    and air densities are independent because they are not interchangeable in a
    platforming room. This is an approximation for comparison, not a level simulator.
    """
    semicircle_area = 0.5 * math.pi * model.aoe_range**2
    # Equal area weighting is intentionally explicit and can be changed when room
    # sampling data exists. Underground density is not included.
    area_targets = semicircle_area * (max(0.0, model.ground_density) + max(0.0, model.air_density))
    return min(float(max(1, model.target_count)), max(1.0, area_targets))


def mitigation_factor(model: Model) -> float:
    # Flat penetration subtracts defence first; percentage penetration then scales
    # the remaining defence. They are separate design variables.
    defense = max(0.0, model.target_defense - model.flat_penetration)
    penetration = clamp(model.percent_penetration, 0.0, 1.0)
    return 1.0 - (defense / (defense + 100.0)) * (1.0 - penetration)


def shield_split_hit(post_mitigation: float, shield_remaining: float, coefficient: float) -> tuple[float, float]:
    """Return (HP damage, shield damage) for one hit.

    The piecewise rule is the design model used for planning: P < 1 leaks 30% of
    the non-shield portion while the shield holds; P >= 1 fully absorbs until break.
    """
    shield = max(0.0, shield_remaining)
    if shield <= 0.0:
        return post_mitigation, 0.0

    coefficient = max(0.0, coefficient)
    shield_damage = post_mitigation * coefficient
    if coefficient < 1.0:
        if shield_damage <= shield:
            return post_mitigation * (1.0 - coefficient) * 0.3, shield_damage
        return post_mitigation * (1.0 - coefficient) + (shield_damage - shield), shield

    if shield_damage <= shield:
        return 0.0, shield_damage
    return (shield_damage - shield) / coefficient, shield


def attacks_to_defeat(post_mitigation: float, model: Model) -> tuple[int, float, float]:
    """Simulate discrete hits until one target's HP reaches zero.

    Shields are consumed and persist between hits. This keeps shield sensitivity
    piecewise without pretending that a shield regenerates after every attack.
    """
    hp_remaining = max(0.0, model.target_max_hp)
    shield_remaining = max(0.0, model.shield_amount)
    attacks = 0
    hp_dealt = 0.0
    max_attacks = 10_000_000
    while hp_remaining > 1e-9 and attacks < max_attacks:
        hp_damage, shield_damage = shield_split_hit(
            post_mitigation, shield_remaining, model.shield_coeff
        )
        hp_remaining -= hp_damage
        shield_remaining = max(0.0, shield_remaining - shield_damage)
        hp_dealt += hp_damage
        attacks += 1
        if hp_damage <= 1e-12 and shield_damage <= 1e-12:
            return max_attacks, hp_dealt, shield_remaining
    return attacks, hp_dealt, shield_remaining


def sustained_attack_rate(model: Model) -> float:
    """Return the steady-state attack rate under the resource recovery rule.

    Every spend resets the regeneration timer. Once the resource pool is depleted,
    each attack needs one recovery delay plus enough regeneration to replace its
    cost, so the resource-limited rate is ``1 / (delay + cost / regen)``. The
    initial full-resource burst is intentionally excluded from this metric; it is
    reported separately by ``burst_ttk`` and ``resource_burst_seconds``.
    """
    desired_rate = max(0.0, model.attacks_per_second)
    cost = max(0.0, model.resource_cost)
    if desired_rate <= 0.0:
        return 0.0
    if cost <= 0.0:
        return desired_rate
    if max(0.0, model.max_resource) + 1e-9 < cost:
        return 0.0

    regen = max(0.0, model.resource_regen)
    if regen <= 0.0:
        return 0.0

    delay = max(0.0, model.resource_regen_delay)
    resource_limited_rate = 1.0 / (delay + cost / regen)
    return min(desired_rate, resource_limited_rate)


def burst_ttk(post_mitigation: float, model: Model) -> float:
    """Time to defeat one target from a full resource pool.

    The first attack occurs after one attack interval. Subsequent attacks follow the
    requested cadence until resource is insufficient; then the one-second recovery
    delay and regeneration are simulated before the next attack.
    """
    hp_remaining = max(0.0, model.target_max_hp)
    shield_remaining = max(0.0, model.shield_amount)
    cost = max(0.0, model.resource_cost)
    resource = max(0.0, model.max_resource)
    rate = max(0.0, model.attacks_per_second)
    if rate <= 0.0 or post_mitigation <= 0.0:
        return math.inf

    interval = 1.0 / rate
    if cost > 0.0 and max(0.0, model.max_resource) + 1e-9 < cost:
        return math.inf
    time = interval
    last_spend = -math.inf
    attacks = 0
    while attacks < 10_000_000:
        if last_spend > -math.inf:
            regen_start = last_spend + max(0.0, model.resource_regen_delay)
            if time > regen_start:
                resource = min(
                    max(0.0, model.max_resource),
                    resource + (time - regen_start) * max(0.0, model.resource_regen),
                )

        if cost > 0.0 and resource + 1e-9 < cost:
            if model.resource_regen <= 0.0:
                return math.inf
            regen_start = time if last_spend == -math.inf else last_spend + max(0.0, model.resource_regen_delay)
            time = max(time, regen_start)
            time += (cost - resource) / model.resource_regen
            continue

        hp_damage, shield_damage = shield_split_hit(
            post_mitigation, shield_remaining, model.shield_coeff
        )
        if hp_damage <= 1e-12 and shield_damage <= 1e-12:
            return math.inf
        hp_remaining -= hp_damage
        shield_remaining = max(0.0, shield_remaining - shield_damage)
        if cost > 0.0:
            resource -= cost
            last_spend = time
        attacks += 1
        if hp_remaining <= 1e-9:
            return time
        time += interval
    return math.inf


def evaluate(model: Model) -> dict[str, float]:
    raw = (
        model.flat_damage + max(0.0, model.target_max_hp) * max(0.0, model.percent_damage)
    ) * max(0.0, model.skill_multiplier) * max(0.0, model.attack_multiplier)

    crit_factor = 1.0 + clamp(model.crit_chance, 0.0, 1.0) * (
        max(0.0, model.crit_multiplier) - 1.0
    )
    backstab_factor = 1.0 + clamp(model.backstab_uptime, 0.0, 1.0) * (
        max(0.0, model.backstab_multiplier) - 1.0
    )
    category_factor = (
        (1.0 + model.category_bonus)
        * (1.0 + model.element_bonus)
        * (1.0 + model.rarity_bonus)
    )
    post_mitigation = raw * crit_factor * backstab_factor * category_factor
    post_mitigation *= mitigation_factor(model)
    hp_per_hit, _ = shield_split_hit(
        post_mitigation, model.shield_amount, model.shield_coeff
    )
    hp_per_hit = max(0.0, hp_per_hit)

    raw_dps = hp_per_hit * max(0.0, model.attacks_per_second) * clamp(model.attack_uptime, 0.0, 1.0)
    targets = effective_targets(model)

    sustainable_rate = sustained_attack_rate(model)
    sustainable_dps = hp_per_hit * sustainable_rate * clamp(model.attack_uptime, 0.0, 1.0)
    attacks_needed, total_hp_dealt, _ = attacks_to_defeat(post_mitigation, model)
    sustainable_target_ttk = attacks_needed / max(sustainable_rate, 1e-9)
    burst_target_ttk = burst_ttk(post_mitigation, model)
    # AOE hits progress several targets in parallel.  The per-target TTK is
    # unchanged; target coverage increases the number of kills per minute.
    effective_dps = model.target_max_hp / max(sustainable_target_ttk, 1e-9) * targets
    kills_per_minute = 60.0 * targets / max(sustainable_target_ttk, 1e-9)
    earnings_per_minute = kills_per_minute * max(0.0, model.reward_per_kill)

    burst_seconds = (
        model.max_resource / (model.resource_cost * max(model.attacks_per_second, 1e-9))
        if model.resource_cost > 0.0
        else math.inf
    )
    return {
        "raw_damage": raw,
        "hp_damage_per_hit": hp_per_hit,
        "raw_dps_single_target": raw_dps,
        "sustainable_dps": effective_dps,
        "sustainable_attack_rate": sustainable_rate,
        "effective_targets": targets,
        "ttk_seconds": burst_target_ttk,
        "sustainable_target_ttk_seconds": sustainable_target_ttk,
        "kills_per_minute": kills_per_minute,
        "earnings_per_minute": earnings_per_minute,
        "resource_burst_seconds": burst_seconds,
    }


BASELINE = evaluate(Model())


@dataclass(frozen=True)
class Sweep:
    key: str
    label: str
    values: tuple[float, ...]
    unit: str = ""


def frange(start: float, stop: float, step: float) -> tuple[float, ...]:
    values: list[float] = []
    value = start
    while value <= stop + step * 0.001:
        values.append(round(value, 8))
        value += step
    return tuple(values)


SWEEPS: tuple[Sweep, ...] = (
    Sweep("flat_damage", "Flat damage", frange(0, 200, 10)),
    Sweep("percent_damage", "Max-HP damage", frange(0, 0.10, 0.005), "% max HP"),
    Sweep("skill_multiplier", "Skill multiplier", frange(0.5, 3, 0.1)),
    Sweep("attacks_per_second", "Attack frequency", frange(0.25, 3, 0.125), "hits/s"),
    Sweep("crit_chance", "Crit chance", frange(0, 1, 0.05), "%"),
    Sweep("crit_multiplier", "Crit multiplier", frange(1, 4, 0.15)),
    Sweep("backstab_uptime", "Backstab uptime", frange(0, 1, 0.05), "%"),
    Sweep("backstab_multiplier", "Backstab multiplier", frange(1, 3, 0.1)),
    Sweep("target_defense", "Target defense", frange(0, 200, 10)),
    Sweep("flat_penetration", "Flat penetration", frange(0, 100, 5)),
    Sweep("percent_penetration", "Percent penetration", frange(0, 1, 0.05), "%"),
    Sweep("shield_amount", "Shield amount", frange(0, 1500, 50)),
    Sweep("shield_coeff", "Shield coefficient", frange(0, 2, 0.1)),
    Sweep("resource_cost", "Resource cost", frange(0, 40, 2)),
    Sweep("target_max_hp", "Target max HP", frange(250, 3000, 50)),
    Sweep("ground_density", "Ground density (reference)", frange(0.25, 4, 0.25)),
    Sweep("air_density", "Air density (reference)", frange(0.0, 2, 0.1)),
    Sweep("aoe_range", "AOE range", frange(0.5, 3, 0.1)),
    Sweep("reward_per_kill", "Reward per kill", frange(0, 10, 0.5)),
)


def sweep_rows(sweep: Sweep) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for value in sweep.values:
        result = evaluate(replace(Model(), **{sweep.key: value}))
        rows.append(
            {
                "variable": sweep.key,
                "label": sweep.label,
                "value": value,
                "baseline_value": getattr(Model(), sweep.key),
                "raw_damage": result["raw_damage"],
                "hp_damage_per_hit": result["hp_damage_per_hit"],
                "sustainable_dps": result["sustainable_dps"],
                "burst_ttk_seconds": result["ttk_seconds"],
                "sustainable_target_ttk_seconds": result["sustainable_target_ttk_seconds"],
                "sustainable_attack_rate": result["sustainable_attack_rate"],
                "kills_per_minute": result["kills_per_minute"],
                "earnings_per_minute": result["earnings_per_minute"],
                "earnings_delta_percent": (result["earnings_per_minute"] / BASELINE["earnings_per_minute"] - 1.0) * 100.0,
            }
        )
    return rows


def write_csv(rows: Iterable[dict[str, object]]) -> None:
    path = OUTPUT / "sensitivity_data.csv"
    rows = list(rows)
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)


def svg_escape(value: object) -> str:
    return html.escape(str(value))


def line_path(points: list[tuple[float, float]], x0: float, y0: float, width: float, height: float,
              xmin: float, xmax: float, ymin: float, ymax: float) -> str:
    def px(x: float) -> float:
        return x0 + (x - xmin) / max(xmax - xmin, 1e-9) * width

    def py(y: float) -> float:
        return y0 + height - (y - ymin) / max(ymax - ymin, 1e-9) * height

    return " ".join(
        ("M" if index == 0 else "L") + f" {px(x):.2f} {py(y):.2f}"
        for index, (x, y) in enumerate(points)
    )


def chart_panel(x: float, y: float, width: float, height: float, title: str,
               rows: list[dict[str, object]]) -> str:
    pad_left, pad_right, pad_top, pad_bottom = 46, 12, 32, 28
    plot_x, plot_y = x + pad_left, y + pad_top
    plot_w, plot_h = width - pad_left - pad_right, height - pad_top - pad_bottom
    xs = [float(row["value"]) for row in rows]
    ys = [float(row["earnings_delta_percent"]) for row in rows]
    xmin, xmax = min(xs), max(xs)
    ymin, ymax = min(-100.0, min(ys)), max(100.0, max(ys))
    if ymax - ymin < 20:
        mid = (ymax + ymin) / 2
        ymin, ymax = mid - 10, mid + 10

    path = line_path(list(zip(xs, ys)), plot_x, plot_y, plot_w, plot_h, xmin, xmax, ymin, ymax)
    zero_y = plot_y + plot_h - (0 - ymin) / (ymax - ymin) * plot_h
    baseline_x = plot_x + (float(getattr(Model(), rows[0]["variable"])) - xmin) / max(xmax - xmin, 1e-9) * plot_w
    return f'''<g>
  <rect x="{x:.1f}" y="{y:.1f}" width="{width:.1f}" height="{height:.1f}" fill="#f8fafc" stroke="#cbd5e1"/>
  <text x="{x + 8:.1f}" y="{y + 20:.1f}" font-size="13" font-weight="600" fill="#0f172a">{svg_escape(title)}</text>
  <line x1="{plot_x:.1f}" y1="{zero_y:.1f}" x2="{plot_x + plot_w:.1f}" y2="{zero_y:.1f}" stroke="#94a3b8" stroke-dasharray="3 3"/>
  <line x1="{plot_x:.1f}" y1="{plot_y:.1f}" x2="{plot_x:.1f}" y2="{plot_y + plot_h:.1f}" stroke="#334155"/>
  <line x1="{plot_x:.1f}" y1="{plot_y + plot_h:.1f}" x2="{plot_x + plot_w:.1f}" y2="{plot_y + plot_h:.1f}" stroke="#334155"/>
  <line x1="{baseline_x:.1f}" y1="{plot_y:.1f}" x2="{baseline_x:.1f}" y2="{plot_y + plot_h:.1f}" stroke="#f59e0b" stroke-dasharray="4 3"/>
  <path d="{path}" fill="none" stroke="#2563eb" stroke-width="2"/>
  <text x="{plot_x - 6:.1f}" y="{plot_y + 4:.1f}" text-anchor="end" font-size="10" fill="#475569">+{ymax:.0f}%</text>
  <text x="{plot_x - 6:.1f}" y="{zero_y + 4:.1f}" text-anchor="end" font-size="10" fill="#475569">0%</text>
  <text x="{plot_x - 6:.1f}" y="{plot_y + plot_h + 4:.1f}" text-anchor="end" font-size="10" fill="#475569">{ymin:.0f}%</text>
  <text x="{plot_x + plot_w / 2:.1f}" y="{y + height - 7:.1f}" text-anchor="middle" font-size="10" fill="#475569">variable value</text>
  <text x="{x + 12:.1f}" y="{plot_y + plot_h / 2:.1f}" text-anchor="middle" font-size="10" fill="#475569" transform="rotate(-90 {x + 12:.1f} {plot_y + plot_h / 2:.1f})">earnings delta</text>
</g>'''


def write_curve_svg(all_rows: dict[str, list[dict[str, object]]]) -> None:
    width, panel_w, panel_h, columns = 1120, 270, 205, 4
    panels = []
    for index, sweep in enumerate(SWEEPS):
        x = (index % columns) * panel_w
        y = (index // columns) * panel_h
        panels.append(chart_panel(x, y, panel_w - 10, panel_h - 10, sweep.label, all_rows[sweep.key]))
    height = math.ceil(len(SWEEPS) / columns) * panel_h
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
<title>Theoretical one-variable sensitivity curves</title>
<rect width="100%" height="100%" fill="#ffffff"/>
{''.join(panels)}
</svg>'''
    (OUTPUT / "sensitivity_curves.svg").write_text(svg, encoding="utf-8")


CONTEXT_COLORS = ("#2563eb", "#dc2626", "#16a34a", "#9333ea")
REFERENCE_AOE_TARGET_COUNT = 20


@dataclass(frozen=True)
class ContextPlot:
    sweep_key: str
    title: str
    context_key: str
    contexts: tuple[float, ...]
    fixed: tuple[tuple[str, float], ...] = ()


CONTEXT_PLOTS: tuple[ContextPlot, ...] = (
    ContextPlot("crit_multiplier", "Crit multiplier by crit chance", "crit_chance", (0.0, 0.25, 0.50, 1.0)),
    ContextPlot("crit_chance", "Crit chance by crit multiplier", "crit_multiplier", (1.5, 2.0, 3.0, 4.0)),
    ContextPlot("percent_penetration", "Percent penetration by target defense", "target_defense", (0.0, 50.0, 100.0, 200.0)),
    ContextPlot("flat_penetration", "Flat penetration by target defense", "target_defense", (0.0, 50.0, 100.0, 200.0)),
    ContextPlot("target_defense", "Defense by percent penetration", "percent_penetration", (0.0, 0.25, 0.50, 1.0)),
    ContextPlot("backstab_multiplier", "Backstab multiplier by uptime", "backstab_uptime", (0.0, 0.25, 0.50, 1.0)),
    ContextPlot("attacks_per_second", "Attack frequency by resource cost", "resource_cost", (0.0, 5.0, 10.0, 20.0)),
    ContextPlot("shield_coeff", "Shield coefficient by shield amount", "shield_amount", (0.0, 250.0, 500.0, 1000.0)),
    ContextPlot("flat_damage", "Flat damage by target max HP", "target_max_hp", (250.0, 1000.0, 2000.0, 3000.0)),
    ContextPlot("percent_damage", "Max-HP damage by target max HP", "target_max_hp", (250.0, 1000.0, 2000.0, 3000.0)),
    ContextPlot("target_max_hp", "Target max HP by attack frequency", "attacks_per_second", (0.5, 1.0, 2.0, 3.0)),
    ContextPlot("aoe_range", "AOE range by ground density (reference)", "ground_density", (0.25, 1.0, 2.0, 4.0), (("target_count", 20.0),)),
    ContextPlot("aoe_range", "AOE range by air density (reference)", "air_density", (0.0, 0.35, 0.75, 1.5), (("target_count", 20.0),)),
)


def contextual_panel(x: float, y: float, width: float, height: float, plot: ContextPlot,
                     sweep: Sweep) -> str:
    pad_left, pad_right, pad_top, pad_bottom = 48, 12, 48, 28
    plot_x, plot_y = x + pad_left, y + pad_top
    plot_w, plot_h = width - pad_left - pad_right, height - pad_top - pad_bottom
    series: list[tuple[str, list[tuple[float, float]]]] = []
    all_y: list[float] = []
    baseline_earnings = BASELINE["earnings_per_minute"]
    fixed = dict(plot.fixed)
    for context_value in plot.contexts:
        points: list[tuple[float, float]] = []
        for value in sweep.values:
            changes = {sweep.key: value, plot.context_key: context_value, **fixed}
            result = evaluate(replace(Model(), **changes))
            normalized = result["earnings_per_minute"] / baseline_earnings * 100.0
            points.append((float(value), normalized))
            all_y.append(normalized)
        series.append((f"{plot.context_key}={context_value:g}", points))

    xmin, xmax = min(sweep.values), max(sweep.values)
    ymin, ymax = min(0.0, min(all_y)), max(100.0, max(all_y))
    if ymax - ymin < 20:
        ymax = ymin + 20
    parts = [
        f'<g><rect x="{x:.1f}" y="{y:.1f}" width="{width:.1f}" height="{height:.1f}" fill="#f8fafc" stroke="#cbd5e1"/>',
        f'<text x="{x + 8:.1f}" y="{y + 18:.1f}" font-size="13" font-weight="600" fill="#0f172a">{svg_escape(plot.title)}</text>',
        f'<line x1="{plot_x:.1f}" y1="{plot_y:.1f}" x2="{plot_x:.1f}" y2="{plot_y + plot_h:.1f}" stroke="#334155"/>',
        f'<line x1="{plot_x:.1f}" y1="{plot_y + plot_h:.1f}" x2="{plot_x + plot_w:.1f}" y2="{plot_y + plot_h:.1f}" stroke="#334155"/>',
    ]
    for index, (label, points) in enumerate(series):
        color = CONTEXT_COLORS[index % len(CONTEXT_COLORS)]
        path = line_path(points, plot_x, plot_y, plot_w, plot_h, xmin, xmax, ymin, ymax)
        legend_x = x + 8 + (index % 2) * (width / 2 - 6)
        legend_y = y + 31 + (index // 2) * 12
        parts.extend([
            f'<path d="{path}" fill="none" stroke="{color}" stroke-width="2"/>',
            f'<line x1="{legend_x:.1f}" y1="{legend_y - 3:.1f}" x2="{legend_x + 12:.1f}" y2="{legend_y - 3:.1f}" stroke="{color}" stroke-width="2"/>',
            f'<text x="{legend_x + 16:.1f}" y="{legend_y:.1f}" font-size="9" fill="#475569">{svg_escape(label)}</text>',
        ])
    parts.extend([
        f'<text x="{plot_x - 6:.1f}" y="{plot_y + 4:.1f}" text-anchor="end" font-size="9" fill="#475569">{ymax:.0f}%</text>',
        f'<text x="{plot_x - 6:.1f}" y="{plot_y + plot_h + 4:.1f}" text-anchor="end" font-size="9" fill="#475569">{ymin:.0f}%</text>',
        f'<text x="{plot_x + plot_w / 2:.1f}" y="{y + height - 7:.1f}" text-anchor="middle" font-size="10" fill="#475569">{svg_escape(sweep.key)}</text>',
        f'<text x="{x + 12:.1f}" y="{plot_y + plot_h / 2:.1f}" text-anchor="middle" font-size="10" fill="#475569" transform="rotate(-90 {x + 12:.1f} {plot_y + plot_h / 2:.1f})">baseline earnings %</text>',
        '</g>',
    ])
    return ''.join(parts)


def write_context_svg() -> None:
    sweep_map = {sweep.key: sweep for sweep in SWEEPS}
    width, panel_w, panel_h, columns = 1120, 550, 250, 2
    panels = []
    for index, plot in enumerate(CONTEXT_PLOTS):
        x = (index % columns) * panel_w
        y = (index // columns) * panel_h
        panels.append(contextual_panel(x, y, panel_w - 10, panel_h - 10, plot, sweep_map[plot.sweep_key]))
    height = math.ceil(len(CONTEXT_PLOTS) / columns) * panel_h
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
<title>Contextual one-variable sensitivity slices</title>
<rect width="100%" height="100%" fill="#ffffff"/>
{''.join(panels)}
</svg>'''
    (OUTPUT / "context_slices.svg").write_text(svg, encoding="utf-8")


def heat_color(value: float, low: float, high: float) -> str:
    normalized = clamp((value - low) / max(high - low, 1e-9), 0.0, 1.0)
    if normalized < 0.5:
        ratio = normalized * 2
        r, g, b = int(239 * ratio), int(68 + 130 * ratio), int(68 + 160 * ratio)
    else:
        ratio = (normalized - 0.5) * 2
        r, g, b = int(239 - 203 * ratio), int(198 - 100 * ratio), int(68 - 20 * ratio)
    return f"#{r:02x}{g:02x}{b:02x}"


def write_heatmaps() -> None:
    pairs = (
        ("flat_damage", "percent_damage", "Flat damage × max-HP damage"),
        ("crit_chance", "crit_multiplier", "Crit chance × crit multiplier"),
        ("target_defense", "flat_penetration", "Defense × flat penetration"),
        ("target_defense", "percent_penetration", "Defense × percent penetration"),
        ("attacks_per_second", "resource_cost", "Attack frequency × resource cost"),
        ("aoe_range", "ground_density", "AOE range × ground density (reference)"),
        ("aoe_range", "air_density", "AOE range × air density (reference)"),
        ("shield_amount", "shield_coeff", "Shield amount × shield coefficient"),
        ("backstab_uptime", "backstab_multiplier", "Backstab uptime × multiplier"),
        ("target_max_hp", "flat_damage", "Target max HP × flat damage"),
    )
    sweep_map = {sweep.key: sweep for sweep in SWEEPS}
    width, cell, title_h, panel_h, columns = 1120, 18, 30, 235, 2
    panels = []
    for index, (x_key, y_key, title) in enumerate(pairs):
        x0 = (index % columns) * (width // columns)
        y0 = (index // columns) * panel_h
        xs = sweep_map[x_key].values[::max(1, len(sweep_map[x_key].values) // 16)]
        ys = sweep_map[y_key].values[::max(1, len(sweep_map[y_key].values) // 12)]
        values: list[float] = []
        grid: list[list[float]] = []
        # A single-target baseline intentionally makes AOE irrelevant. For the
        # AOE heatmaps use a fixed multi-target encounter so the plot measures
        # coverage sensitivity; this remains reference-only because density is
        # not controlled by the abstract model.
        fixed = {"target_count": REFERENCE_AOE_TARGET_COUNT} if "aoe_range" in (x_key, y_key) else {}
        for y_value in ys:
            row: list[float] = []
            for x_value in xs:
                model = replace(Model(), **{x_key: x_value, y_key: y_value, **fixed})
                result = evaluate(model)["earnings_per_minute"] / BASELINE["earnings_per_minute"] * 100.0
                row.append(result)
                values.append(result)
            grid.append(row)
        low, high = min(values), max(values)
        grid_x = x0 + 72
        grid_y = y0 + title_h + 20
        grid_w = max(120, min(360, len(xs) * cell))
        grid_h = max(120, min(216, len(ys) * cell))
        cell_w, cell_h = grid_w / len(xs), grid_h / len(ys)
        parts = [f'<g><text x="{x0 + 10}" y="{y0 + 20}" font-size="13" font-weight="600" fill="#0f172a">{svg_escape(title)}</text>']
        for row_index, row in enumerate(grid):
            for col_index, value in enumerate(row):
                rect_x = grid_x + col_index * cell_w
                rect_y = grid_y + (len(grid) - 1 - row_index) * cell_h
                parts.append(f'<rect x="{rect_x:.1f}" y="{rect_y:.1f}" width="{cell_w + .4:.1f}" height="{cell_h + .4:.1f}" fill="{heat_color(value, low, high)}" stroke="#ffffff" stroke-width=".4"><title>{x_key}={xs[col_index]}, {y_key}={ys[row_index]}, earnings={value:.1f}%</title></rect>')
        parts.extend([
            f'<text x="{grid_x + grid_w / 2:.1f}" y="{grid_y + grid_h + 18:.1f}" text-anchor="middle" font-size="10" fill="#475569">{svg_escape(x_key)}</text>',
            f'<text x="{grid_x - 38:.1f}" y="{grid_y + grid_h / 2:.1f}" text-anchor="middle" font-size="10" fill="#475569" transform="rotate(-90 {grid_x - 38:.1f} {grid_y + grid_h / 2:.1f})">{svg_escape(y_key)}</text>',
            f'<text x="{grid_x + grid_w + 12:.1f}" y="{grid_y + 10:.1f}" font-size="9" fill="#475569">max {high:.0f}%</text>',
            f'<text x="{grid_x + grid_w + 12:.1f}" y="{grid_y + grid_h:.1f}" font-size="9" fill="#475569">min {low:.0f}%</text>',
            '</g>',
        ])
        panels.append(''.join(parts))
    height = math.ceil(len(pairs) / columns) * panel_h
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">
<title>Theoretical interaction heatmaps</title>
<rect width="100%" height="100%" fill="#ffffff"/>
{''.join(panels)}
</svg>'''
    (OUTPUT / "interaction_heatmaps.svg").write_text(svg, encoding="utf-8")


def percentile_sensitivity(rows: list[dict[str, object]]) -> tuple[float, float]:
    values = [abs(float(row["earnings_delta_percent"])) for row in rows]
    values.sort()
    return values[len(values) // 2], values[-1]


def write_summary(all_rows: dict[str, list[dict[str, object]]]) -> None:
    lines = [
        "# Theoretical sensitivity analysis",
        "",
        "This report uses only abstract parameters. No game asset, weapon, enemy or scene is referenced.",
        "",
        "## Baseline assumptions",
        "",
        "- Raw damage: 100 flat damage + 0% target-max-HP damage.",
        "- Target: 1,000 maximum HP, evaluated from full health, zero defense, zero shield.",
        "- Attack rate: 1 hit/s, 100% uptime, one target.",
        "- Crit: 0% chance, 150% multiplier; backstab uptime 0%, 150% multiplier.",
        "- Resource: 10 cost, 100 capacity, 10 regeneration/s, 1 s delay after the last spend.",
        "- Reward: one normalized unit per kill.",
        "",
        f"Baseline raw damage: **{BASELINE['raw_damage']:.2f}**",
        f"Baseline sustainable DPS: **{BASELINE['sustainable_dps']:.2f}**",
        f"Baseline burst TTK: **{BASELINE['ttk_seconds']:.2f}s**",
        f"Baseline sustainable target TTK: **{BASELINE['sustainable_target_ttk_seconds']:.2f}s**",
        f"Baseline earnings/min: **{BASELINE['earnings_per_minute']:.2f} normalized units**",
        "",
        "## Interpretation",
        "",
        "- Curves report earnings/minute relative to the baseline. This is a normalized kill-efficiency metric, not a game economy value.",
        "- Each curve changes exactly one variable. The heatmaps are limited to selected within-group interactions.",
        "- Critical and backstab results use expected multipliers; randomness and variance are intentionally excluded from this first deterministic pass.",
        "- A zero baseline sensitivity means that the variable's interaction partner is inactive (for example, crit multiplier at 0% crit chance). Use context_slices.svg and interaction_heatmaps.svg for those variables.",
        "- AOE curves are flat in the one-target baseline by design. AOE context slices and heatmaps use a fixed 20-target reference encounter and must not be read as room-accurate predictions.",
        "- Sustainable attack rate uses the steady-state cycle: min(input rate, 1 / (recovery delay + resource cost / regeneration)).",
        "",
        "## Largest median and maximum sensitivities",
        "",
        "| Variable | Median absolute earnings delta | Maximum absolute earnings delta |",
        "| --- | ---: | ---: |",
    ]
    for sweep in SWEEPS:
        median, maximum = percentile_sensitivity(all_rows[sweep.key])
        lines.append(f"| {sweep.label} | {median:.1f}% | {maximum:.1f}% |")
    lines.extend([
        "",
        "## Next analysis pass",
        "",
        "1. Add stochastic Monte Carlo runs for crit and damage variance.",
        "2. Add resource burst versus sustained-output plots.",
        "3. Add a separate encounter model for spawn rate, density and reward-per-kill.",
    ])
    (OUTPUT / "sensitivity_summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def ttk_benefit(before: float, after: float) -> float:
    if not math.isfinite(before) or not math.isfinite(after) or after <= 0.0:
        return -math.inf
    return before / after - 1.0


def closest_ttk_match(sweep: Sweep, metric: str, target_benefit: float = 0.10,
                      samples: int = 20_000) -> dict[str, object]:
    """Find the value in a dense one-variable scan nearest the requested TTK gain."""
    baseline_model = Model()
    baseline_value = float(getattr(baseline_model, sweep.key))
    baseline_ttk = float(BASELINE[metric])
    low, high = float(min(sweep.values)), float(max(sweep.values))
    best: tuple[float, float, float, float] | None = None

    for index in range(samples + 1):
        value = low + (high - low) * index / samples
        if abs(value - baseline_value) <= 1e-12:
            continue
        result = evaluate(replace(baseline_model, **{sweep.key: value}))
        after = float(result[metric])
        benefit = ttk_benefit(baseline_ttk, after)
        if benefit <= 0.0:
            continue
        score = abs(benefit - target_benefit)
        candidate = (score, abs(value - baseline_value), value, after)
        if best is None or candidate < best:
            best = candidate

    metric_label = "burst" if metric == "ttk_seconds" else "sustainable"
    if best is None:
        return {
            "metric": metric_label,
            "variable": sweep.key,
            "label": sweep.label,
            "status": "no positive TTK gain in scan range",
            "baseline_value": baseline_value,
            "test_value": "",
            "delta": "",
            "delta_percent": "",
            "ttk_before": baseline_ttk,
            "ttk_after": "",
            "ttk_benefit_percent": "",
        }

    _, _, value, after = best
    delta = value - baseline_value
    delta_percent = delta / baseline_value * 100.0 if abs(baseline_value) > 1e-12 else math.nan
    return {
        "metric": metric_label,
        "variable": sweep.key,
        "label": sweep.label,
        "status": "matched",
        "baseline_value": baseline_value,
        "test_value": value,
        "delta": delta,
        "delta_percent": delta_percent,
        "ttk_before": baseline_ttk,
        "ttk_after": after,
        "ttk_benefit_percent": ttk_benefit(baseline_ttk, after) * 100.0,
    }


def write_equal_ttk_analysis() -> None:
    rows = [
        closest_ttk_match(sweep, metric)
        for metric in ("ttk_seconds", "sustainable_target_ttk_seconds")
        for sweep in SWEEPS
    ]
    csv_path = OUTPUT / "equal_ttk_increments.csv"
    with csv_path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    lines = [
        "# Equal-TTK increment analysis",
        "",
        "Target benefit: `TTK_before / TTK_after - 1 = 10%`.",
        "",
        "- Burst TTK starts with a full resource pool and is the primary target-dummy comparison.",
        "- Sustainable TTK excludes the initial full-resource burst and uses the steady-state resource cycle.",
        "- Discrete hit counts mean many damage variables can only reach 11.11%, not exactly 10%.",
        "- A missing match means the variable cannot improve that TTK under the current baseline and scan range.",
        "",
    ]
    for metric, title in (("burst", "Burst TTK"), ("sustainable", "Sustainable TTK")):
        lines.extend([
            f"## {title}",
            "",
            "| Variable | Baseline | Test value | Delta | Delta % | TTK before | TTK after | Benefit | Status |",
            "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |",
        ])
        for row in rows:
            if row["metric"] != metric:
                continue
            if row["status"] == "matched":
                delta_percent = row["delta_percent"]
                delta_percent_text = "n/a" if not math.isfinite(float(delta_percent)) else f"{float(delta_percent):.3f}%"
                lines.append(
                    f"| {row['label']} | {float(row['baseline_value']):.6g} | "
                    f"{float(row['test_value']):.6g} | {float(row['delta']):+.6g} | "
                    f"{delta_percent_text} | {float(row['ttk_before']):.4g}s | "
                    f"{float(row['ttk_after']):.4g}s | {float(row['ttk_benefit_percent']):.3f}% | matched |"
                )
            else:
                lines.append(
                    f"| {row['label']} | {float(row['baseline_value']):.6g} | — | — | — | "
                    f"{float(row['ttk_before']):.4g}s | — | — | no positive gain |"
                )
        lines.append("")
    (OUTPUT / "equal_ttk_summary.md").write_text("\n".join(lines), encoding="utf-8")


SHIELD_CONTEXTS: tuple[tuple[str, float], ...] = (
    ("Low HP", 500.0),
    ("Medium HP", 1000.0),
    ("High HP", 2000.0),
)
SHIELD_LEVELS: tuple[tuple[str, float], ...] = (
    ("Low shield", 250.0),
    ("Medium shield", 500.0),
    ("High shield", 1000.0),
)
SHIELD_COEFF_VALUES: tuple[float, ...] = (0.0, 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0)


def write_shield_context_analysis() -> None:
    rows: list[dict[str, object]] = []
    for hp_label, hp in SHIELD_CONTEXTS:
        for shield_label, shield in SHIELD_LEVELS:
            for coefficient in SHIELD_COEFF_VALUES:
                result = evaluate(replace(
                    Model(),
                    target_max_hp=hp,
                    shield_amount=shield,
                    shield_coeff=coefficient,
                ))
                rows.append({
                    "hp_band": hp_label,
                    "target_max_hp": hp,
                    "shield_band": shield_label,
                    "shield_amount": shield,
                    "shield_coeff": coefficient,
                    "burst_ttk_seconds": result["ttk_seconds"],
                    "sustainable_ttk_seconds": result["sustainable_target_ttk_seconds"],
                })

    csv_path = OUTPUT / "shield_coeff_context.csv"
    with csv_path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    lines = [
        "# Shield coefficient context analysis",
        "",
        "This scan uses the theory-only combat model with the current resource rules.",
        "Low/medium/high target maximum HP are 500/1000/2000; low/medium/high shield are 250/500/1000.",
        "Shield coefficient values are 0 to 2 in steps of 0.25.",
        "Burst TTK starts from full resources; sustainable TTK uses the steady-state resource cycle.",
        "",
    ]
    for hp_label, hp in SHIELD_CONTEXTS:
        for shield_label, shield in SHIELD_LEVELS:
            subset = [row for row in rows if row["target_max_hp"] == hp and row["shield_amount"] == shield]
            lines.extend([
                f"## {hp_label} ({hp:g} HP) × {shield_label} ({shield:g} shield)",
                "",
                "| ShieldCoeff | Burst TTK | Sustainable TTK | Burst vs coeff 0 | Sustainable vs coeff 0 |",
                "| ---: | ---: | ---: | ---: | ---: |",
            ])
            baseline_burst = float(subset[0]["burst_ttk_seconds"])
            baseline_sustainable = float(subset[0]["sustainable_ttk_seconds"])
            for row in subset:
                burst = float(row["burst_ttk_seconds"])
                sustainable = float(row["sustainable_ttk_seconds"])
                burst_gain = baseline_burst / burst - 1.0
                sustainable_gain = baseline_sustainable / sustainable - 1.0
                lines.append(
                    f"| {float(row['shield_coeff']):g} | {burst:.3g}s | {sustainable:.3g}s | "
                    f"{burst_gain * 100:+.2f}% | {sustainable_gain * 100:+.2f}% |"
                )
            lines.append("")

    lines.extend([
        "## Observations",
        "",
        "- With low shield, increasing ShieldCoeff beyond 1 often has little effect because the shield is broken quickly.",
        "- With high shield, higher ShieldCoeff produces a much larger TTK reduction, especially for high-HP targets.",
        "- A coefficient below 1 can be worse than coefficient 0: it reduces HP leakage while only slowly damaging the shield.",
        "- The result is not monotonic in every low-coefficient region, so ShieldAmount and ShieldCoeff must be tested together.",
    ])
    (OUTPUT / "shield_coeff_context.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_equal_survival_analysis() -> None:
    """Compare defensive increments that provide 10% continuous effective survival."""
    baseline = Model()
    baseline_result = evaluate(baseline)
    cases = (
        ("Target max HP", "target_max_hp", 1000.0, 1100.0, replace(baseline, target_max_hp=1100.0)),
        ("Relevant defense (armor or MR)", "target_defense", 0.0, 10.0, replace(baseline, target_defense=10.0)),
        ("Shield amount at ShieldCoeff=1", "shield_amount", 0.0, 100.0, replace(baseline, shield_amount=100.0, shield_coeff=1.0)),
    )
    rows: list[dict[str, object]] = []
    for label, key, before_value, after_value, model in cases:
        result = evaluate(model)
        rows.append({
            "variable": key,
            "label": label,
            "baseline_value": before_value,
            "test_value": after_value,
            "delta": after_value - before_value,
            "continuous_survival_before": 10.0,
            "continuous_survival_after": 11.0,
            "continuous_survival_gain_percent": 10.0,
            "burst_ttk_before": baseline_result["ttk_seconds"],
            "burst_ttk_after": result["ttk_seconds"],
            "burst_survival_gain_percent": (result["ttk_seconds"] / baseline_result["ttk_seconds"] - 1.0) * 100.0,
            "sustainable_ttk_before": baseline_result["sustainable_target_ttk_seconds"],
            "sustainable_ttk_after": result["sustainable_target_ttk_seconds"],
            "sustainable_survival_gain_percent": (
                result["sustainable_target_ttk_seconds"] / baseline_result["sustainable_target_ttk_seconds"] - 1.0
            ) * 100.0,
        })

    with (OUTPUT / "equal_survival_increments.csv").open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    lines = [
        "# Equal-survival defensive increments",
        "",
        "Target gain: `survival_after / survival_before - 1 = 10%`.",
        "The fixed attacker deals 100 damage before target defense once per second; baseline defense is 0, so baseline post-mitigation damage is also 100. Percentage damage, crit and backstab are disabled.",
        "ShieldCoeff is not valued as an attribute; ShieldCoeff=1 is used only for the shield-amount comparison.",
        "The generic defense row represents armor against a physical attacker or magic resistance against a magical attacker.",
        "",
        "| Defensive attribute | Baseline | Test value | Increment | Continuous gain | Burst TTK gain | Sustainable TTK gain |",
        "| --- | ---: | ---: | ---: | ---: | ---: | ---: |",
    ]
    for row in rows:
        lines.append(
            f"| {row['label']} | {float(row['baseline_value']):g} | {float(row['test_value']):g} | "
            f"+{float(row['delta']):g} | {float(row['continuous_survival_gain_percent']):.2f}% | "
            f"{float(row['burst_ttk_before']):.3g}s → {float(row['burst_ttk_after']):.3g}s "
            f"({float(row['burst_survival_gain_percent']):+.2f}%) | "
            f"{float(row['sustainable_ttk_before']):.3g}s → {float(row['sustainable_ttk_after']):.3g}s "
            f"({float(row['sustainable_survival_gain_percent']):+.2f}%) |"
        )
    lines.extend([
        "",
        "## Interpretation",
        "",
        "- +100 maximum HP, +10 relevant defense, and +100 shield at coefficient 1 are equal at +10% continuous effective survival.",
        "- Sustainable TTK also rises by exactly 10% because it is based on the steady 0.5 attacks/s resource cycle.",
        "- Burst TTK rises from 10s to 12s (+20%), not 11s, because the eleventh attack occurs only after the resource recovery delay.",
    ])
    (OUTPUT / "equal_survival_summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    all_rows = {sweep.key: sweep_rows(sweep) for sweep in SWEEPS}
    write_csv(row for rows in all_rows.values() for row in rows)
    write_curve_svg(all_rows)
    write_context_svg()
    write_heatmaps()
    write_summary(all_rows)
    write_equal_ttk_analysis()
    write_shield_context_analysis()
    write_equal_survival_analysis()
    print(f"Wrote theoretical sensitivity outputs to {OUTPUT}")


if __name__ == "__main__":
    main()
