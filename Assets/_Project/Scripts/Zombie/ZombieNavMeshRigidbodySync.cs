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

        [SerializeField, Tooltip("Resync agent to Rigidbody when shoved farther than this (m). Higher = fewer resyncs = less jitter.")]
        private float warpIfDriftMeters = 0.5f;

        [SerializeField, Tooltip("If agent desiredVelocity is below this (m/s), skip MovePosition and let physics settle (prevents crowd jitter).")]
        private float idleVelocityThreshold = 0.25f;

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

            // When the agent is stopped or moving very slowly (e.g., zombie surrounding the truck),
            // skip MovePosition and let physics settle. This eliminates crowd-jitter.
            bool agentIdle = _agent.isStopped || _agent.desiredVelocity.sqrMagnitude < idleVelocityThreshold * idleVelocityThreshold;
            if (agentIdle)
            {
                // Damp horizontal velocity so zombies don't drift while standing still.
                Vector3 v = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(v.x * 0.75f, v.y, v.z * 0.75f);
                return;
            }

            Vector3 offset = _agent.desiredVelocity * Time.fixedDeltaTime;
            _agent.Move(offset);

            Vector3 target = _agent.nextPosition;
            _rb.MovePosition(new Vector3(target.x, target.y, target.z));

            Vector3 dir = _agent.desiredVelocity;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.06f)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, look, 14f * Time.fixedDeltaTime));
            }
        }
    }
}
