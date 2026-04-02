using UnityEngine;

namespace Project.Player.Car
{
    [CreateAssetMenu(menuName = "Game/Car Stats", fileName = "CarStats")]
    public class CarStats : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("Maximum speed in km/h.")]
        public float maxSpeedKmh = 95f;

        [Tooltip("How quickly the truck reaches its max speed (used for UI / upgrade display).")]
        public float acceleration = 28f;

        [Header("Handling")]
        [Range(0.1f, 1f)]
        [Tooltip("Steering responsiveness multiplier (scales turn rate).")]
        public float handling = 0.88f;

        [Range(0.1f, 1f)]
        [Tooltip("Arcade: higher = stronger lateral grip correction. Lower = easier slide (with speedDrift / handbrake).")]
        public float stability = 0.42f;

        [Header("Crowd Interaction")]
        [Tooltip("Pushes through crowds / run-over (higher = better). Values above 1 are OK; gameplay code clamps as needed.")]
        public float power = 0.5f;

        [Header("Fuel")]
        [Tooltip("Maximum fuel capacity for this truck.")]
        public float maxFuel = 200f;

        [Header("Cargo")]
        [Tooltip("Maximum cargo weight the truck can carry.")]
        public float maxCargoWeight = 5000f;

        [Header("Modules")]
        [Tooltip("Number of module slots available on this truck.")]
        public int moduleSlots = 3;

        [Header("Arcade Drive")]
        [Tooltip("Forward force applied when accelerating (Newtons). With mass 2000: 10000 ≈ 5 m/s².")]
        public float driveForce = 10000f;

        [Tooltip("Braking force (Newtons). Higher = faster stop.")]
        public float brakeForce = 12000f;

        [Tooltip("Handbrake force (Newtons).")]
        public float handbrakeForce = 10000f;

        [Header("Arcade Steering")]
        [Tooltip("Maximum turn rate in degrees per second.")]
        public float maxTurnRate = 145f;

        [Tooltip("Maximum visual steering angle for front wheels (degrees).")]
        public float maxSteerAngle = 38f;

        [Range(0.15f, 1f)]
        [Tooltip("Turn rate multiplier at max speed. Lower = more stable at high speed.")]
        public float highSpeedSteerFactor = 0.55f;

        [Header("Arcade Grip")]
        [Tooltip("Reserved for future tuning / arcade drive systems (sideways correction).")]
        public float lateralGrip = 8.5f;

        [Range(0f, 1f)]
        [Tooltip("Grip multiplier during handbrake. Lower = more drift (0 = full slide, 1 = no drift).")]
        public float handbrakeGripFactor = 0.2f;

        [Header("Physics")]
        [Tooltip("Downforce coefficient — presses the truck to the road at speed.")]
        public float downforce = 80f;
    }
}
