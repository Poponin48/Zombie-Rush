using UnityEngine;
using UnityEngine.AI;

namespace Project.Zombie
{
    /// <summary>
    /// Idle until the player enters activation radius, then chases using NavMeshAgent (obstacle avoidance built-in).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
        [SerializeField] private ZombieStats stats;
        [SerializeField] private ZombieDeathHandler deathHandler;
        [SerializeField] private Animator animator;
        [SerializeField] private string playerTag = "Player";

        [Header("Optional")]
        [Tooltip("If set, used instead of searching by Player tag (fixes missing tag on truck).")]
        [SerializeField] private Transform playerOverride;

        [Tooltip("How far to search for a NavMesh under the zombie if not placed exactly on the bake.")]
        [SerializeField] private float navMeshSnapDistance = 4f;
        [SerializeField, Tooltip("How often to retry snapping when pushed off NavMesh.")]
        private float navMeshResnapIntervalSeconds = 1f;

        private NavMeshAgent _agent;
        private Transform _player;
        private bool _animatorHasSpeedParam;
        private float _nextResnapAt;
        private static readonly int SpeedParamHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (deathHandler == null)
                deathHandler = GetComponent<ZombieDeathHandler>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void Start()
        {
            CachePlayer();
            SnapAgentOntoNavMesh();
            ApplyAgentFromStats();
            CacheAnimatorSpeedParam();

            if (_agent != null && !_agent.isOnNavMesh)
            {
                Debug.LogWarning(
                    $"{name}: NavMeshAgent is still not on a NavMesh after snap. Bake a NavMesh (NavMesh Surface) under the zombie and check Area Mask.",
                    this);
            }

            if (_player == null)
            {
                Debug.LogWarning(
                    $"{name}: No player target. Set tag **Player** on the truck root (Rigidbody) or assign **Player Override** on Zombie AI.",
                    this);
            }
        }

        /// <summary>
        /// Agents slightly above/below the baked surface do not move until warped onto the mesh.
        /// </summary>
        private void SnapAgentOntoNavMesh()
        {
            if (_agent == null || _agent.isOnNavMesh)
                return;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSnapDistance, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
        }

        private void CacheAnimatorSpeedParam()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == SpeedParamHash && p.type == AnimatorControllerParameterType.Float)
                {
                    _animatorHasSpeedParam = true;
                    return;
                }
            }
        }

        private void CachePlayer()
        {
            if (playerOverride != null)
            {
                _player = playerOverride;
                return;
            }

            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
            {
                _player = p.transform;
                return;
            }

            var car = Object.FindFirstObjectByType<CarControl>(FindObjectsInactive.Exclude);
            if (car != null)
                _player = car.transform;
        }

        private void ApplyAgentFromStats()
        {
            if (_agent == null)
                return;

            _agent.angularSpeed = 520f;
            _agent.acceleration = 14f;
            _agent.stoppingDistance = 0.35f;
            _agent.autoBraking = true;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            if (stats != null)
            {
                _agent.speed = stats.moveSpeed;
                _agent.radius = Mathf.Clamp(stats.mass / 180f, 0.25f, 0.55f);
                _agent.height = 1.85f;
                _agent.stoppingDistance = Mathf.Max(0.2f, EffectiveStopRadius(stats) * 0.92f);
            }
            else
            {
                _agent.speed = 5.5f;
                _agent.radius = 0.4f;
                _agent.height = 1.85f;
            }

            _agent.avoidancePriority = Random.Range(35, 90);
        }

        private void Update()
        {
            if (deathHandler != null && deathHandler.IsDead)
                return;

            if (_player == null)
            {
                CachePlayer();
                if (_player == null)
                    return;
            }

            if (_agent == null || !_agent.isOnNavMesh)
            {
                if (_agent != null && Time.time >= _nextResnapAt)
                {
                    _nextResnapAt = Time.time + Mathf.Max(0.2f, navMeshResnapIntervalSeconds);
                    SnapAgentOntoNavMesh();
                }
                return;
            }

            Vector3 toPlayer = _player.position - transform.position;
            float sqr = toPlayer.sqrMagnitude;
            float r = stats != null ? stats.activationRadius : 50f;

            if (sqr > r * r)
            {
                if (!_agent.isStopped)
                    _agent.isStopped = true;
                SetAnimatorSpeed(0f);
                return;
            }

            Vector3 toPlayerFlat = toPlayer;
            toPlayerFlat.y = 0f;
            float stopDist = stats != null ? EffectiveStopRadius(stats) : 4f;
            if (toPlayerFlat.sqrMagnitude <= stopDist * stopDist)
            {
                if (!_agent.isStopped)
                    _agent.isStopped = true;
                SetAnimatorSpeed(0f);
                return;
            }

            if (_agent.isStopped)
                _agent.isStopped = false;

            _agent.SetDestination(_player.position);
            SetAnimatorSpeed(_agent.velocity.magnitude);
        }

        private void SetAnimatorSpeed(float planarSpeed)
        {
            if (animator == null || !_animatorHasSpeedParam)
                return;

            animator.SetFloat(SpeedParamHash, planarSpeed);
        }

        /// <summary>
        /// Horizontal distance at which we stop pathing toward the player pivot. Accounts for truck length from pivot and agent radius.
        /// </summary>
        private float EffectiveStopRadius(ZombieStats s)
        {
            if (_agent == null)
                return s.stopChasingPlayerDistance + s.extraClearanceFromTruckPivot;

            return s.stopChasingPlayerDistance + s.extraClearanceFromTruckPivot + _agent.radius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float r = stats != null ? stats.activationRadius : 50f;
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, r);
            if (stats != null)
            {
                Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.45f);
                float eff = stats.stopChasingPlayerDistance + stats.extraClearanceFromTruckPivot;
                if (_agent != null)
                    eff += _agent.radius;
                Gizmos.DrawWireSphere(transform.position, eff);
            }
        }
#endif
    }
}
