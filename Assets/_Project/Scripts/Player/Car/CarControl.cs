using System;
using Project.Player.Car;
using UnityEngine;

/// <summary>
/// WheelCollider pickup drive (Pack_Pickup) + optional arcade drift (sideways grip only).
/// </summary>
public class CarControl : MonoBehaviour
{
    [Serializable]
    private class GripProfile
    {
        public string name = "Asphalt";
        public LayerMask layers;
        [Range(0.2f, 2f)] public float forwardStiffness = 1f;
        [Range(0.2f, 2f)] public float sidewaysStiffness = 1f;
    }

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

    [Header("Braking")]
    [Tooltip("Base brake force for all wheels.")]
    public float brakePower = 3000f;
    [Tooltip("Brake force applied when W is released and no reverse input is active.")]
    public float releaseBrakePower = 2600f;
    [Tooltip("Extra rear brake force while handbrake is active.")]
    public float handbrakePower = 6500f;
    [Tooltip("Rear wheel grip multiplier during handbrake.")]
    [Range(0.1f, 1f)] public float handbrakeRearGrip = 0.35f;

    [Header("Coasting")]
    [Tooltip("Rigidbody linear damping while accelerating or reversing.")]
    [SerializeField, Range(0f, 0.3f)] private float driveLinearDamping = 0.03f;
    [Tooltip("Rigidbody linear damping after releasing throttle. Keep low if using constant coast decel (drag ∝ speed makes slowdown non-linear).")]
    [SerializeField, Range(0f, 0.5f)] private float coastLinearDamping = 0.02f;
    [Tooltip("If true, coasting uses constant planar deceleration (linear speed vs time). Wheel release-brake is skipped.")]
    [SerializeField] private bool useConstantCoastDecel = true;
    [Tooltip("Planar speed loss rate while coasting (m/s²). ~3.5 ≈ 12.6 km/h per second.")]
    [SerializeField, Min(0.1f)] private float coastDecelerationMps2 = 3.4f;
    [Tooltip("When not steering, damp lateral planar velocity vs car forward so the truck does not crab-drift.")]
    [SerializeField, Min(0f)] private float coastLateralStabilize = 14f;

    [Header("Transmission (Auto only)")]
    [SerializeField, Min(1)] private int maxForwardGears = 5;
    [SerializeField] private float[] gearRatios = { 3.2f, 2.2f, 1.6f, 1.2f, 0.95f };
    [SerializeField, Min(0.05f)] private float finalDrive = 3.1f;
    [SerializeField, Min(800f)] private float minEngineRpm = 900f;
    [SerializeField, Min(2000f)] private float maxEngineRpm = 7000f;
    [SerializeField, Range(0f, 1f)] private float upshiftRpmThreshold = 0.88f;
    [SerializeField, Range(0f, 1f)] private float downshiftRpmThreshold = 0.38f;
    [SerializeField, Min(0.05f)] private float shiftDelay = 0.18f;

    [Header("Surface Grip Profiles")]
    [SerializeField] private GripProfile[] gripProfiles = Array.Empty<GripProfile>();
    [SerializeField, Range(0.1f, 1.5f)] private float defaultForwardStiffness = 1f;
    [SerializeField, Range(0.1f, 1.5f)] private float defaultSidewaysStiffness = 1f;

    [Header("Body Roll")]
    [SerializeField] private Transform bodyVisual;
    [SerializeField, Min(0.5f)] private float bodyRollAngle = 2.1f;
    [SerializeField, Min(1f)] private float bodyRollSpeed = 7.8f;

    [Header("Suspension Stiffness")]
    [SerializeField, Range(0.4f, 3f)] private float suspensionSpringMultiplier = 1.45f;
    [SerializeField, Range(0.4f, 3f)] private float suspensionDamperMultiplier = 1.3f;
    [SerializeField, Range(0.6f, 3f)] private float suspensionDistanceMultiplier = 1f;
    [SerializeField, Range(0.6f, 2f)] private float wheelDampingRateMultiplier = 1f;
    [SerializeField, Range(-0.2f, 0.2f)] private float suspensionTargetPositionOffset;
    [SerializeField, Min(0f)] private float forceAppPointDistance = 0.02f;
    [Tooltip("Per wheel (FL, FR, BL, BR): extra suspension travel scale on top of global multiplier. Empty = 1 each.")]
    [SerializeField] private float[] perWheelSuspensionDistanceScale;
    [Tooltip("Per wheel spring stiffness scale. Empty = 1 each.")]
    [SerializeField] private float[] perWheelSpringScale;
    [Tooltip("Per wheel damper scale. Empty = 1 each.")]
    [SerializeField] private float[] perWheelDamperScale;
    [SerializeField, Min(1000f)] private float targetVehicleMass = 1700f;
    [SerializeField, Range(0.05f, 2f)] private float pitchStabilization = 0.85f;
    [Tooltip("Lower = less violent collision separation bounce (helps on uneven logs).")]
    [SerializeField, Range(1f, 10f)] private float maxDepenetrationVelocity = 3.5f;

