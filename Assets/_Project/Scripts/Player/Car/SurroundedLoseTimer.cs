using System.Collections.Generic;
using Project.Zombie;
using UnityEngine;
using UnityEngine.Events;

namespace Project.Player.Car
{
    /// <summary>
    /// If the truck is almost stopped and enough zombies are nearby on foot, invokes lose after a delay.
    /// Uses only ground zombies (no attach mechanic required).
    /// </summary>
    public class SurroundedLoseTimer : MonoBehaviour
    {
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private LayerMask zombieLayers;
        [SerializeField] private float checkRadius = 7f;
        [SerializeField] private float maxSpeedKmhToCount = 5f;
        [SerializeField] private int minZombiesNearby = 4;
        [SerializeField] private float loseAfterSeconds = 3f;

        public UnityEvent OnSurroundedLose;

        private float _timer;
        private bool _fired;

        private void Awake()
        {
            if (zombieLayers.value == 0)
            {
                Debug.LogWarning(
                    $"{nameof(SurroundedLoseTimer)} on {name}: assign **Zombie Layers** mask so nearby zombies are detected.",
                    this);
            }
        }

        private void Reset()
        {
            vehicleBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_fired || vehicleBody == null)
                return;

            Vector3 planar = new Vector3(vehicleBody.linearVelocity.x, 0f, vehicleBody.linearVelocity.z);
            float kmh = planar.magnitude * 3.6f;

            if (kmh > maxSpeedKmhToCount)
            {
                _timer = 0f;
                return;
            }

            int count = CountZombiesNearby();
            if (count < minZombiesNearby)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.fixedDeltaTime;
            if (_timer >= loseAfterSeconds)
            {
                _fired = true;
                OnSurroundedLose?.Invoke();
            }
        }

        private int CountZombiesNearby()
        {
            var pos = vehicleBody.transform.position;
            var hits = Physics.OverlapSphere(pos, checkRadius, zombieLayers, QueryTriggerInteraction.Collide);
            var seen = new HashSet<ZombieAI>();
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null)
                    continue;

                var ai = col.GetComponentInParent<ZombieAI>();
                if (ai == null)
                    continue;

                var dh = ai.GetComponent<ZombieDeathHandler>();
                if (dh != null && dh.IsDead)
                    continue;

                seen.Add(ai);
            }

            return seen.Count;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (vehicleBody == null)
                return;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(vehicleBody.transform.position, checkRadius);
        }
#endif
    }
}
