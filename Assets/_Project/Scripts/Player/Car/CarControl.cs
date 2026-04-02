using Project.Player.Car;
using UnityEngine;

/// <summary>
/// WheelCollider pickup drive (Pack_Pickup) + optional arcade drift (sideways grip only).
/// </summary>
public class CarControl : MonoBehaviour
{
    public float enginePower = 2000f;
    public float turnSpeed = 35f;
    public float turnSmoothness = 5f;
    public Transform[] wheels;
    public Transform[] wheelMeshes;
    public Transform centerOfMass;
    public GameObject steeringWheel;

    [Header("Drift (subtle)")]
    [Tooltip("If off, friction is left as on the WheelCollider prefab.")]
    public bool useDrift = true;
    [Range(0f, 1f)]
    [Tooltip("How strong the grip reduction is at full blend (steer + speed).")]
    public float driftAmount = 0.3f;
    [Range(0.78f, 1f)]
    [Tooltip("Sideways stiffness multiplier at full drift blend (closer to 1 = less slide).")]
    public float driftSidewaysGripMin = 0.88f;
    [Tooltip("Planar speed (m/s) at which drift blend reaches ~1. Lower = drift starts earlier.")]
    public float driftSpeedRefMs = 17f;

    [Header("Fuel (optional)")]
    [SerializeField, Tooltip("If assigned, throttle consumes fuel and motor is cut when empty.")]
    private FuelSystem fuelSystem;

    [Header("Durability (optional)")]
    [SerializeField, Tooltip("If assigned, motor is cut when truck HP reaches 0.")]
    private VehicleHealth vehicleHealth;

    [Header("Straighten (release steer)")]
    [Tooltip("If |Horizontal| is below this, we treat steer as released — damp yaw and return wheels faster.")]
    [Range(0.02f, 0.35f)] public float steerInputDeadzone = 0.12f;
    [Tooltip("How fast yaw spin decays when steer is released (lower = more inertia / slide after drift).")]
    public float yawStraightenStrength = 7f;
    [Tooltip("Steer angle return speed multiplier when input is released.")]
    [Range(1f, 4f)] public float steerReturnMultiplier = 2.2f;

    private Rigidbody rb;
    private float currentTurnAngle;
    private WheelCollider[] wheelColliders;
    private WheelFrictionCurve[] baseSideways;
    private VehicleCrowdBrake vehicleCrowdBrake;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null)
            rb.centerOfMass = centerOfMass.localPosition;

        // Reduces visible stutter when the view follows the car between physics steps.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        CacheWheels();

        if (fuelSystem == null)
            fuelSystem = GetComponent<FuelSystem>();

        if (vehicleHealth == null)
            vehicleHealth = GetComponent<VehicleHealth>();

        vehicleCrowdBrake = GetComponent<VehicleCrowdBrake>();
    }

    private void CacheWheels()
    {
        if (wheels == null || wheels.Length == 0) return;
        wheelColliders = new WheelCollider[wheels.Length];
        baseSideways = new WheelFrictionCurve[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            wheelColliders[i] = wc;
            if (wc != null)
                baseSideways[i] = wc.sidewaysFriction;
        }
    }

    private void FixedUpdate()
    {
        if (wheels == null || wheels.Length == 0) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        float speedKmh = 0f;
        if (rb != null)
        {
            Vector3 vel = rb.linearVelocity;
            float planarMs = new Vector3(vel.x, 0f, vel.z).magnitude;
            speedKmh = planarMs * 3.6f;
        }

        if (fuelSystem != null)
        {
            if (!fuelSystem.IsEmpty)
                fuelSystem.Consume(verticalInput, speedKmh, Time.fixedDeltaTime);
            if (fuelSystem.IsEmpty)
                verticalInput = 0f;
        }

        if (vehicleHealth != null && !vehicleHealth.IsAlive)
            verticalInput = 0f;

        bool steerReleased = Mathf.Abs(horizontalInput) <= steerInputDeadzone;

        float targetTurnAngle = horizontalInput * turnSpeed;
        float steerRate = turnSmoothness * (steerReleased ? steerReturnMultiplier : 1f);
        float steerT = Mathf.Clamp01(Time.fixedDeltaTime * steerRate);
        currentTurnAngle = Mathf.Lerp(currentTurnAngle, targetTurnAngle, steerT);

        if (steerReleased && rb != null)
        {
            Vector3 av = rb.angularVelocity;
            av.y *= Mathf.Exp(-yawStraightenStrength * Time.fixedDeltaTime);
            rb.angularVelocity = av;
        }

        if (steeringWheel != null)
            steeringWheel.transform.localEulerAngles = new Vector3(-64f, 0f, currentTurnAngle * 3f);

        float driftGripMult = 1f;
        if (useDrift && rb != null && driftAmount > 0.001f)
        {
            Vector3 v = rb.linearVelocity;
            float planar = new Vector3(v.x, 0f, v.z).magnitude;
            // Prefer live input so grip returns once keys are released, not only when wheels finish centering.
            float steerFactor = Mathf.Max(
                Mathf.Abs(horizontalInput),
                Mathf.Abs(currentTurnAngle) / Mathf.Max(turnSpeed, 0.01f) * (steerReleased ? 0.35f : 1f));
            float speedFactor = Mathf.Clamp01(planar / Mathf.Max(driftSpeedRefMs, 0.5f));
            float blend = Mathf.Clamp01(driftAmount * steerFactor * speedFactor);
            driftGripMult = Mathf.Lerp(1f, driftSidewaysGripMin, blend);
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheelCollider = wheelColliders != null && i < wheelColliders.Length
                ? wheelColliders[i]
                : wheels[i].GetComponent<WheelCollider>();
            if (wheelCollider == null) continue;

            if (useDrift && baseSideways != null && i < baseSideways.Length &&
                wheelColliders != null && i < wheelColliders.Length && wheelColliders[i] != null)
            {
                WheelFrictionCurve side = baseSideways[i];
                side.stiffness = baseSideways[i].stiffness * driftGripMult;
                wheelCollider.sidewaysFriction = side;
            }

            if (i < 2)
                wheelCollider.steerAngle = currentTurnAngle;
            else
                wheelCollider.steerAngle = 0f;

            float torqueMul = vehicleCrowdBrake != null ? vehicleCrowdBrake.MotorTorqueMultiplier : 1f;
            wheelCollider.motorTorque = verticalInput * enginePower * torqueMul;

            if (wheelMeshes != null && i < wheelMeshes.Length && wheelMeshes[i] != null)
            {
                wheelCollider.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
                wheelMeshes[i].position = wheelPosition;
                wheelMeshes[i].rotation = wheelRotation;
            }
        }
    }
}