    [Header("Effects / Audio")]
    [SerializeField] private AudioClip engineIdleClip;
    [SerializeField] private AudioClip engineLoadClip;
    [SerializeField] private AudioClip skidLoopClip;
    [SerializeField] private AudioClip brakeLoopClip;
    [SerializeField, Range(0f, 1f)] private float engineIdleVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float engineLoadVolume = 0.65f;
    [SerializeField, Range(0.6f, 1.2f)] private float enginePitchMultiplier = 0.82f;
    [SerializeField, Range(0f, 1f)] private float skidVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float brakeVolume = 0.45f;
    [SerializeField, Range(0.5f, 1f)] private float brakePitchMultiplier = 0.75f;
    [SerializeField] private bool createTireMarks = true;
    [SerializeField] private float slipForEffects = 0.26f;
    [SerializeField, Tooltip("Slip threshold used only for skid sound playback.")]
    private float skidSoundSlipThreshold = 0.38f;
    [SerializeField, Tooltip("Skid sound starts only above this speed.")]
    private float skidMinSpeedKmh = 10f;
    [SerializeField, Tooltip("Skid effects and sound play only if steering exceeds this threshold.")]
    private float skidTurnInputThreshold = 0.12f;
    [SerializeField, Tooltip("Steering hold time before drift mode is enabled.")]
    private float skidTurnHoldSeconds = 0.5f;
    [SerializeField, Tooltip("If steering is sharp enough and speed is high enough, drift can arm instantly.")]
    private bool allowInstantDriftAtHighSteer = true;
    [SerializeField, Range(0.5f, 1f), Tooltip("Steering threshold for instant drift arming.")]
    private float instantDriftSteerThreshold = 0.82f;
    [SerializeField, Min(5f), Tooltip("Minimum speed (km/h) for instant drift arming.")]
    private float instantDriftMinSpeedKmh = 28f;
    [SerializeField] private Material tireMarkMaterial;

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

    [Header("Steering Ramp")]
    [Tooltip("Time in seconds to reach full steering lock when input is held.")]
    [SerializeField, Min(0.05f)] private float steerRampUpTime = 0.7f;
    [Tooltip("Time in seconds to return steering back to zero after releasing input.")]
    [SerializeField, Min(0.05f)] private float steerRampDownTime = 0.35f;
    [Tooltip("Immediate steering fraction at input press; then linearly ramps to 100% over Steer Ramp Up Time.")]
    [SerializeField, Range(0f, 0.45f)] private float steerRampInitial = 0.2f;
    [Tooltip("Input axis values below this threshold are ignored to prevent tiny steering bias from devices/noise.")]
    [SerializeField, Range(0f, 0.35f)] private float steerAxisNoiseDeadzone = 0.2f;

    [Header("Handling Assist")]
    [Tooltip("Overall steering heaviness. Lower = heavier steering response.")]
    [SerializeField, Range(0.55f, 1f)] private float steeringWeight = 0.82f;
    [Tooltip("Boosts drift blend so the car enters slide a bit easier.")]
    [SerializeField, Range(1f, 1.7f)] private float driftEntryBoost = 1.18f;
    [Tooltip("Lowers speed threshold for drift activation (lower = earlier drift entry).")]
    [SerializeField, Range(0.7f, 1f)] private float driftSpeedEntryMultiplier = 0.86f;
    [Tooltip("At full-speed turns in drift, hold this fraction of max speed instead of allowing unstable 180 spins.")]
    [SerializeField, Range(0.6f, 1f)] private float driftTurnSpeedLimit01 = 0.8f;
    [Tooltip("Steering amount required for high-speed drift stabilization/cap.")]
    [SerializeField, Range(0.4f, 1f)] private float highSpeedDriftSteerThreshold = 0.72f;
    [Tooltip("Yaw stabilization strength while high-speed drift assist is active.")]
    [SerializeField, Min(0f)] private float highSpeedDriftYawDamping = 5.8f;
    [Tooltip("Maximum yaw angular velocity (rad/s) while high-speed drift assist is active.")]
    [SerializeField, Min(0.1f)] private float highSpeedDriftMaxYawRate = 1.95f;
    [Tooltip("How quickly speed is pulled down to drift turn speed cap (m/s²).")]
    [SerializeField, Min(0.1f)] private float highSpeedDriftCapDecelMps2 = 10f;
    [Tooltip("Keep drift assist active briefly after speed/steer dips to prevent sudden spin on long held turns.")]
    [SerializeField, Min(0f)] private float driftAssistGraceSeconds = 0.45f;
    [Tooltip("While throttle is held in drift, keep at least this fraction of drift speed cap.")]
    [SerializeField, Range(0.55f, 1f)] private float driftSustainSpeedFloor01 = 0.78f;
    [Tooltip("Forward acceleration used only to maintain drift sustain speed floor.")]
    [SerializeField, Min(0f)] private float driftSustainAccelMps2 = 5.5f;
    [Tooltip("If planar velocity points backwards too much during drift, blend it toward forward to avoid 180 spin.")]
    [SerializeField, Range(-0.2f, 0.4f)] private float driftReverseGuardDot = 0.1f;
    [SerializeField, Min(0f)] private float driftReverseRecoverStrength = 8f;

