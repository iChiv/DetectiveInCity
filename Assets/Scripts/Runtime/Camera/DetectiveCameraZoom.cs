using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Detective
{
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class DetectiveCameraZoom : MonoBehaviour
    {
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minimumFieldOfView = 30f;
        [SerializeField] private float maximumFieldOfView = 55f;

        private CinemachineCamera virtualCamera;

        private void Awake()
        {
            virtualCamera = GetComponent<CinemachineCamera>();
        }

        private void Update()
        {
            if (InteractionController.DialogueBlock) return;
            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            LensSettings lens = virtualCamera.Lens;
            lens.FieldOfView = Mathf.Clamp(lens.FieldOfView - scroll * zoomSpeed, minimumFieldOfView, maximumFieldOfView);
            virtualCamera.Lens = lens;
        }
    }
}
