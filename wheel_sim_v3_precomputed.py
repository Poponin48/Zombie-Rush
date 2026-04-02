from __future__ import annotations

import argparse
import bisect
import json
import random
from pathlib import Path

import matplotlib.pyplot as plt


PLAYERS = 10_000
BASE_SPIN_COST = 100
COST_STEP = 50
BASE_JACKPOT_CHANCE = 0.01
PITY_STEP = 0.005
SEED = 20260316


def build_cycle_cdf() -> list[float]:
    cdf: list[float] = []
    survival_probability = 1.0
    spin_index = 0

    while survival_probability > 0.0:
        current_chance = min(BASE_JACKPOT_CHANCE + spin_index * PITY_STEP, 1.0)
        probability_here = survival_probability * current_chance
        cumulative = (cdf[-1] if cdf else 0.0) + probability_here
        cdf.append(cumulative)
        survival_probability *= 1.0 - current_chance
        spin_index += 1

        if current_chance >= 1.0:
            break

    cdf[-1] = 1.0
    return cdf


def sample_cycle_length(rng: random.Random, cycle_cdf: list[float]) -> int:
    return bisect.bisect_left(cycle_cdf, rng.random()) + 1


def build_histogram(total_spend_values: list[int], output_path: Path) -> None:
    plt.figure(figsize=(11, 6))
    plt.hist(total_spend_values, bins=50, color="#54a24b", edgecolor="white")
    plt.title("Wheel of Fortune: cost distribution for 3 jackpots")
    plt.xlabel("Coins spent")
    plt.ylabel("Players")
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close()


def run_simulation(players: int, seed: int, histogram_path: Path) -> dict[str, float | int | str]:
    rng = random.Random(seed)
    cycle_cdf = build_cycle_cdf()

    first_jackpot_spend: list[int] = []
    three_jackpots_spend: list[int] = []

    for _ in range(players):
        cycle_1 = sample_cycle_length(rng, cycle_cdf)
        cycle_2 = sample_cycle_length(rng, cycle_cdf)
        cycle_3 = sample_cycle_length(rng, cycle_cdf)

        spend_first = cycle_1 * BASE_SPIN_COST
        spend_total = (
            cycle_1 * BASE_SPIN_COST
            + cycle_2 * (BASE_SPIN_COST + COST_STEP)
            + cycle_3 * (BASE_SPIN_COST + 2 * COST_STEP)
        )

        first_jackpot_spend.append(spend_first)
        three_jackpots_spend.append(spend_total)

    build_histogram(three_jackpots_spend, histogram_path)

    return {
        "variant": "v3_precomputed_cycle_distribution",
        "players": players,
        "seed": seed,
        "avg_spend_first_jackpot": sum(first_jackpot_spend) / players,
        "avg_spend_three_jackpots": sum(three_jackpots_spend) / players,
        "min_spend_three_jackpots": min(three_jackpots_spend),
        "max_spend_three_jackpots": max(three_jackpots_spend),
        "max_spins_in_single_cycle": len(cycle_cdf),
        "histogram_path": str(histogram_path),
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Simulation using a precomputed pity-cycle distribution.")
    parser.add_argument("--players", type=int, default=PLAYERS)
    parser.add_argument("--seed", type=int, default=SEED)
    parser.add_argument("--histogram", type=Path, default=Path("wheel_jackpot_hist_v3.png"))
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    result = run_simulation(players=args.players, seed=args.seed, histogram_path=args.histogram)
    print(json.dumps(result, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