    [Header("Throttle Smoothing")]
    [Tooltip("How quickly throttle ramps up (higher = snappier).")]
    [Range(1f, 12f)] public float throttleRise = 5.5f;
    [Tooltip("How quickly throttle drops (higher = faster release).")]
    [Range(1f, 16f)] public float throttleFall = 8f;

    [Header("Debug Input Override (Tests)")]
    [SerializeField, Tooltip("Allows scripted tests to inject drive input without keyboard.")]
    private bool useDebugInputOverride;
    [SerializeField, Range(-1f, 1f)] private float debugSteerInput;
    [SerializeField, Range(-1f, 1f)] private float debugThrottleInput;
    [SerializeField] private bool debugHandbrakeInput;

    private Rigidbody rb;
    private float currentTurnAngle;
    private WheelCollider[] wheelColliders;
    private WheelFrictionCurve[] baseSideways;
    private WheelFrictionCurve[] baseForward;
    private JointSpring[] _baseSuspensionSpring;
    private float[] _baseSuspensionDistance;
    private float[] _baseWheelDampingRate;
    private TrailRenderer[] tireTrails;
    private ParticleSystem[] tireSmoke;
    private VehicleCrowdBrake vehicleCrowdBrake;
    private float _smoothedThrottle;
    private int _currentGear = 1;
    private float _estimatedEngineRpm;
    private float _nextShiftTime;
    private float _currentBodyRoll;
    private Quaternion _bodyInitialLocalRotation = Quaternion.identity;
    private AudioSource _engineIdleSource;
    private AudioSource _engineLoadSource;
    private AudioSource _skidSource;
    private AudioSource _brakeSource;
    private float _turnHoldTimer;
    private float _steerRamp01;
    private bool _highSpeedDriftAssistActive;
    private float _driftAssistHoldTimer;
    public int CurrentGear => _currentGear;
    public float EstimatedEngineRpm => _estimatedEngineRpm;
    public float PlanarSpeedKmh
    {
        get
        {
            if (rb == null)
                return 0f;
            Vector3 planar = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            return planar.magnitude * 3.6f;
        }
    }

    public void SetDebugInputOverride(bool enabled)
    {
        useDebugInputOverride = enabled;
        if (!enabled)
        {
            debugSteerInput = 0f;
            debugThrottleInput = 0f;
            debugHandbrakeInput = false;
        }
    }

    public void SetDebugInput(float steer, float throttle, bool handbrake)
    {
        debugSteerInput = Mathf.Clamp(steer, -1f, 1f);
        debugThrottleInput = Mathf.Clamp(throttle, -1f, 1f);
        debugHandbrakeInput = handbrake;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null)
        {
            rb.automaticCenterOfMass = false;
            rb.centerOfMass = centerOfMass.localPosition;
        }

        // Reduces visible stutter when the view follows the car between physics steps.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = Mathf.Max(1000f, targetVehicleMass);
        rb.angularDamping = Mathf.Max(0.05f, rb.angularDamping);
        rb.maxDepenetrationVelocity = maxDepenetrationVelocity;
        rb.solverIterations = Mathf.Max(rb.solverIterations, 12);
        rb.solverVelocityIterations = Mathf.Max(rb.solverVelocityIterations, 2);

        CacheWheels();

        if (fuelSystem == null)
            fuelSystem = GetComponent<FuelSystem>();

        if (vehicleHealth == null)
            vehicleHealth = GetComponent<VehicleHealth>();

        vehicleCrowdBrake = GetComponent<VehicleCrowdBrake>();
        if (bodyVisual == null && transform.childCount > 0)
            bodyVisual = transform.GetChild(0);
        if (bodyVisual != null)
            _bodyInitialLocalRotation = bodyVisual.localRotation;

