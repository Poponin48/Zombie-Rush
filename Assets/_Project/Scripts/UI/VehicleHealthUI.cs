using Project.Player.Car;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class VehicleHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VehicleHealth vehicleHealth;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthTextTMP;
        [SerializeField] private string healthLabel = "HP";

        private void Awake()
        {
            if (vehicleHealth == null)
                Debug.LogWarning($"{nameof(VehicleHealthUI)} on {name} has no VehicleHealth assigned.");
        }

        private void OnEnable()
        {
            if (vehicleHealth == null)
                return;

            vehicleHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(vehicleHealth.Current, vehicleHealth.MaxHealth);
        }

        private void OnDisable()
        {
            if (vehicleHealth != null)
                vehicleHealth.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = Mathf.Max(1f, max);
                healthSlider.value = Mathf.Clamp(current, 0f, max);
            }

            if (healthTextTMP != null)
            {
                int currentInt = Mathf.CeilToInt(current);
                int maxInt = Mathf.CeilToInt(max);
                healthTextTMP.text = $"{healthLabel}:{currentInt}/{maxInt}";
            }
        }
    }
}
