using TMPro;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Shows planar speed (km/h) on optional HUD text and/or throttled Debug.Log.
    /// </summary>
    public class VehicleSpeedDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Truck Rigidbody. If empty, first object tagged Player with Rigidbody is used.")]
        private Rigidbody vehicleBody;

        [SerializeField, Tooltip("Optional HUD line, e.g. \"72 km/h\".")]
        private TextMeshProUGUI speedTextTMP;

        [Header("Debug console")]
        [SerializeField] private bool logToConsole;
        [SerializeField, Min(0.05f)] private float consoleLogIntervalSeconds = 0.5f;

        [SerializeField] private string playerTag = "Player";

        private float _nextConsoleLogTime;

        private void Awake()
        {
            if (vehicleBody == null)
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go != null)
                    vehicleBody = go.GetComponent<Rigidbody>();
            }

            if (vehicleBody == null)
                Debug.LogWarning($"{nameof(VehicleSpeedDisplay)} on {name}: assign Vehicle Body or tag {playerTag} + Rigidbody.", this);
        }

        private void Update()
        {
            if (vehicleBody == null)
                return;

            float kmh = PlanarSpeedKmh(vehicleBody.linearVelocity);

            if (speedTextTMP != null)
                speedTextTMP.text = $"{kmh:F0} km/h";

            if (!logToConsole)
                return;

            if (Time.unscaledTime < _nextConsoleLogTime)
                return;

            _nextConsoleLogTime = Time.unscaledTime + consoleLogIntervalSeconds;
            Debug.Log($"[VehicleSpeed] {kmh:F1} km/h", this);
        }

        private static float PlanarSpeedKmh(Vector3 velocity)
        {
            velocity.y = 0f;
            return velocity.magnitude * 3.6f;
        }
    }
}