        ApplySuspensionTuning();
        EnsureAudioSources();
        BuildTireEffects();
    }

    private void CacheWheels()
    {
        if (wheels == null || wheels.Length == 0) return;
        wheelColliders = new WheelCollider[wheels.Length];
        baseSideways = new WheelFrictionCurve[wheels.Length];
        baseForward = new WheelFrictionCurve[wheels.Length];
        _baseSuspensionSpring = new JointSpring[wheels.Length];
        _baseSuspensionDistance = new float[wheels.Length];
        _baseWheelDampingRate = new float[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            wheelColliders[i] = wc;
            if (wc != null)
            {
                baseSideways[i] = wc.sidewaysFriction;
                baseForward[i] = wc.forwardFriction;
                _baseSuspensionSpring[i] = wc.suspensionSpring;
                _baseSuspensionDistance[i] = wc.suspensionDistance;
                _baseWheelDampingRate[i] = wc.wheelDampingRate;
            }
        }
    }

    private void ApplySuspensionTuning()
    {
        if (wheelColliders == null || _baseSuspensionSpring == null || _baseSuspensionDistance == null || _baseWheelDampingRate == null)
            return;

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelCollider wc = wheelColliders[i];
            if (wc == null)
                continue;
            float springScale = GetPerWheelScale(perWheelSpringScale, i, 1f);
            float damperScale = GetPerWheelScale(perWheelDamperScale, i, 1f);
            float travelScale = GetPerWheelScale(perWheelSuspensionDistanceScale, i, 1f);

            JointSpring spring = _baseSuspensionSpring[i];
            spring.spring *= suspensionSpringMultiplier * springScale;
            spring.damper *= suspensionDamperMultiplier * damperScale;
            float baseTarget = _baseSuspensionSpring[i].targetPosition;
            spring.targetPosition = Mathf.Clamp01(baseTarget + suspensionTargetPositionOffset);
            wc.suspensionSpring = spring;
            wc.suspensionDistance = _baseSuspensionDistance[i] * suspensionDistanceMultiplier * travelScale;
            wc.wheelDampingRate = _baseWheelDampingRate[i] * wheelDampingRateMultiplier;
            wc.forceAppPointDistance = forceAppPointDistance;
        }
    }

    private static float GetPerWheelScale(float[] scales, int wheelIndex, float defaultScale)
    {
        if (scales == null || wheelIndex < 0 || wheelIndex >= scales.Length)
            return defaultScale;
        float v = scales[wheelIndex];
        return v > 0.01f ? v : defaultScale;
    }

    private void EnsureAudioSources()
    {
        _engineIdleSource = CreateLoopSource("EngineIdleLoop", engineIdleClip, 0f);
        _engineLoadSource = CreateLoopSource("EngineLoadLoop", engineLoadClip, 0f);
        _skidSource = CreateLoopSource("SkidLoop", skidLoopClip, 0f);
        _brakeSource = CreateLoopSource("BrakeLoop", brakeLoopClip, 0f);
    }

    private AudioSource CreateLoopSource(string objectName, AudioClip clip, float volume)
    {
        if (clip == null)
            return null;

        GameObject sourceGo = new GameObject(objectName);
        sourceGo.transform.SetParent(transform, false);
        AudioSource source = sourceGo.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 5f;
        source.maxDistance = 80f;
        source.volume = volume;
        source.Play();
        return source;
    }

    private void BuildTireEffects()
    {
        if (wheelColliders == null || wheelColliders.Length == 0)
            return;

        tireTrails = new TrailRenderer[wheelColliders.Length];
        tireSmoke = new ParticleSystem[wheelColliders.Length];

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelCollider wc = wheelColliders[i];
            if (wc == null)
                continue;

            if (createTireMarks)
            {
                GameObject trailGo = new GameObject($"TireTrail_{i}");
                trailGo.transform.SetParent(wc.transform, false);
                trailGo.transform.localPosition = Vector3.zero;
                TrailRenderer tr = trailGo.AddComponent<TrailRenderer>();
                tr.time = 0.65f;
                float tireWidth = EstimateTireMarkWidth(wc, i);
                tr.startWidth = tireWidth;
                tr.endWidth = tireWidth * 0.92f;
                tr.minVertexDistance = 0.05f;
                tr.autodestruct = false;
                tr.emitting = false;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                if (tireMarkMaterial != null)
                    tr.material = tireMarkMaterial;
                tireTrails[i] = tr;
            }

            GameObject smokeGo = new GameObject($"TireSmoke_{i}");
            smokeGo.transform.SetParent(wc.transform, false);
            smokeGo.transform.localPosition = Vector3.zero;
            ParticleSystem ps = smokeGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.45f;
            main.startSize = 0.45f;
            main.maxParticles = 60;
            main.playOnAwake = false;
            main.loop = true;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.04f;
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.78f, 0.78f, 0.78f), 0f), new GradientColorKey(new Color(0.34f, 0.34f, 0.34f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;
            tireSmoke[i] = ps;
        }
    }

    private void FixedUpdate()
    {
        if (wheels == null || wheels.Length == 0) return;

        float horizontalInputRaw;
        bool forwardKeyHeld;
        bool reverseKeyHeld;
        bool handbrake;
        if (useDebugInputOverride)
        {
            horizontalInputRaw = Mathf.Clamp(debugSteerInput, -1f, 1f);
            float debugThrottle = Mathf.Clamp(debugThrottleInput, -1f, 1f);
            forwardKeyHeld = debugThrottle > 0.05f;
            reverseKeyHeld = debugThrottle < -0.05f;
            handbrake = debugHandbrakeInput;
        }
        else
        {
            // Keyboard: explicit A/D avoids legacy Horizontal axis bleed when releasing S (brake),
            // which caused a slight persistent yaw (often felt as pulling right).
            bool leftSteer = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool rightSteer = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            if (leftSteer ^ rightSteer)
            {
                horizontalInputRaw = leftSteer ? -1f : 1f;
            }
            else
            {
                float axis = Input.GetAxisRaw("Horizontal");
                horizontalInputRaw = Mathf.Abs(axis) >= steerAxisNoiseDeadzone ? axis : 0f;
            }

            forwardKeyHeld = Input.GetKey(KeyCode.W);
            reverseKeyHeld = Input.GetKey(KeyCode.S);
            handbrake = Input.GetKey(KeyCode.Space);
        }

        float horizontalInput = Mathf.Abs(horizontalInputRaw) <= steerInputDeadzone ? 0f : horizontalInputRaw;
        float verticalInput = 0f;
        if (forwardKeyHeld ^ reverseKeyHeld)
            verticalInput = forwardKeyHeld ? 1f : -1f;

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

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        bool throttleForward = verticalInput > 0.05f;
        bool reverseInput = verticalInput < -0.05f;
        bool brakingInput = reverseInput && forwardSpeed > 1f;
        bool reverseDriveInput = reverseInput && forwardSpeed < 0.5f;
        float desiredThrottle = throttleForward ? verticalInput : (reverseDriveInput ? verticalInput * 0.6f : 0f);
        float planarSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        bool stopOnRelease = !throttleForward && !reverseDriveInput && planarSpeed > 0.5f;

        if (rb != null)
        {
            // Instant switch: slow Lerp here made coasting feel like "gentle first, then harder" as damping ramped up.
            rb.linearDamping = stopOnRelease ? coastLinearDamping : driveLinearDamping;
        }

        bool steerReleased = Mathf.Abs(horizontalInput) <= steerInputDeadzone;
        float throttleRate = Mathf.Abs(desiredThrottle) > Mathf.Abs(_smoothedThrottle) ? throttleRise : throttleFall;
        float throttleT = Mathf.Clamp01(Time.fixedDeltaTime * throttleRate);
        _smoothedThrottle = Mathf.Lerp(_smoothedThrottle, desiredThrottle, throttleT);
        if (!throttleForward && !reverseDriveInput)
            _smoothedThrottle = 0f;

        float steerTarget01 = Mathf.Abs(horizontalInput);
        float releaseRampTime = Mathf.Max(0.05f, steerRampDownTime / Mathf.Max(steerReturnMultiplier, 0.01f));
        float rampTime = steerTarget01 > _steerRamp01 ? steerRampUpTime : releaseRampTime;
        float rampStep = rampTime > 0.001f ? Time.fixedDeltaTime / rampTime : 1f;
        _steerRamp01 = Mathf.MoveTowards(_steerRamp01, steerTarget01, rampStep);
        float rampProgress = steerTarget01 > 0.001f ? Mathf.Clamp01(_steerRamp01 / steerTarget01) : 0f;
        float steerMag01 = steerTarget01 * Mathf.Lerp(steerRampInitial, 1f, rampProgress);
        float steeringRampInput = Mathf.Abs(horizontalInput) > 0.001f ? Mathf.Sign(horizontalInput) * steerMag01 : 0f;

        float effectiveTurnSpeed = turnSpeed * steeringWeight;
        float targetTurnAngle = steeringRampInput * effectiveTurnSpeed;
        currentTurnAngle = targetTurnAngle;
        if (steerReleased && Mathf.Abs(currentTurnAngle) < 0.15f)
            currentTurnAngle = 0f;

        if (steerReleased && rb != null)
        {
            Vector3 av = rb.angularVelocity;
            av.y *= Mathf.Exp(-yawStraightenStrength * Time.fixedDeltaTime);
            rb.angularVelocity = av;
        }

        if (rb != null)
        {
            Vector3 av = rb.angularVelocity;
            av.x *= Mathf.Exp(-pitchStabilization * Time.fixedDeltaTime);
            rb.angularVelocity = av;
        }

        if (steeringWheel != null)
            steeringWheel.transform.localEulerAngles = new Vector3(-64f, 0f, currentTurnAngle * 3f);

        float steeringAbs = Mathf.Abs(horizontalInput);
        if (steeringAbs >= skidTurnInputThreshold)
            _turnHoldTimer += Time.fixedDeltaTime;
        else
            _turnHoldTimer = 0f;
        bool instantDriftArmed = allowInstantDriftAtHighSteer &&
                                 steeringAbs >= instantDriftSteerThreshold &&
                                 speedKmh >= instantDriftMinSpeedKmh;
        bool driftArmed = _turnHoldTimer >= skidTurnHoldSeconds || instantDriftArmed;

        float driftBlend = 0f;
        if (useDrift && driftArmed && rb != null && driftAmount > 0.001f)
        {
            Vector3 v = rb.linearVelocity;
            float planar = new Vector3(v.x, 0f, v.z).magnitude;
            // Prefer live input so grip returns once keys are released.
            float steerFactor = Mathf.Max(
                Mathf.Abs(horizontalInput),
                Mathf.Abs(currentTurnAngle) / Mathf.Max(effectiveTurnSpeed, 0.01f) * (steerReleased ? 0.35f : 1f));
            float driftSpeedRef = Mathf.Max(driftSpeedRefMs * driftSpeedEntryMultiplier, 0.5f);
            float speedFactor = Mathf.Clamp01(planar / driftSpeedRef);
            driftBlend = Mathf.Clamp01(driftAmount * driftEntryBoost * steerFactor * speedFactor);
        }

        bool sustainedDriftAssistCandidate = false;
        if (rb != null && useDrift && driftBlend > 0.01f && maxSpeedKmh > 0f)
        {
            float planarMs = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            float speedCapMs = maxSpeedKmh / 3.6f;
            bool enoughSpeed = planarMs >= speedCapMs * 0.45f;
            bool enoughSteer = steeringAbs >= highSpeedDriftSteerThreshold * 0.9f;
            sustainedDriftAssistCandidate = enoughSpeed && enoughSteer;
        }
        _driftAssistHoldTimer = sustainedDriftAssistCandidate
            ? driftAssistGraceSeconds
            : Mathf.Max(0f, _driftAssistHoldTimer - Time.fixedDeltaTime);
        _highSpeedDriftAssistActive = _driftAssistHoldTimer > 0f;

        UpdateTransmission(forwardSpeed, Mathf.Abs(_smoothedThrottle));

        float gearRatio = GetCurrentGearRatio();
        float motorTorqueBase = _smoothedThrottle * enginePower * gearRatio * (forwardSpeed >= 0f || _smoothedThrottle >= 0f ? 1f : 0.8f);
        if (_highSpeedDriftAssistActive)
            motorTorqueBase *= 0.55f;
        float brakeTorque = brakingInput ? brakePower * Mathf.Abs(verticalInput) : 0f;
        if (stopOnRelease && !useConstantCoastDecel)
        {
            float releaseBrakeFactor = Mathf.Clamp01(planarSpeed / 10f);
            brakeTorque = Mathf.Max(brakeTorque, releaseBrakePower * releaseBrakeFactor);
        }
        float handbrakeTorque = handbrake ? handbrakePower : 0f;
        float torqueMul = vehicleCrowdBrake != null ? vehicleCrowdBrake.MotorTorqueMultiplier : 1f;
        bool turnForSkid = Mathf.Abs(horizontalInput) >= skidTurnInputThreshold;

        bool skidSoundNow = false;
        for (int i = 0; i < wheels.Length; i++)
        {
            WheelCollider wheelCollider = wheelColliders != null && i < wheelColliders.Length
                ? wheelColliders[i]
                : wheels[i].GetComponent<WheelCollider>();
            if (wheelCollider == null) continue;

            float forwardGripMultiplier = defaultForwardStiffness;
            float sidewaysGripMultiplier = defaultSidewaysStiffness;

            bool isRearWheel = i >= 2;
            bool grounded = wheelCollider.GetGroundHit(out WheelHit hit);
            if (grounded)
                GetGripMultipliers(hit.collider, out forwardGripMultiplier, out sidewaysGripMultiplier);

            if (baseForward != null && i < baseForward.Length)
            {
                WheelFrictionCurve fwd = baseForward[i];
                fwd.stiffness = baseForward[i].stiffness * forwardGripMultiplier;
                wheelCollider.forwardFriction = fwd;
            }

            if (baseSideways != null && i < baseSideways.Length &&
                wheelColliders != null && i < wheelColliders.Length && wheelColliders[i] != null)
            {
                // Rear wheels (i>=2) slide hard; front wheels keep grip for steering control.
                float gripMult = 1f;
                if (useDrift)
                {
                    float gripMin = isRearWheel ? driftRearGripMin : driftFrontGripMin;
                    gripMult = Mathf.Lerp(1f, gripMin, driftBlend);
                }

                if (handbrake && isRearWheel)
                    gripMult *= handbrakeRearGrip;

                WheelFrictionCurve side = baseSideways[i];
                side.stiffness = baseSideways[i].stiffness * sidewaysGripMultiplier * gripMult;
                wheelCollider.sidewaysFriction = side;
            }

            if (i < 2)
                wheelCollider.steerAngle = currentTurnAngle;
            else
                wheelCollider.steerAngle = 0f;

            wheelCollider.motorTorque = motorTorqueBase * torqueMul;
            wheelCollider.brakeTorque = brakeTorque + (isRearWheel ? handbrakeTorque : 0f);

            if (grounded)
            {
                float slip = Mathf.Max(Mathf.Abs(hit.sidewaysSlip), Mathf.Abs(hit.forwardSlip));
                bool allowSkidFx = driftArmed && turnForSkid;
                bool fxSkid = allowSkidFx && slip >= slipForEffects;
                bool audioSkid = allowSkidFx && slip >= skidSoundSlipThreshold && speedKmh >= skidMinSpeedKmh;
                skidSoundNow |= audioSkid;
                SetWheelEffects(i, fxSkid, hit.point);
            }
            else
            {
                SetWheelEffects(i, false, wheelCollider.transform.position);
            }

            if (wheelMeshes != null && i < wheelMeshes.Length && wheelMeshes[i] != null)
            {
                wheelCollider.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
                wheelMeshes[i].position = wheelPosition;
                wheelMeshes[i].rotation = wheelRotation;
            }
        }

        if (rb != null)
        {
            if (steerReleased && coastLateralStabilize > 0.001f)
            {
                Vector3 fwd = transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-8f)
                {
                    fwd.Normalize();
                    Vector3 pv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    if (pv.sqrMagnitude > 0.04f)
                    {
                        Vector3 lateral = pv - fwd * Vector3.Dot(pv, fwd);
                        float k = 1f - Mathf.Exp(-coastLateralStabilize * Time.fixedDeltaTime);
                        pv -= lateral * k;
                        rb.linearVelocity = new Vector3(pv.x, rb.linearVelocity.y, pv.z);
                    }
                }
            }

            if (stopOnRelease && useConstantCoastDecel && coastDecelerationMps2 > 0.001f && !brakingInput)
            {
                Vector3 pv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                float pm = pv.magnitude;
                if (pm > 0.05f)
                {
                    Vector3 dir = pv / pm;
                    float newPm = Mathf.MoveTowards(pm, 0f, coastDecelerationMps2 * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector3(dir.x * newPm, rb.linearVelocity.y, dir.z * newPm);
                }
            }

            if (_highSpeedDriftAssistActive)
            {
                Vector3 av = rb.angularVelocity;
                float limitedYaw = Mathf.Clamp(av.y, -highSpeedDriftMaxYawRate, highSpeedDriftMaxYawRate);
                float yawT = 1f - Mathf.Exp(-highSpeedDriftYawDamping * Time.fixedDeltaTime);
                av.y = Mathf.Lerp(av.y, limitedYaw, yawT);
                rb.angularVelocity = av;

                float driftCapMs = maxSpeedKmh / 3.6f * driftTurnSpeedLimit01;
                Vector3 pv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                float pm = pv.magnitude;
                if (pm > 0.5f)
                {
                    Vector3 dir = pv / pm;
                    float reverseAmount = Mathf.Clamp01((driftReverseGuardDot - Vector3.Dot(dir, transform.forward)) / Mathf.Max(0.001f, driftReverseGuardDot + 0.25f));
                    if (reverseAmount > 0.001f)
                    {
                        float recoverT = 1f - Mathf.Exp(-driftReverseRecoverStrength * reverseAmount * Time.fixedDeltaTime);
                        Vector3 correctedDir = Vector3.Slerp(dir, transform.forward, recoverT).normalized;
                        rb.linearVelocity = new Vector3(correctedDir.x * pm, rb.linearVelocity.y, correctedDir.z * pm);
                        pv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                        pm = pv.magnitude;
                    }
                }

                if (pm > driftCapMs + 0.01f)
                {
                    float newPm = Mathf.MoveTowards(pm, driftCapMs, highSpeedDriftCapDecelMps2 * Time.fixedDeltaTime);
                    Vector3 dir = pv / Mathf.Max(pm, 1e-6f);
                    rb.linearVelocity = new Vector3(dir.x * newPm, rb.linearVelocity.y, dir.z * newPm);
                }

                if (throttleForward && driftSustainAccelMps2 > 0.001f)
                {
                    float sustainFloorMs = driftCapMs * driftSustainSpeedFloor01;
                    Vector3 pv2 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    float pm2 = pv2.magnitude;
                    if (pm2 < sustainFloorMs - 0.01f)
                    {
                        float newPm = Mathf.MoveTowards(pm2, sustainFloorMs, driftSustainAccelMps2 * Time.fixedDeltaTime);
                        Vector3 driveDir = transform.forward;
                        driveDir.y = 0f;
                        if (driveDir.sqrMagnitude > 1e-8f)
                        {
                            driveDir.Normalize();
                            rb.linearVelocity = new Vector3(driveDir.x * newPm, rb.linearVelocity.y, driveDir.z * newPm);
                        }
                    }
                }
            }
        }

        UpdateAudio(speedKmh, skidSoundNow, brakingInput || handbrake);
        UpdateBodyRoll(horizontalInput, speedKmh);
    }

    private void UpdateTransmission(float forwardSpeed, float throttleAbs)
    {
        if (Time.time < _nextShiftTime)
            goto EstimateRpm;

        if (forwardSpeed < -1f && throttleAbs > 0.1f)
        {
            _currentGear = 1;
            _nextShiftTime = Time.time + shiftDelay;
            goto EstimateRpm;
        }

        float speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(4f, maxSpeedKmh / 3.6f));
        float rpm01 = Mathf.Clamp01(Mathf.Lerp(speed01 * 0.9f, 1f, throttleAbs * 0.15f));

        if (rpm01 >= upshiftRpmThreshold && _currentGear < maxForwardGears)
        {
            _currentGear++;
            _nextShiftTime = Time.time + shiftDelay;
        }
        else if (rpm01 <= downshiftRpmThreshold && _currentGear > 1)
        {
            _currentGear--;
            _nextShiftTime = Time.time + shiftDelay;
        }

    EstimateRpm:
        _estimatedEngineRpm = Mathf.Lerp(minEngineRpm, maxEngineRpm, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(5f, maxSpeedKmh / 3.6f) + throttleAbs * 0.12f));
    }

    private float GetCurrentGearRatio()
    {
        if (gearRatios == null || gearRatios.Length == 0)
            return finalDrive;

        int idx = Mathf.Clamp(_currentGear - 1, 0, gearRatios.Length - 1);
        return Mathf.Max(0.1f, gearRatios[idx] * finalDrive);
    }

    private void SetWheelEffects(int index, bool active, Vector3 groundPoint)
    {
        if (tireTrails != null && index < tireTrails.Length && tireTrails[index] != null)
        {
            tireTrails[index].emitting = active;
            if (active)
                tireTrails[index].transform.position = groundPoint + Vector3.up * 0.02f;
        }

        if (tireSmoke != null && index < tireSmoke.Length && tireSmoke[index] != null)
        {
            var emission = tireSmoke[index].emission;
            emission.rateOverTime = active ? 24f : 0f;
            if (active && !tireSmoke[index].isPlaying)
                tireSmoke[index].Play();
            if (!active && tireSmoke[index].isPlaying)
                tireSmoke[index].Stop();
        }
    }

    private void UpdateAudio(float speedKmh, bool skidNow, bool brakingNow)
    {
        float rpm01 = Mathf.InverseLerp(minEngineRpm, maxEngineRpm, _estimatedEngineRpm);
        float speed01 = Mathf.Clamp01(speedKmh / Mathf.Max(1f, maxSpeedKmh));
        float throttle01 = Mathf.Clamp01(Mathf.Abs(_smoothedThrottle));

        if (_engineIdleSource != null)
        {
            float idleWeight = Mathf.Clamp01(1f - rpm01 * 0.9f);
            _engineIdleSource.pitch = Mathf.Lerp(0.62f, 1.05f, rpm01) * enginePitchMultiplier;
            _engineIdleSource.volume = engineIdleVolume * Mathf.Clamp01(idleWeight + 0.2f * (1f - throttle01));
        }

        if (_engineLoadSource != null)
        {
            float loadWeight = Mathf.Clamp01(Mathf.Max(throttle01, speed01 * 0.65f));
            _engineLoadSource.pitch = Mathf.Lerp(0.75f, 1.55f, Mathf.Max(rpm01, speed01)) * enginePitchMultiplier;
            _engineLoadSource.volume = engineLoadVolume * loadWeight;
        }

        if (_skidSource != null)
            _skidSource.volume = skidNow ? skidVolume : Mathf.Lerp(_skidSource.volume, 0f, Time.fixedDeltaTime * 8f);
        if (_brakeSource != null)
        {
            _brakeSource.pitch = brakePitchMultiplier;
            _brakeSource.volume = brakingNow ? brakeVolume : Mathf.Lerp(_brakeSource.volume, 0f, Time.fixedDeltaTime * 8f);
        }
    }

    private void UpdateBodyRoll(float steerInput, float speedKmh)
    {
        if (bodyVisual == null)
            return;

        float speedFactor = Mathf.Clamp01(speedKmh / Mathf.Max(60f, maxSpeedKmh * 1.1f));
        float targetRoll = -steerInput * bodyRollAngle * speedFactor;
        _currentBodyRoll = Mathf.Lerp(_currentBodyRoll, targetRoll, Time.fixedDeltaTime * bodyRollSpeed);
        bodyVisual.localRotation = _bodyInitialLocalRotation * Quaternion.Euler(0f, 0f, _currentBodyRoll);
    }

    private void GetGripMultipliers(Collider groundCollider, out float forward, out float sideways)
    {
        forward = defaultForwardStiffness;
        sideways = defaultSidewaysStiffness;

        if (groundCollider == null || gripProfiles == null || gripProfiles.Length == 0)
            return;

        int layerMask = 1 << groundCollider.gameObject.layer;
        for (int i = 0; i < gripProfiles.Length; i++)
        {
            GripProfile profile = gripProfiles[i];
            if (profile == null || profile.layers.value == 0)
                continue;

            if ((profile.layers.value & layerMask) == 0)
                continue;

            forward = profile.forwardStiffness;
            sideways = profile.sidewaysStiffness;
            return;
        }
    }

    private float EstimateTireMarkWidth(WheelCollider wheelCollider, int wheelIndex)
    {
        float baseWidth = Mathf.Clamp(wheelCollider.radius * 0.32f, 0.22f, 0.6f);

        if (wheelMeshes != null && wheelIndex < wheelMeshes.Length && wheelMeshes[wheelIndex] != null)
        {
            Renderer rend = wheelMeshes[wheelIndex].GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Vector3 size = rend.bounds.size;
                float meshWidth = Mathf.Min(size.x, size.z);
                if (meshWidth > 0.01f)
                    baseWidth = Mathf.Clamp(meshWidth * 0.9f, 0.24f, 0.65f);
            }
        }

        return baseWidth;
    }
}
