using System.Collections;
using UnityEngine;

namespace Project.Zombie
{
    /// <summary>
    /// Death flow: disables AI/movement and hides the body. Ragdoll can replace this later.
    /// </summary>
    public class ZombieDeathHandler : MonoBehaviour
    {
        [SerializeField] private ZombieAI zombieAi;
        [SerializeField] private ZombieCrowdResistance crowdResistance;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject visualRoot;

        public bool IsDead { get; private set; }

        private void Awake()
        {
            if (zombieAi == null)
                zombieAi = GetComponent<ZombieAI>();
            if (crowdResistance == null)
                crowdResistance = GetComponent<ZombieCrowdResistance>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (visualRoot == null)
                visualRoot = gameObject;
        }

        /// <summary>
        /// Placeholder death: no ragdoll — disable behaviour and mesh.
        /// </summary>
        public void Die() => Die(Vector3.zero);

        /// <param name="planarKnockVelocity">Horizontal world velocity (m/s) for a short slide; zero = instant hide.</param>
        public void Die(Vector3 planarKnockVelocity)
        {
            if (IsDead)
                return;

            IsDead = true;

            ZombieStats statsForKnock = crowdResistance != null ? crowdResistance.StatsAsset : null;

            if (crowdResistance != null)
                crowdResistance.enabled = false;

            if (zombieAi != null)
                zombieAi.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var sync = GetComponent<ZombieNavMeshRigidbodySync>();
            if (sync != null)
                sync.enabled = false;

            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            if (animator != null)
                animator.enabled = false;

            planarKnockVelocity.y = 0f;
            float knockDuration = statsForKnock != null ? statsForKnock.runOverKnockDurationSeconds : 0.4f;
            if (planarKnockVelocity.sqrMagnitude > 0.01f && knockDuration > 0.01f)
                StartCoroutine(KnockbackThenHideRoutine(planarKnockVelocity, knockDuration));
            else if (visualRoot != null)
                visualRoot.SetActive(false);
        }

        private IEnumerator KnockbackThenHideRoutine(Vector3 planarVelocity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                transform.position += planarVelocity * dt;
                planarVelocity *= Mathf.Exp(-5f * dt);
                elapsed += dt;
                yield return null;
            }

            if (visualRoot != null)
                visualRoot.SetActive(false);
        }
    }
}
