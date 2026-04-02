from __future__ import annotations

import argparse
import json
import random
from pathlib import Path

import matplotlib.pyplot as plt


PLAYERS = 10_000
TARGET_JACKPOTS = 3
BASE_SPIN_COST = 100
COST_STEP = 50
BASE_JACKPOT_CHANCE = 0.01
PITY_STEP = 0.005
SEED = 20260316

# Base wheel shares from the design config.
RESOURCE_SHARE = 50
BOOSTER_SHARE = 40
HERO_SHARD_SHARE = 9
NON_JACKPOT_TOTAL = RESOURCE_SHARE + BOOSTER_SHARE + HERO_SHARD_SHARE


def jackpot_chance(misses_since_last_jackpot: int) -> float:
    return min(BASE_JACKPOT_CHANCE + misses_since_last_jackpot * PITY_STEP, 1.0)


def spin_wheel(rng: random.Random, current_jackpot_chance: float) -> str:
    roll = rng.random()
    if roll < current_jackpot_chance:
        return "jackpot"

    normalized_roll = (roll - current_jackpot_chance) / (1.0 - current_jackpot_chance)
    resource_cutoff = RESOURCE_SHARE / NON_JACKPOT_TOTAL
    booster_cutoff = (RESOURCE_SHARE + BOOSTER_SHARE) / NON_JACKPOT_TOTAL

    if normalized_roll < resource_cutoff:
        return "resources"
    if normalized_roll < booster_cutoff:
        return "boosters"
    return "hero_shards"


def simulate_player(rng: random.Random) -> tuple[int, int]:
    total_spend = 0
    spend_to_first_jackpot = 0
    jackpots = 0
    misses_since_last_jackpot = 0
    current_spin_cost = BASE_SPIN_COST

    while jackpots < TARGET_JACKPOTS:
        total_spend += current_spin_cost
        current_chance = jackpot_chance(misses_since_last_jackpot)
        reward = spin_wheel(rng, current_chance)

        if reward == "jackpot":
            jackpots += 1
            if jackpots == 1:
                spend_to_first_jackpot = total_spend
            misses_since_last_jackpot = 0
            current_spin_cost += COST_STEP
        else:
            misses_since_last_jackpot += 1

    return spend_to_first_jackpot, total_spend


def build_histogram(total_spend_values: list[int], output_path: Path) -> None:
    plt.figure(figsize=(11, 6))
    plt.hist(total_spend_values, bins=50, color="#4c78a8", edgecolor="white")
    plt.title("Wheel of Fortune: cost distribution for 3 jackpots")
    plt.xlabel("Coins spent")
    plt.ylabel("Players")
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close()


def run_simulation(players: int, seed: int, histogram_path: Path) -> dict[str, float | int | str]:
    rng = random.Random(seed)
    first_jackpot_spend: list[int] = []
    three_jackpots_spend: list[int] = []

    for _ in range(players):
        first_spend, total_spend = simulate_player(rng)
        first_jackpot_spend.append(first_spend)
        three_jackpots_spend.append(total_spend)

    build_histogram(three_jackpots_spend, histogram_path)

    return {
        "variant": "v1_basic_step_by_step",
        "players": players,
        "seed": seed,
        "avg_spend_first_jackpot": sum(first_jackpot_spend) / players,
        "avg_spend_three_jackpots": sum(three_jackpots_spend) / players,
        "min_spend_three_jackpots": min(three_jackpots_spend),
        "max_spend_three_jackpots": max(three_jackpots_spend),
        "histogram_path": str(histogram_path),
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Basic Monte Carlo simulation for Wheel of Fortune jackpot economy.")
    parser.add_argument("--players", type=int, default=PLAYERS)
    parser.add_argument("--seed", type=int, default=SEED)
    parser.add_argument("--histogram", type=Path, default=Path("wheel_jackpot_hist_v1.png"))
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    result = run_simulation(players=args.players, seed=args.seed, histogram_path=args.histogram)
    print(json.dumps(result, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
