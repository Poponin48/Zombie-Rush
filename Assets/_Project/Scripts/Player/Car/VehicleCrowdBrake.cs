using System.Collections.Generic;
using Project.Zombie;
using UnityEngine;

namespace Project.Player.Car
{
    /// <summary>
    /// Aggregates touching zombies and reduces motor torque multiplier (crowd slowdown).
    /// </summary>
    public class VehicleCrowdBrake : MonoBehaviour
    {
        [SerializeField] private CarStats carStats;
        [SerializeField, Tooltip("Higher = stronger slowdown for the same number of contacts.")]
        [Range(0.1f, 2f)]
        private float crowdSensitivity = 0.85f;

        [SerializeField, Tooltip("Maximum torque reduction at full crowd.")]
        [Range(0.2f, 0.95f)]
        private float maxTorqueCut = 0.82f;

        private readonly HashSet<ZombieCrowdResistance> _contacts = new HashSet<ZombieCrowdResistance>();

        /// <summary>Motor torque multiplier 0..1 applied by CarControl.</summary>
        public float MotorTorqueMultiplier => ComputeMultiplier();

        /// <summary>Used by zombies to scale run-over speed vs truck Power.</summary>
        public CarStats CarStats => carStats;

        public void Register(ZombieCrowdResistance zombie)
        {
            if (zombie == null || zombie.IsDead)
                return;

            _contacts.Add(zombie);
        }

        public void Unregister(ZombieCrowdResistance zombie)
        {
            if (zombie == null)
                return;

            _contacts.Remove(zombie);
        }

        private float ComputeMultiplier()
        {
            float sum = 0f;
            foreach (var z in _contacts)
            {
                if (z == null || z.IsDead)
                    continue;

                sum += z.SlowdownContribution;
            }

            // Saturating curve with softer low-end: 1 zombie barely affects torque,
            // packs still become dangerous.
            float tRaw = 1f - Mathf.Exp(-sum * crowdSensitivity);
            float t = Mathf.Pow(Mathf.Clamp01(tRaw), 1.65f);

            float powerMitigation = 1f;
            if (carStats != null)
            {
                // power > 1 still counts as full mitigation (Clamp01).
                powerMitigation = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(carStats.power));
            }

            float cut = t * maxTorqueCut * (2f - powerMitigation);
            cut = Mathf.Clamp01(cut);
            return Mathf.Clamp01(1f - cut);
        }
    }
}
