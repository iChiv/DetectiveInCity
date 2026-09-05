using UnityEngine;
using UnityEngine.InputSystem;

namespace Detective
{
    [RequireComponent(typeof(DetectiveClickMover))]
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private float maxRayDistance = 250f;
        [SerializeField] private float interactionDistance = 1.5f;
        [SerializeField] private float interactionDistanceTolerance = 0.15f;

        private DetectiveClickMover mover;
        private DetectiveInputActions inputActions;
        private IInteractable pendingInteractable;

        public string CurrentTargetName => pendingInteractable?.DisplayName ?? "None";

        public string LastInteractionMessage { get; private set; } = "None";
        public string InteractionPrompt { get; private set; } = "None";
        public bool IsAwaitingConfirmation => false;

        private void Awake()
        {
            mover = GetComponent<DetectiveClickMover>();

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            inputActions = new DetectiveInputActions();
            inputActions.Detective.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Detective.Disable();
            inputActions?.Dispose();
            inputActions = null;
        }

        private void Update()
        {
            if (inputActions != null && inputActions.Detective.Click.WasPressedThisFrame())
            {
                HandleClick(inputActions.Detective.Point.ReadValue<Vector2>());
            }

            UpdatePendingInteraction();
        }

        private void HandleClick(Vector2 screenPoint)
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[InteractionController] No camera is assigned.", this);
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableMask, QueryTriggerInteraction.Ignore))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<MonoBehaviour>() as IInteractable;
                if (interactable != null)
                {
                    pendingInteractable = interactable;
                    RequestMoveOrInteract(interactable);
                    return;
                }
            }

            ClearPendingInteraction();
            mover.RestoreDefaultStoppingDistance();
            mover.TryMoveToScreenPoint(screenPoint);
        }

        private void UpdatePendingInteraction()
        {
            if (pendingInteractable == null)
            {
                return;
            }

            Component targetComponent = pendingInteractable as Component;
            if (targetComponent == null || !targetComponent.gameObject.activeInHierarchy || !pendingInteractable.CanInteract)
            {
                ClearPendingInteraction();
                return;
            }

            float range = GetInteractionRange(pendingInteractable);
            if (GetInteractionDistance(targetComponent) <= range + interactionDistanceTolerance && !mover.IsMoving)
            {
                mover.Stop();
                ExecutePendingInteraction();
            }
            else
            {
                InteractionPrompt = $"Moving to {GetTargetName(pendingInteractable)}...";
            }
        }

        private void RequestMoveOrInteract(IInteractable interactable)
        {
            Component targetComponent = interactable as Component;
            if (targetComponent == null)
            {
                ClearPendingInteraction();
                return;
            }

            if (!interactable.CanInteract)
            {
                ClearPendingInteraction();
                return;
            }

            float range = GetInteractionRange(interactable);
            if (GetInteractionDistance(targetComponent) <= range + interactionDistanceTolerance)
            {
                mover.Stop();
                ExecutePendingInteraction();
                return;
            }

            InteractionPrompt = $"Moving to {GetTargetName(interactable)}...";
            Collider targetCollider = targetComponent.GetComponent<Collider>();
            Transform interactionPoint = interactable.InteractionPoint;
            Vector3 targetPosition = interactionPoint != null ? interactionPoint.position : targetComponent.transform.position;
            if (!mover.TryMoveToInteractionRange(targetPosition, targetCollider, range))
            {
                LastInteractionMessage = $"Could not reach {GetTargetName(interactable)}";
                ClearPendingInteraction();
            }
        }

        private void ExecutePendingInteraction()
        {
            if (pendingInteractable == null)
            {
                return;
            }

            string targetName = GetTargetName(pendingInteractable);
            pendingInteractable.Interact(gameObject);
            LastInteractionMessage = $"Interacted: {targetName}";
            ClearPendingInteraction();
            mover.RestoreDefaultStoppingDistance();
        }

        private void ClearPendingInteraction()
        {
            pendingInteractable = null;
            InteractionPrompt = "None";
        }

        private float GetInteractionRange(IInteractable interactable)
        {
            return Mathf.Max(0.1f, interactable != null ? interactable.InteractionRange : interactionDistance);
        }

        private float GetInteractionDistance(Component targetComponent)
        {
            Collider targetCollider = targetComponent.GetComponent<Collider>();
            Vector3 closestPoint = targetCollider != null
                ? targetCollider.ClosestPoint(transform.position)
                : targetComponent.transform.position;
            return GetFlatDistance(transform.position, closestPoint);
        }

        private static float GetFlatDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private static string GetTargetName(IInteractable interactable)
        {
            return interactable?.DisplayName ?? "Interactable";
        }
    }
}