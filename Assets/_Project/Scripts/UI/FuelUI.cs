using Project.Player.Car;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project.UI
{
    public class FuelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FuelSystem fuelSystem;
        [SerializeField] private Slider fuelSlider;
        [SerializeField, Tooltip("Legacy UI Text (GameObject → Add Component → UI → Text).")]
        private Text fuelText;
        [SerializeField, Tooltip("TextMeshProUGUI (GameObject → Add Component → TextMeshPro → Text - UI).")]
        private TextMeshProUGUI fuelTextTMP;
        [SerializeField, Tooltip("Compact label prefix.")]
        private string fuelLabel = "F";

        private void Awake()
        {
            if (fuelSystem == null)
            {
                Debug.LogWarning($"{nameof(FuelUI)} on {name} has no FuelSystem assigned.");
            }

            if (fuelSlider == null)
            {
                Debug.LogWarning($"{nameof(FuelUI)} on {name} has no Slider assigned.");
            }

            if (fuelText == null && fuelTextTMP == null)
            {
                Debug.LogWarning($"{nameof(FuelUI)} on {name}: assign either fuelText or fuelTextTMP.");
            }
        }

        private void OnEnable()
        {
            if (fuelSystem != null)
            {
                fuelSystem.OnFuelChanged += HandleFuelChanged;
                HandleFuelChanged(fuelSystem.CurrentFuel, fuelSystem.MaxFuel);
            }
        }

        private void OnDisable()
        {
            if (fuelSystem != null)
            {
                fuelSystem.OnFuelChanged -= HandleFuelChanged;
            }
        }

        private void HandleFuelChanged(float current, float max)
        {
            if (fuelSlider != null)
            {
                fuelSlider.minValue = 0f;
                fuelSlider.maxValue = max;
                fuelSlider.value = current;
            }

            int currentInt = Mathf.CeilToInt(current);
            int maxInt = Mathf.CeilToInt(max);
            string text = $"{fuelLabel}:{currentInt}/{maxInt}";
            if (fuelText != null)
                fuelText.text = text;
            if (fuelTextTMP != null)
                fuelTextTMP.text = text;
        }
    }
}

