using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    [RequireComponent(typeof(Collider))]
    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        public enum InteractableType
        {
            Npc,
            Clue
        }

        [SerializeField] private string interactionId;
        [SerializeField] private DetectiveCharacterDefinition characterDefinition;
        [SerializeField] private DetectiveClueDefinition clueDefinition;
        [SerializeField] private string interactionLabel = "Interact";
        [SerializeField] private string displayName = "Interactable";
        [SerializeField] private string clueId;
        [SerializeField] private InteractableType interactableType = InteractableType.Npc;
        [SerializeField] private float interactionRange = 1.5f;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private bool hideWhenCollected = true;

        private static readonly HashSet<string> activeInteractionIds = new();

        private DetectiveWorldLabel worldLabel;
        private Renderer targetRenderer;
        private Collider targetCollider;
        private bool registeredInteractionId;

        public string InteractionId => GetEffectiveInteractionId();
        public string InteractionLabel => interactionLabel;
        public string DisplayName => GetEffectiveDisplayName();
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => Mathf.Max(0.1f, interactionRange);
        public bool IsCollected { get; private set; }
        public bool IsNpc => interactableType == InteractableType.Npc;
        public bool CanInteract => enabled && gameObject.activeInHierarchy && (IsNpc || !IsCollected);

        private void OnEnable()
        {
            RegisterInteractionId();
        }

        private void OnDisable()
        {
            UnregisterInteractionId();
        }

        private void Awake()
        {
            ValidateConfiguration();
            targetRenderer = GetComponent<Renderer>();
            targetCollider = GetComponent<Collider>();
            worldLabel = GetComponent<DetectiveWorldLabel>();
            if (worldLabel == null)
            {
                worldLabel = gameObject.AddComponent<DetectiveWorldLabel>();
            }

            worldLabel.Initialize(this);
            IsCollected = !IsNpc && DetectiveInvestigationState.IsCollected(GetEffectiveClueId());
            if (IsCollected)
            {
                ApplyCollectedVisualState();
            }
        }

        public void SetDebugLabelVisible(bool visible)
        {
            if (worldLabel == null)
            {
                worldLabel = gameObject.AddComponent<DetectiveWorldLabel>();
                worldLabel.Initialize(this);
            }

            worldLabel.SetVisible(visible);
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            if (IsNpc)
            {
                FaceInteractor(interactor.transform);
                Debug.Log($"[TestInteractable] {interactor.name} talked to {DisplayName} ({InteractionId}).", this);
                return;
            }

            string effectiveClueId = GetEffectiveClueId();
            if (string.IsNullOrWhiteSpace(effectiveClueId))
            {
                Debug.LogError($"[TestInteractable] Clue '{DisplayName}' is missing a clue ID.", this);
                return;
            }

            IsCollected = DetectiveInvestigationState.RegisterClue(effectiveClueId);
            if (IsCollected)
            {
                ApplyCollectedVisualState();
                Debug.Log($"[TestInteractable] {interactor.name} collected clue {DisplayName} ({InteractionId}).", this);
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(GetEffectiveInteractionId()))
            {
                Debug.LogError($"[TestInteractable] '{name}' is missing an interaction ID.", this);
            }

            if (!IsNpc && string.IsNullOrWhiteSpace(GetEffectiveClueId()))
            {
                Debug.LogError($"[TestInteractable] Clue '{name}' is missing a clue ID.", this);
            }
        }

        private string GetEffectiveInteractionId()
        {
            if (IsNpc && characterDefinition != null && !string.IsNullOrWhiteSpace(characterDefinition.CharacterId))
            {
                return characterDefinition.CharacterId;
            }

            if (!IsNpc && clueDefinition != null && !string.IsNullOrWhiteSpace(clueDefinition.ClueId))
            {
                return clueDefinition.ClueId;
            }

            return interactionId;
        }

        private string GetEffectiveDisplayName()
        {
            if (IsNpc && characterDefinition != null && !string.IsNullOrWhiteSpace(characterDefinition.DisplayName))
            {
                return characterDefinition.DisplayName;
            }

            if (!IsNpc && clueDefinition != null && !string.IsNullOrWhiteSpace(clueDefinition.Title))
            {
                return clueDefinition.Title;
            }

            return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        }

        private string GetEffectiveClueId()
        {
            if (clueDefinition != null && !string.IsNullOrWhiteSpace(clueDefinition.ClueId))
            {
                return clueDefinition.ClueId;
            }

            return clueId;
        }

        private void RegisterInteractionId()
        {
            string effectiveInteractionId = GetEffectiveInteractionId();
            if (string.IsNullOrWhiteSpace(effectiveInteractionId) || registeredInteractionId)
            {
                return;
            }

            if (!activeInteractionIds.Add(effectiveInteractionId))
            {
                Debug.LogError($"[TestInteractable] Duplicate interaction ID '{effectiveInteractionId}' detected on '{name}'.", this);
                return;
            }

            registeredInteractionId = true;
        }

        private void UnregisterInteractionId()
        {
            if (!registeredInteractionId)
            {
                return;
            }

            activeInteractionIds.Remove(GetEffectiveInteractionId());
            registeredInteractionId = false;
        }

        private void FaceInteractor(Transform interactor)
        {
            Vector3 direction = interactor.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void ApplyCollectedVisualState()
        {
            if (hideWhenCollected && targetRenderer != null)
            {
                targetRenderer.enabled = false;
            }

            if (hideWhenCollected && targetCollider != null)
            {
                targetCollider.enabled = false;
            }

            if (worldLabel != null)
            {
                worldLabel.SetCollected(true);
            }
        }
    }
}