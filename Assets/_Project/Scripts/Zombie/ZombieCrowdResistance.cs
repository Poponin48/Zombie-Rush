using Project.Player.Car;
using UnityEngine;

namespace Project.Zombie
{
    /// <summary>
    /// Run-over + crowd slowdown. Use a <b>non-trigger</b> capsule + <see cref="ZombieNavMeshRigidbodySync"/> (light Rigidbody) so the truck shoves zombies instead of clipping or being pushed by kinematic colliders.
    /// Avoid shove: <see cref="ZombieAI"/> stops pathing before overlapping the hull (ZombieStats stop + clearance).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZombieCrowdResistance : MonoBehaviour
    {
        [SerializeField] private ZombieStats stats;
        [SerializeField] private ZombieDeathHandler deathHandler;
        [SerializeField] private string playerTag = "Player";

        private VehicleCrowdBrake _vehicleBrake;
        private VehicleHealth _vehicleHealth;
        private bool _registered;
        private float _nextVehicleDamageTime;

        public float SlowdownContribution => stats != null ? stats.slowdownToVehicle : 0.1f;
        public bool IsDead => deathHandler != null && deathHandler.IsDead;
        public ZombieStats StatsAsset => stats;

        private void Awake()
        {
            if (deathHandler == null)
                deathHandler = GetComponent<ZombieDeathHandler>();

            var col = GetComponent<Collider>();
            if (col != null && col.isTrigger)
            {
                Debug.LogWarning(
                    $"{nameof(ZombieCrowdResistance)} on {name}: **Is Trigger** is ON — the truck will not physically hit this zombie. " +
                    "Use a solid capsule + {nameof(ZombieNavMeshRigidbodySync)} for pushable zombies.",
                    this);
            }

            CacheVehicleReferences();
        }

        private void FixedUpdate()
        {
            if (IsDead || !_registered)
                return;

            if (_vehicleHealth == null || !_vehicleHealth.IsAlive)
                return;

            float interval = stats != null ? stats.vehicleDamageHitIntervalSeconds : 2f;
            if (Time.fixedTime < _nextVehicleDamageTime)
                return;

            float dmg = stats != null ? stats.damageToVehiclePerHit : 5f;
            if (dmg > 0f)
                _vehicleHealth.TakeDamage(dmg);

            _nextVehicleDamageTime = Time.fixedTime + interval;
        }

        private void OnDisable()
        {
            UnregisterSafe();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsVehicleCollider(other))
                return;

            var rb = other.attachedRigidbody;
            if (TryRunOverFromTruck(rb, null))
                return;

            TryRegister();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsVehicleCollider(other))
                return;

            var rb = other.attachedRigidbody;
            if (TryRunOverFromTruck(rb, null))
                return;

