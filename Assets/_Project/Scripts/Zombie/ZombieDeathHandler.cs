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

        /// <param name="planarKnockVelocity">Horizontal world velocity (m/s) for knock direction; zero = instant hide.</param>
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

            var sync = GetComponent<ZombieNavMeshRigidbodySync>();
            if (sync != null)
                sync.enabled = false;

            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (animator != null)
                animator.enabled = false;

            planarKnockVelocity.y = 0f;
            float knockDuration = statsForKnock != null ? statsForKnock.runOverKnockDurationSeconds : 0.5f;

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                return;

            // Always enable short ragdoll tumble so corpses never stay standing and blocking the lane.
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            Vector3 planar = planarKnockVelocity;
            planar.y = 0f;
            if (planar.sqrMagnitude < 0.01f)
                planar = transform.forward * 2.5f;

            float knockSpeed = Mathf.Max(2.5f, planar.magnitude);
            float upSpeed = Mathf.Clamp(knockSpeed * 0.35f, 1.2f, 4.5f);
            rb.linearVelocity = planar.normalized * knockSpeed + Vector3.up * upSpeed;
            rb.angularVelocity = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-6f, 6f),
                Random.Range(-10f, 10f));

            float settleSeconds = Mathf.Max(1.3f, knockDuration * 4f);
            StartCoroutine(SettleCorpse(settleSeconds));
        }

        /// <summary>
        /// After the body settles on the floor, freeze it kinematically so it doesn't slide forever.
        /// </summary>
        private IEnumerator SettleCorpse(float settleDelay)
        {
            yield return new WaitForSeconds(settleDelay);

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Force a side-lying corpse pose.
            Vector3 e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(90f, e.y, Random.Range(-14f, 14f));

            // Keep visual corpse, but stop blocking the truck physically.
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }
    }
}
