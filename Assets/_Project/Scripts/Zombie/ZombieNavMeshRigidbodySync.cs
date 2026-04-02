using UnityEngine;
using UnityEngine.AI;

namespace Project.Zombie
{
    /// <summary>
    /// Drives a <b>dynamic</b> Rigidbody from <see cref="NavMeshAgent"/> desired motion so zombies are light obstacles
    /// the truck can shove. Agent does not move the transform directly — physics does.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class ZombieNavMeshRigidbodySync : MonoBehaviour
    {
        [SerializeField] private ZombieStats stats;
        [SerializeField] private ZombieDeathHandler deathHandler;

        [SerializeField, Tooltip("Resync agent to Rigidbody when shoved farther than this (m).")]
        private float warpIfDriftMeters = 0.12f;

        private NavMeshAgent _agent;
        private Rigidbody _rb;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();

            if (deathHandler == null)
                deathHandler = GetComponent<ZombieDeathHandler>();

            _agent.updatePosition = false;
            _agent.updateRotation = false;

            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            ApplyRigidbodyFromStats();
        }

        private void Start()
        {
            if (_agent != null && _agent.isOnNavMesh && _rb != null && !_rb.isKinematic)
                _agent.Warp(_rb.position);
        }

        private void ApplyRigidbodyFromStats()
        {
            float m = stats != null ? stats.mass : 70f;
            _rb.mass = Mathf.Clamp(m, 8f, 150f);
            _rb.linearDamping = stats != null ? stats.bodyLinearDamping : 4f;
            _rb.angularDamping = 2f;

            if (stats != null && stats.freezePitchRoll)
                _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            else
                _rb.constraints = RigidbodyConstraints.None;
        }

        private void FixedUpdate()
        {
            if (deathHandler != null && deathHandler.IsDead)
                return;

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _rb.isKinematic)
                return;

            Vector3 rbPos = _rb.position;
            float driftSq = (_agent.nextPosition - rbPos).sqrMagnitude;
            float thresh = warpIfDriftMeters * warpIfDriftMeters;
            if (driftSq > thresh)
                _agent.Warp(rbPos);

            Vector3 offset = _agent.desiredVelocity * Time.fixedDeltaTime;
            _agent.Move(offset);

            Vector3 target = _agent.nextPosition;
            _rb.MovePosition(new Vector3(target.x, target.y, target.z));

            Vector3 v = _agent.desiredVelocity;
            v.y = 0f;
            if (v.sqrMagnitude > 0.06f)
            {
                Quaternion look = Quaternion.LookRotation(v.normalized, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, look, 14f * Time.fixedDeltaTime));
            }
        }
    }
}
