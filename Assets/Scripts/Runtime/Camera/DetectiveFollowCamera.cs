using UnityEngine;
using UnityEngine.InputSystem;

namespace Detective
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public sealed class DetectiveFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 12f, -10f);
        [SerializeField, Range(-180f, 180f)] private float cameraYawDegrees = 35f;
        [SerializeField] private float defaultYawDegrees = 35f;
        [SerializeField] private float followSmoothTime = 0.2f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float minimumFieldOfView = 32f;
        [SerializeField] private float maximumFieldOfView = 55f;
        [SerializeField] private float yawRotateDegreesPerKey = 45f;
        [SerializeField] private float yawSmoothTime = 0.25f;

        private Camera targetCamera;
        private Vector3 followVelocity;
        private float targetYawDegrees;
        private float yawVelocity;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            targetYawDegrees = cameraYawDegrees;
        }

        private void Start()
        {
            ResolveTarget();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            ResolveTarget();
            if (target == null)
            {
                return;
            }

            ApplyRotationInput();
            ApplyZoom();

            Vector3 desiredPosition = GetDesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }

        private Vector3 GetDesiredPosition()
        {
            Vector3 rotatedOffset = Quaternion.Euler(0f, cameraYawDegrees, 0f) * followOffset;
            return target.position + rotatedOffset;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = GetDesiredPosition();
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            GameObject player = GameObject.Find("DetectivePlayer");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // Q / E 每次旋转 45 度，W 回到默认方向；+/- 调整视野。
        // 标题、暂停、对话、对决期间不接收镜头操作。
        private void ApplyRotationInput()
        {
            if (Keyboard.current != null && !InteractionController.DialogueBlock)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    targetYawDegrees -= yawRotateDegreesPerKey;
                }
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    targetYawDegrees += yawRotateDegreesPerKey;
                }
                if (Keyboard.current.wKey.wasPressedThisFrame)
                {
                    targetYawDegrees = defaultYawDegrees;
                }
            }

            cameraYawDegrees = Mathf.SmoothDampAngle(cameraYawDegrees, targetYawDegrees, ref yawVelocity, yawSmoothTime);
        }

        private void ApplyZoom()
        {
            if (InteractionController.DialogueBlock) return;
            float delta = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.minusKey.wasPressedThisFrame)
                {
                    delta += zoomSpeed;
                }

                if (Keyboard.current.equalsKey.wasPressedThisFrame)
                {
                    delta -= zoomSpeed;
                }
            }

            if (Mathf.Abs(delta) < 0.01f)
            {
                return;
            }

            targetCamera.fieldOfView = Mathf.Clamp(
                targetCamera.fieldOfView + delta,
                minimumFieldOfView,
                maximumFieldOfView);
        }
    }
}
