using UnityEngine;

namespace Project.Camera
{
    /// <summary>
    /// Rigid chase camera with optional tiny smoothing and limited horizontal orbit.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Position")]
        [SerializeField, Tooltip("Offset in target's local space (X=right, Y=up, Z=forward). Negative Z = behind.")]
        private Vector3 offset = new Vector3(0f, 8f, -6f);

        [SerializeField, Tooltip("Extra forward shift so the player sees more road ahead.")]
        private float lookAheadDistance = 1.25f;

        [Header("Follow")]
        [SerializeField, Tooltip("Hard follow in LateUpdate. Disable only if you need slight interpolation.")]
        private bool hardFollow = true;
        [SerializeField, Range(0f, 0.25f), Tooltip("Used only when Hard Follow is off.")]
        private float smoothTime = 0.08f;

        [SerializeField, Tooltip("Max camera move speed (units/sec). 0 = no cap. Low values (e.g. 40–50) can cause visible stepping when the car accelerates.")]
        private float maxFollowSpeed;

        [SerializeField, Range(1f, 20f), Tooltip("Rotation follow speed. Higher = faster tracking.")]
        private float rotationSpeed = 6f;

        [Header("Mouse orbit (vehicle doc)")]
        [SerializeField, Tooltip("Horizontal orbit from Mouse X.")]
        private bool mouseOrbitEnabled = true;
        [SerializeField, Range(0.1f, 8f)] private float mouseOrbitSensitivity = 2f;
        [SerializeField, Range(0f, 15f), Tooltip("Maximum camera yaw deviation around the car.")]
        private float maxOrbitYaw = 15f;
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
            {
                _orbitYaw += Input.GetAxis("Mouse X") * mouseOrbitSensitivity;
                _orbitYaw = Mathf.Clamp(_orbitYaw, -maxOrbitYaw, maxOrbitYaw);
            }

            Vector3 desired = GetDesiredPosition();
            float dt = Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime;
            if (hardFollow)
            {
                transform.position = desired;
                _velocity = Vector3.zero;
            }
            else
            {
                float maxSpeed = maxFollowSpeed <= 0f ? Mathf.Infinity : maxFollowSpeed;
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desired,
                    ref _velocity,
                    smoothTime,
                    maxSpeed,
                    dt);
            }

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
