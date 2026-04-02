using UnityEngine;

namespace Project.Camera
{
    /// <summary>
    /// Smooth chase camera. Offset is applied in the target's local space,
    /// so the camera always stays behind the car. SmoothDamp for position,
    /// Slerp for rotation — no jerks, no weird angles.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Position")]
        [SerializeField, Tooltip("Offset in target's local space (X=right, Y=up, Z=forward). Negative Z = behind.")]
        private Vector3 offset = new Vector3(0f, 8f, -6f);

        [SerializeField, Tooltip("Extra forward shift so the player sees more road ahead.")]
        private float lookAheadDistance = 1.5f;

        [Header("Smoothing")]
        [SerializeField, Range(0.05f, 0.8f), Tooltip("Position smooth time (seconds). Higher = smoother, less jitter when following physics.")]
        private float smoothTime = 0.28f;

        [SerializeField, Tooltip("Max camera move speed (units/sec). 0 = no cap. Low values (e.g. 40–50) can cause visible stepping when the car accelerates.")]
        private float maxFollowSpeed;

        [SerializeField, Range(1f, 20f), Tooltip("Rotation follow speed. Higher = faster tracking.")]
        private float rotationSpeed = 6f;

        [Header("Mouse orbit (vehicle doc)")]
        [SerializeField, Tooltip("Horizontal orbit from Mouse X.")]
        private bool mouseOrbitEnabled = true;
        [SerializeField, Range(0.1f, 8f)] private float mouseOrbitSensitivity = 2f;
        [SerializeField] private bool lockCursorInPlayMode;

        private Vector3 _velocity;
        private float _orbitYaw;

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }
            if (target == null) return;
            transform.position = GetDesiredPosition();
            transform.LookAt(target.position + Vector3.up * 1.5f);

            if (lockCursorInPlayMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (mouseOrbitEnabled)
                _orbitYaw += Input.GetAxis("Mouse X") * mouseOrbitSensitivity;

            Vector3 desired = GetDesiredPosition();
            float maxSpeed = maxFollowSpeed <= 0f ? Mathf.Infinity : maxFollowSpeed;
            // smoothDeltaTime reduces micro-jitter when frame time varies (chase + physics).
            float dt = Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _velocity,
                smoothTime,
                maxSpeed,
                dt);

            Vector3 lookPoint = target.position + Vector3.up * 1.5f;
            Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position);
            float rotT = 1f - Mathf.Exp(-rotationSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotT);
        }

        private Vector3 GetDesiredPosition()
        {
            // Local offset, then extra yaw around world Y (mouse), then follow car heading
            Vector3 rotatedLocal = Quaternion.Euler(0f, _orbitYaw, 0f) * offset;
            return target.TransformPoint(rotatedLocal) + target.forward * lookAheadDistance;
        }
    }
}
