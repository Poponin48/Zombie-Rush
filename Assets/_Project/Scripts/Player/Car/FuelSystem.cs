using System;
using UnityEngine;

namespace Project.Player.Car
{
    public class FuelSystem : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private CarStats stats;
        [SerializeField, Tooltip("Fuel consumption per second at full throttle.")]
        private float baseConsumptionPerSecond = 1f;
        [SerializeField, Tooltip("Extra consumption factor based on current speed (0..1).")]
        private float speedConsumptionFactor = 0.5f;

        [Header("Runtime (Read Only)")]
        [SerializeField] private float currentFuel;

        public float CurrentFuel => currentFuel;
        public float MaxFuel => stats != null ? stats.maxFuel : 0f;
        public bool IsEmpty => currentFuel <= 0.01f;

        public event Action<float, float> OnFuelChanged; // current, max
        public event Action OnFuelEmpty;

        private void Awake()
        {
            if (stats == null)
            {
                Debug.LogWarning($"{nameof(FuelSystem)} on {name} has no CarStats assigned.");
                return;
            }

            currentFuel = stats.maxFuel;
            RaiseFuelChanged();
        }

        /// <summary>
        /// Call from movement code every physics step.
        /// </summary>
        public void Consume(float throttleInput01, float speedKmh, float deltaTime)
        {
            if (stats == null || IsEmpty)
            {
                return;
            }

            throttleInput01 = Mathf.Clamp01(Mathf.Abs(throttleInput01));
            float speedFactor = Mathf.Clamp01(speedKmh / stats.maxSpeedKmh);

            float consumption = baseConsumptionPerSecond * throttleInput01;
            consumption += baseConsumptionPerSecond * speedConsumptionFactor * speedFactor;
            consumption *= deltaTime;

            currentFuel -= consumption;
            if (currentFuel <= 0f)
            {
                currentFuel = 0f;
                RaiseFuelChanged();
                OnFuelEmpty?.Invoke();
                return;
            }

            RaiseFuelChanged();
        }

        public void Refuel(float amount)
        {
            if (stats == null || amount <= 0f)
            {
                return;
            }

            currentFuel = Mathf.Clamp(currentFuel + amount, 0f, stats.maxFuel);
            RaiseFuelChanged();
        }

        public void SetFuelToMax()
        {
            if (stats == null)
            {
                return;
            }

            currentFuel = stats.maxFuel;
            RaiseFuelChanged();
        }

        private void RaiseFuelChanged()
        {
            OnFuelChanged?.Invoke(currentFuel, MaxFuel);
        }
    }
}

