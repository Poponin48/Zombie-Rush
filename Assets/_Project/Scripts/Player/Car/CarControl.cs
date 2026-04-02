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

    [Header("Speed")]
    [Tooltip("Hard cap on planar speed (km/h). 0 = disabled.")]
    public float maxSpeedKmh = 70f;

    [Header("Drift")]
    [Tooltip("If off, friction is left as on the WheelCollider prefab.")]
    public bool useDrift = true;
    [Range(0f, 1f)]
    [Tooltip("Overall blend strength — how aggressively drift kicks in.")]
    public float driftAmount = 0.62f;
    [Range(0.15f, 1f)]
    [Tooltip("Rear-wheel sideways stiffness at full drift (lower = more rear slide).")]
    public float driftRearGripMin = 0.44f;
    [Range(0.5f, 1f)]
    [Tooltip("Front-wheel sideways stiffness at full drift (keep higher to maintain steering).")]
    public float driftFrontGripMin = 0.82f;
    [Tooltip("Planar speed (m/s) at which drift blend reaches ~1. Lower = drift starts earlier.")]
    public float driftSpeedRefMs = 8f;

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
    public float yawStraightenStrength = 4.5f;
    [Tooltip("Steer angle return speed multiplier when input is released.")]
    [Range(1f, 4f)] public float steerReturnMultiplier = 2.2f;

    [Header("Throttle Smoothing")]
    [Tooltip("How quickly throttle ramps up (higher = snappier).")]
    [Range(1f, 12f)] public float throttleRise = 5.5f;
    [Tooltip("How quickly throttle drops (higher = faster release).")]
    [Range(1f, 16f)] public float throttleFall = 8f;

    private Rigidbody rb;
    private float currentTurnAngle;
    private WheelCollider[] wheelColliders;
    private WheelFrictionCurve[] baseSideways;
    private VehicleCrowdBrake vehicleCrowdBrake;
    private float _smoothedThrottle;

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

        if (maxSpeedKmh > 0f)
        {
            Vector3 v = rb.linearVelocity;
            float planarMs = new Vector3(v.x, 0f, v.z).magnitude;
            float capMs = maxSpeedKmh / 3.6f;
            if (planarMs > capMs)
            {
                Vector3 planarDir = new Vector3(v.x, 0f, v.z).normalized;
                rb.linearVelocity = new Vector3(planarDir.x * capMs, v.y, planarDir.z * capMs);
            }
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
        float throttleRate = Mathf.Abs(verticalInput) > Mathf.Abs(_smoothedThrottle) ? throttleRise : throttleFall;
        float throttleT = Mathf.Clamp01(Time.fixedDeltaTime * throttleRate);
        _smoothedThrottle = Mathf.Lerp(_smoothedThrottle, verticalInput, throttleT);

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

        float driftBlend = 0f;
        if (useDrift && rb != null && driftAmount > 0.001f)
        {
            Vector3 v = rb.linearVelocity;
            float planar = new Vector3(v.x, 0f, v.z).magnitude;
            // Prefer live input so grip returns once keys are released.
            float steerFactor = Mathf.Max(
                Mathf.Abs(horizontalInput),
                Mathf.Abs(currentTurnAngle) / Mathf.Max(turnSpeed, 0.01f) * (steerReleased ? 0.35f : 1f));
            float speedFactor = Mathf.Clamp01(planar / Mathf.Max(driftSpeedRefMs, 0.5f));
            driftBlend = Mathf.Clamp01(driftAmount * steerFactor * speedFactor);
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
                // Rear wheels (i>=2) slide hard; front wheels keep grip for steering control.
                bool isRear = i >= 2;
                float gripMin = isRear ? driftRearGripMin : driftFrontGripMin;
                float gripMult = Mathf.Lerp(1f, gripMin, driftBlend);

                WheelFrictionCurve side = baseSideways[i];
                side.stiffness = baseSideways[i].stiffness * gripMult;
                wheelCollider.sidewaysFriction = side;
            }

            if (i < 2)
                wheelCollider.steerAngle = currentTurnAngle;
            else
                wheelCollider.steerAngle = 0f;

            float torqueMul = vehicleCrowdBrake != null ? vehicleCrowdBrake.MotorTorqueMultiplier : 1f;
            wheelCollider.motorTorque = _smoothedThrottle * enginePower * torqueMul;

            if (wheelMeshes != null && i < wheelMeshes.Length && wheelMeshes[i] != null)
            {
                wheelCollider.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
                wheelMeshes[i].position = wheelPosition;
                wheelMeshes[i].rotation = wheelRotation;
            }
        }
    }
}