            if (!_registered)
                TryRegister();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsVehicleCollider(other))
                return;

            UnregisterSafe();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsVehicleCollision(collision))
                return;

            if (TryRunOverFromCollision(collision))
                return;

            TryRegister();
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!IsVehicleCollision(collision))
                return;

            if (TryRunOverFromCollision(collision))
                return;

            if (!_registered)
                TryRegister();
        }

        /// <summary>
        /// Fast enough truck kills zombie (placeholder death). Slower contact only slows the truck.
        /// </summary>
        private bool TryRunOverFromCollision(Collision collision)
        {
            return TryRunOverFromTruck(collision.rigidbody, collision);
        }

        private bool TryRunOverFromTruck(Rigidbody truckRb, Collision collision)
        {
            if (IsDead || deathHandler == null || truckRb == null)
                return false;

            if (!truckRb.gameObject.CompareTag(playerTag))
                return false;

            EnsureVehicleBrakeReference();

            float truckKmh = PlanarSpeedKmh(truckRb.linearVelocity);
            float baseRequired = stats != null ? stats.runOverSpeedKmh : 50f;

            float powerT = 0.5f;
            if (_vehicleBrake != null && _vehicleBrake.CarStats != null)
                powerT = Mathf.Clamp01(_vehicleBrake.CarStats.power);

            // Higher truck Power → lower speed needed to run over.
            float requiredKmh = baseRequired * Mathf.Lerp(1.12f, 0.68f, powerT);

            if (truckKmh < requiredKmh)
                return false;

            float lossKmh = stats != null ? stats.truckSpeedLossKmhPerRunOver : 5f;
            ApplyTruckPlanarSpeedLoss(truckRb, lossKmh);

            Vector3 knock = ComputeRunOverKnock(truckRb, collision);
            deathHandler.Die(knock);
            return true;
        }

        private static void ApplyTruckPlanarSpeedLoss(Rigidbody truckRb, float lossKmh)
        {
            if (truckRb == null || lossKmh <= 0f)
                return;

            Vector3 v = truckRb.linearVelocity;
            Vector3 planar = new Vector3(v.x, 0f, v.z);
            float mag = planar.magnitude;
            if (mag < 0.001f)
                return;

            float lossMs = lossKmh / 3.6f;
            float newMag = Mathf.Max(0f, mag - lossMs);
            planar *= newMag / mag;
            truckRb.linearVelocity = new Vector3(planar.x, v.y, planar.z);
        }

        private Vector3 ComputeRunOverKnock(Rigidbody truckRb, Collision collision)
        {
            Vector3 fromTruck = transform.position - truckRb.position;
            fromTruck.y = 0f;
            if (fromTruck.sqrMagnitude < 0.01f)
                fromTruck = truckRb.transform.forward;

            Vector3 dir = fromTruck.normalized;

            if (collision != null && collision.contactCount > 0)
            {
                Vector3 n = collision.GetContact(0).normal;
                n.y = 0f;
                if (n.sqrMagnitude > 0.01f)
                    dir = Vector3.Slerp(dir, (-n).normalized, 0.35f).normalized;
            }

            Vector3 truckPlanar = truckRb.linearVelocity;
            truckPlanar.y = 0f;
            if (truckPlanar.sqrMagnitude > 4f)
                dir = Vector3.Slerp(dir, truckPlanar.normalized, 0.4f).normalized;

            float knockSpeed = stats != null ? stats.runOverKnockPlanarSpeed : 7f;
            return dir * knockSpeed;
        }

        private static float PlanarSpeedKmh(Vector3 velocity)
        {
            velocity.y = 0f;
            return velocity.magnitude * 3.6f;
        }

        private void CacheVehicleReferences()
        {
            var playerGo = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGo == null)
                return;

            if (_vehicleBrake == null)
                _vehicleBrake = playerGo.GetComponent<VehicleCrowdBrake>();
            if (_vehicleHealth == null)
                _vehicleHealth = playerGo.GetComponent<VehicleHealth>();
        }

        private void EnsureVehicleBrakeReference()
        {
            if (_vehicleBrake != null)
                return;

            CacheVehicleReferences();
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!IsVehicleCollision(collision))
                return;

            UnregisterSafe();
        }

        private bool IsVehicleCollision(Collision collision)
        {
            if (collision.rigidbody == null)
                return false;

            return collision.rigidbody.gameObject.CompareTag(playerTag);
        }

        private bool IsVehicleCollider(Collider other)
        {
            if (other == null || IsDead)
                return false;

            var rb = other.attachedRigidbody;
            if (rb == null)
                return false;

            // Wheels/body may be on children; Rigidbody is on the truck root.
            return rb.gameObject.CompareTag(playerTag);
        }

        private void TryRegister()
        {
            if (IsDead || _registered)
                return;

            if (_vehicleBrake == null)
            {
                var playerGo = GameObject.FindGameObjectWithTag(playerTag);
                if (playerGo != null)
                    _vehicleBrake = playerGo.GetComponent<VehicleCrowdBrake>();
            }

            if (_vehicleBrake == null)
                return;

            _vehicleBrake.Register(this);
            _registered = true;

            if (_vehicleHealth == null)
                CacheVehicleReferences();

            float interval = stats != null ? stats.vehicleDamageHitIntervalSeconds : 2f;
            _nextVehicleDamageTime = Time.fixedTime + interval;
        }

        private void UnregisterSafe()
        {
            if (!_registered || _vehicleBrake == null)
            {
                _registered = false;
                return;
            }

            _vehicleBrake.Unregister(this);
            _registered = false;
            _nextVehicleDamageTime = 0f;
        }
    }
}
