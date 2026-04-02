from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np


PLAYERS = 10_000
BASE_SPIN_COST = 100
COST_STEP = 50
JACKPOTS_TARGET = 3
SEED = 20260316


@dataclass(frozen=True)
class WheelConfig:
    name: str
    base_jackpot_chance: float
    pity_step: float
    color: str


CONFIGS = [
    WheelConfig("Current 1.0% + 0.5%", 0.01, 0.005, "#e45756"),
    WheelConfig("Soft nerf 1.0% + 0.3%", 0.01, 0.003, "#f58518"),
    WheelConfig("Safe 0.8% + 0.2%", 0.008, 0.002, "#4c78a8"),
    WheelConfig("Hard 0.5% + 0.1%", 0.005, 0.001, "#54a24b"),
]


def simulate_cycle_lengths(player_count: int, rng: np.random.Generator, config: WheelConfig) -> np.ndarray:
    spins = np.zeros(player_count, dtype=np.int32)
    misses = np.zeros(player_count, dtype=np.int32)
    finished = np.zeros(player_count, dtype=bool)

    while not finished.all():
        active_idx = np.flatnonzero(~finished)
        jackpot_chance = np.minimum(
            config.base_jackpot_chance + misses[active_idx] * config.pity_step,
            1.0,
        )
        rolls = rng.random(active_idx.size)
        jackpot_mask = rolls < jackpot_chance

        spins[active_idx] += 1
        finished[active_idx[jackpot_mask]] = True
        misses[active_idx[~jackpot_mask]] += 1

    return spins


def simulate_config(player_count: int, rng: np.random.Generator, config: WheelConfig) -> dict[str, object]:
    cycle_spins = [simulate_cycle_lengths(player_count, rng, config) for _ in range(JACKPOTS_TARGET)]

    spend_first = cycle_spins[0] * BASE_SPIN_COST
    spend_total = (
        cycle_spins[0] * BASE_SPIN_COST
        + cycle_spins[1] * (BASE_SPIN_COST + COST_STEP)
        + cycle_spins[2] * (BASE_SPIN_COST + 2 * COST_STEP)
    )

    return {
        "config": config,
        "spend_first": spend_first,
        "spend_total": spend_total,
        "summary": {
            "avg_spend_first_jackpot": float(spend_first.mean()),
            "avg_spend_three_jackpots": float(spend_total.mean()),
            "p50_spend_three_jackpots": float(np.percentile(spend_total, 50)),
            "p90_spend_three_jackpots": float(np.percentile(spend_total, 90)),
            "p95_spend_three_jackpots": float(np.percentile(spend_total, 95)),
            "max_spend_three_jackpots": int(spend_total.max()),
        },
    }


def plot_cdf(results: list[dict[str, object]], output_path: Path) -> None:
    plt.figure(figsize=(12, 7))

    for result in results:
        config = result["config"]
        spend_total = np.sort(result["spend_total"])
        cumulative = np.arange(1, spend_total.size + 1) / spend_total.size

        plt.plot(
            spend_total,
            cumulative,
            label=config.name,
            color=config.color,
            linewidth=2.2,
        )

    plt.title("Wheel of Fortune: cumulative spend to reach 3 jackpots")
    plt.xlabel("Coins spent")
    plt.ylabel("Share of players")
    plt.grid(True, alpha=0.25)
    plt.legend()
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Compare 4 Wheel of Fortune jackpot configurations on one graph.")
    parser.add_argument("--players", type=int, default=PLAYERS)
    parser.add_argument("--seed", type=int, default=SEED)
    parser.add_argument("--plot", type=Path, default=Path("wheel_config_comparison_cdf.png"))
    parser.add_argument("--summary", type=Path, default=Path("wheel_config_comparison_summary.json"))
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    rng = np.random.default_rng(args.seed)

    results = [simulate_config(args.players, rng, config) for config in CONFIGS]
    plot_cdf(results, args.plot)

    summary_payload = {
        "players": args.players,
        "seed": args.seed,
        "plot_path": str(args.plot),
        "configs": [
            {
                "name": result["config"].name,
                "base_jackpot_chance": result["config"].base_jackpot_chance,
                "pity_step": result["config"].pity_step,
                **result["summary"],
            }
            for result in results
        ],
    }

    args.summary.write_text(json.dumps(summary_payload, ensure_ascii=True, indent=2), encoding="utf-8")
    print(json.dumps(summary_payload, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
