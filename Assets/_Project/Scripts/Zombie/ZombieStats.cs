using UnityEngine;

namespace Project.Zombie
{
    [CreateAssetMenu(menuName = "Game/Zombie Stats", fileName = "ZombieStats")]
    public class ZombieStats : ScriptableObject
    {
        [Header("Combat")]
        [Min(1f)]
        public float maxHealth = 40f;

        [Header("Movement")]
        [Tooltip("NavMeshAgent max speed (units/sec).")]
        [Range(1f, 50f)]
        public float moveSpeed = 6f;

        [Tooltip("When player is within this radius, zombie starts chasing.")]
        [Range(5f, 150f)]
        public float activationRadius = 45f;

        [Tooltip("Horizontal distance from the player pivot where NavMesh stops chasing. Must exceed ~half truck length from pivot to bumper or zombies overlap the hull.")]
        [Range(0.8f, 14f)]
        public float stopChasingPlayerDistance = 4.5f;

        [Tooltip("Extra meters so the capsule stays outside the BoxCollider hull (pivot is usually at center, not bumper).")]
        [Range(0f, 6f)]
        public float extraClearanceFromTruckPivot = 2.25f;

        [Header("Vehicle interaction")]
        [Tooltip("How much this zombie contributes to engine slowdown while touching the truck.")]
        [Range(0.02f, 1f)]
        public float slowdownToVehicle = 0.12f;

        [Tooltip("HP removed from the truck per hit while this zombie stays in contact.")]
        [Min(0f)]
        public float damageToVehiclePerHit = 5f;

        [Tooltip("Seconds between truck HP hits per zombie.")]
        [Min(0.1f)]
        public float vehicleDamageHitIntervalSeconds = 2f;

        [Tooltip("Minimum truck planar speed (km/h) to run this zombie over. Below this, contact = push + truck HP drain only. Truck Power reduces this bar.")]
        [Range(5f, 120f)]
        public float runOverSpeedKmh = 50f;

        [Tooltip("Planar speed removed from the truck when this zombie is run over (each zombie applies its own; many hits stack naturally).")]
        [Min(0f)]
        public float truckSpeedLossKmhPerRunOver = 5f;

        [Tooltip("Horizontal knock speed (m/s) applied to the zombie body on run-over (placeholder slide before hide).")]
        [Min(0f)]
        public float runOverKnockPlanarSpeed = 7f;

        [Tooltip("Seconds the corpse keeps sliding after run-over.")]
        [Min(0.05f)]
        public float runOverKnockDurationSeconds = 0.4f;

        [Tooltip("Reserved for future attach mechanic.")]
        [Range(0f, 1f)]
        public float attachCoefficient = 0.5f;

        [Tooltip("Rigidbody mass (keep well below truck ~1000). Also scales NavMeshAgent radius.")]
        [Range(1f, 120f)]
        public float mass = 70f;

        [Header("Physics body (NavMesh + Rigidbody)")]
        [Tooltip("Rigidbody linearDamping — higher = less sliding after the truck hits.")]
        [Min(0f)]
        public float bodyLinearDamping = 4f;

        [Tooltip("If on, locks pitch/roll so the capsule stays upright while walking.")]
        public bool freezePitchRoll = true;
    }
}
