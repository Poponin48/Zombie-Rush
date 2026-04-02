using System;
using UnityEngine;

namespace Project.Player.Car
{
    /// <summary>
    /// Truck durability. At 0 HP raises <see cref="OnDestroyed"/> (lose). Zombies apply damage via <see cref="ZombieCrowdResistance"/> while in contact.
    /// </summary>
    public class VehicleHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField, Tooltip("Refill to max when the scene starts.")]
        private bool resetOnStart = true;

        private float _current;

        public float Current => _current;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _current > 0.001f;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDestroyed;

        private void Start()
        {
            if (resetOnStart)
                _current = maxHealth;
            OnHealthChanged?.Invoke(_current, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            _current = Mathf.Max(0f, _current - amount);
            OnHealthChanged?.Invoke(_current, maxHealth);

            if (!IsAlive)
                OnDestroyed?.Invoke();
        }

        public void SetFull()
        {
            _current = maxHealth;
            OnHealthChanged?.Invoke(_current, maxHealth);
        }
    }
}
