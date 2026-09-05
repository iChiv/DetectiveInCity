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
        [SerializeField] private float followSmoothTime = 0.2f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float minimumFieldOfView = 32f;
        [SerializeField] private float maximumFieldOfView = 55f;

        private Camera targetCamera;
        private Vector3 followVelocity;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
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

            Vector3 desiredPosition = GetDesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);

            ApplyZoom();
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

        private void ApplyZoom()
        {
            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            targetCamera.fieldOfView = Mathf.Clamp(
                targetCamera.fieldOfView - scroll * zoomSpeed,
                minimumFieldOfView,
                maximumFieldOfView);
        }
    }
}