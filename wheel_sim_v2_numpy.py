from __future__ import annotations

import argparse
import json
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np


PLAYERS = 10_000
BASE_SPIN_COST = 100
COST_STEP = 50
BASE_JACKPOT_CHANCE = 0.01
PITY_STEP = 0.005
SEED = 20260316


def simulate_cycle_lengths(player_count: int, rng: np.random.Generator) -> np.ndarray:
    spins = np.zeros(player_count, dtype=np.int32)
    misses = np.zeros(player_count, dtype=np.int32)
    finished = np.zeros(player_count, dtype=bool)

    while not finished.all():
        active_idx = np.flatnonzero(~finished)
        current_chance = np.minimum(BASE_JACKPOT_CHANCE + misses[active_idx] * PITY_STEP, 1.0)
        rolls = rng.random(active_idx.size)
        jackpot_mask = rolls < current_chance

        spins[active_idx] += 1
        finished[active_idx[jackpot_mask]] = True
        misses[active_idx[~jackpot_mask]] += 1

    return spins


def build_histogram(total_spend_values: np.ndarray, output_path: Path) -> None:
    plt.figure(figsize=(11, 6))
    plt.hist(total_spend_values, bins=50, color="#f58518", edgecolor="white")
    plt.title("Wheel of Fortune: cost distribution for 3 jackpots")
    plt.xlabel("Coins spent")
    plt.ylabel("Players")
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close()


def run_simulation(players: int, seed: int, histogram_path: Path) -> dict[str, float | int | str]:
    rng = np.random.default_rng(seed)

    first_cycle_spins = simulate_cycle_lengths(players, rng)
    second_cycle_spins = simulate_cycle_lengths(players, rng)
    third_cycle_spins = simulate_cycle_lengths(players, rng)

    spend_first = first_cycle_spins * BASE_SPIN_COST
    spend_total = (
        first_cycle_spins * BASE_SPIN_COST
        + second_cycle_spins * (BASE_SPIN_COST + COST_STEP)
        + third_cycle_spins * (BASE_SPIN_COST + 2 * COST_STEP)
    )

    build_histogram(spend_total, histogram_path)

    return {
        "variant": "v2_numpy_vectorized",
        "players": players,
        "seed": seed,
        "avg_spend_first_jackpot": float(spend_first.mean()),
        "avg_spend_three_jackpots": float(spend_total.mean()),
        "min_spend_three_jackpots": int(spend_total.min()),
        "max_spend_three_jackpots": int(spend_total.max()),
        "histogram_path": str(histogram_path),
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Vectorized Monte Carlo simulation for Wheel of Fortune jackpot economy.")
    parser.add_argument("--players", type=int, default=PLAYERS)
    parser.add_argument("--seed", type=int, default=SEED)
    parser.add_argument("--histogram", type=Path, default=Path("wheel_jackpot_hist_v2.png"))
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    result = run_simulation(players=args.players, seed=args.seed, histogram_path=args.histogram)
    print(json.dumps(result, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
