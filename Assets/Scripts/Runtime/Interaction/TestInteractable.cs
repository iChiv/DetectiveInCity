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

        public enum MarkerStyle
        {
            None,
            Label,
            GlowRing
        }

        [SerializeField] private string interactionId;
        [SerializeField] private DetectiveCharacterDefinition characterDefinition;
        [SerializeField] private DetectiveClueDefinition clueDefinition;
        [SerializeField] private string interactionLabel = "Interact";
        [SerializeField] private string displayName = "Interactable";
        [SerializeField] private string clueId;
        [SerializeField] private InteractableType interactableType = InteractableType.Npc;
        [SerializeField] private DetectiveDialogueDefinition dialogueDefinition;
        [SerializeField] private string dialogueUnlockFlag;
        [SerializeField] private DetectiveDialogueDefinition beforeUnlockDialogue;
        [SerializeField] private string followupDialogueFlag;
        [SerializeField] private DetectiveDialogueDefinition followupDialogue;
        [SerializeField] private DetectiveDialogueDefinition examineDialogue;
        [SerializeField] private DetectiveVoiceType requiredVoice;
        [SerializeField] private int requiredVoiceLevel = 1;
        [SerializeField] private DetectiveDialogueDefinition lockedDialogue;
        [SerializeField] private DetectiveDialogueDefinition.VoiceChange[] onCollectVoiceChanges;
        [SerializeField] private int onCollectTimeAdvance;
        [SerializeField] private float interactionRange = 1.5f;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private bool hideWhenCollected = true;
        [SerializeField] private MarkerStyle markerStyle = MarkerStyle.Label;

        private static readonly HashSet<string> activeInteractionIds = new();

        private DetectiveWorldLabel worldLabel;
        private Renderer targetRenderer;
        private Renderer clueMarkerRenderer;
        private Collider targetCollider;
        private bool registeredInteractionId;

        public string InteractionId => GetEffectiveInteractionId();
        public string InteractionLabel => interactionLabel;
        public string DisplayName => GetEffectiveDisplayName();
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => Mathf.Max(0.1f, interactionRange);
        public bool IsCollected { get; private set; }
        public bool IsNpc => interactableType == InteractableType.Npc;
        private bool CanCompareGateKey => GetEffectiveClueId() == DetectiveClueIds.GateForced
            && DetectiveInvestigationState.IsCollected(DetectiveClueIds.DroppedKey)
            && !DetectiveGameState.HasReasoningResult("gate_key_match");
        public bool CanInteract => enabled && gameObject.activeInHierarchy && (IsNpc || !IsCollected || CanCompareGateKey);

        private void LateUpdate()
        {
            if (GetEffectiveClueId() == DetectiveClueIds.GateForced && clueMarkerRenderer != null)
                clueMarkerRenderer.enabled = !IsCollected || CanCompareGateKey;
        }

        private void CompareGateKey()
        {
            if (!DetectiveInvestigationState.IsCollected(DetectiveClueIds.DroppedKey))
            {
                DetectiveToastUI.Instance?.Show("铁门已经被撬开。锁孔留下新划痕，目前还没有可核对的钥匙。", 4f);
                return;
            }
            var recipe = Resources.Load<DetectiveReasoningRecipe>("Detective/Recipes/recipe_gate_key");
            if (recipe == null) return;
            DetectiveGameState.RecordReasoningResult(recipe.EffectiveId);
            DetectiveToastUI.Instance?.Show(recipe.ResultText + "\n已记入思维面板的推理结果。", 6f);
            if (clueMarkerRenderer != null) clueMarkerRenderer.enabled = false;
        }

        private void OnEnable()
        {
            RegisterInteractionId();
            if (clueMarkerRenderer != null) clueMarkerRenderer.enabled = !IsCollected;
        }

        private void OnDisable()
        {
            UnregisterInteractionId();
            if (clueMarkerRenderer != null) clueMarkerRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (clueMarkerRenderer != null)
            {
                var filter = clueMarkerRenderer.GetComponent<MeshFilter>();
                if (filter != null) Destroy(filter.sharedMesh);
                Destroy(clueMarkerRenderer.gameObject);
            }
        }

        private void Awake()
        {
            ValidateConfiguration();
            // Clues are identified by a ground ring only. NPCs keep their short world label.
            if (!IsNpc)
            {
                markerStyle = MarkerStyle.GlowRing;
            }
            targetRenderer = GetComponent<Renderer>();
            targetCollider = GetComponent<Collider>();
            if (markerStyle == MarkerStyle.Label)
            {
                worldLabel = GetComponent<DetectiveWorldLabel>();
                if (worldLabel == null)
                {
                    worldLabel = gameObject.AddComponent<DetectiveWorldLabel>();
                }

                worldLabel.Initialize(this);
            }
            else if (markerStyle == MarkerStyle.GlowRing)
            {
                CreateClueGlowRing();
            }

            IsCollected = !IsNpc && DetectiveInvestigationState.IsCollected(GetEffectiveClueId());
            if (IsCollected)
            {
                ApplyCollectedVisualState();
            }
        }

        public void SetDebugLabelVisible(bool visible)
        {
            if (markerStyle == MarkerStyle.GlowRing)
            {
                if (clueMarkerRenderer != null)
                {
                    clueMarkerRenderer.enabled = visible && !IsCollected;
                }

                return;
            }

            if (markerStyle == MarkerStyle.None)
            {
                return;
            }

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
                DetectiveDialogueDefinition selectedDialogue = dialogueDefinition;
                if (!string.IsNullOrWhiteSpace(dialogueUnlockFlag)
                    && !DetectiveGameState.HasFlag(dialogueUnlockFlag))
                {
                    selectedDialogue = beforeUnlockDialogue;
                }
                else if (!string.IsNullOrWhiteSpace(followupDialogueFlag)
                    && DetectiveGameState.HasFlag(followupDialogueFlag))
                {
                    selectedDialogue = followupDialogue;
                }

                if (selectedDialogue != null)
                {
                    FindDialogueRunner()?.StartDialogue(selectedDialogue);
                    return;
                }

                Debug.Log($"[TestInteractable] {interactor.name} talked to {DisplayName} ({InteractionId}).", this);
                return;
            }

            string effectiveClueId = GetEffectiveClueId();
            if (string.IsNullOrWhiteSpace(effectiveClueId))
            {
                Debug.LogError($"[TestInteractable] Clue '{DisplayName}' is missing a clue ID.", this);
                return;
            }

            if (effectiveClueId == DetectiveClueIds.GateForced)
            {
                if (!IsCollected) CollectEffectiveClue(effectiveClueId);
                CompareGateKey();
                return;
            }

            if (effectiveClueId == DetectiveClueIds.Recording)
            {
                DetectiveInspectionUI.Get()?.Password("保险箱", "输入四位密码打开保险箱。\n密码可能藏在死者留下的日记里。", "2077", () =>
                {
                    if (this == null || IsCollected) return;
                    CollectEffectiveClue(effectiveClueId);
                    var clue = Resources.Load<DetectiveClueDefinition>("Detective/Clues/" + effectiveClueId);
                    DetectiveInspectionUI.Get()?.Read("录音回放", clue != null ? clue.Description : "你从保险箱里取出录音笔。");
                });
                return;
            }

            if (requiredVoice != DetectiveVoiceType.None
                && DetectiveGameState.GetVoice(requiredVoice) < Mathf.Max(1, requiredVoiceLevel))
            {
                if (lockedDialogue != null)
                {
                    FindDialogueRunner()?.StartDialogue(lockedDialogue);
                }
                else
                {
                    Debug.Log($"[TestInteractable] {DisplayName} 需要 {requiredVoice} 等级 {requiredVoiceLevel} 以上。", this);
                }

                return;
            }

            if (examineDialogue != null)
            {
                CollectEffectiveClue(effectiveClueId);
                FindDialogueRunner()?.StartDialogue(examineDialogue);
                return;
            }

            CollectEffectiveClue(effectiveClueId);
            if (IsCollected)
            {
                Debug.Log($"[TestInteractable] {interactor.name} collected clue {DisplayName} ({InteractionId}).", this);
            }
        }

        private void CollectEffectiveClue(string effectiveClueId)
        {
            IsCollected = DetectiveGameState.CollectClue(effectiveClueId);
            if (!IsCollected)
            {
                return;
            }

            ApplyCollectedVisualState();

            if (onCollectVoiceChanges != null)
            {
                foreach (DetectiveDialogueDefinition.VoiceChange change in onCollectVoiceChanges)
                {
                    DetectiveGameState.AddVoice(change.voice, change.delta);
                }
            }

            if (onCollectTimeAdvance > 0)
            {
                DetectiveGameState.AdvanceTime(onCollectTimeAdvance);
            }
        }

        private static DetectiveDialogueRunner FindDialogueRunner()
        {
            return DetectiveDialogueRunner.Instance != null
                ? DetectiveDialogueRunner.Instance
                : FindFirstObjectByType<DetectiveDialogueRunner>();
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
            if (!string.IsNullOrWhiteSpace(interactionId))
            {
                return interactionId;
            }

            if (IsNpc && characterDefinition != null && !string.IsNullOrWhiteSpace(characterDefinition.CharacterId))
            {
                return characterDefinition.CharacterId;
            }

            if (!IsNpc && clueDefinition != null && !string.IsNullOrWhiteSpace(clueDefinition.ClueId))
            {
                return clueDefinition.ClueId;
            }

            return name;
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

        private static Material clueMarkerMaterial;

        private static Material GetClueMarkerMaterial()
        {
            if (clueMarkerMaterial == null)
            {
                clueMarkerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "ClueMarkerGold" };
                clueMarkerMaterial.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 0.6f));
                clueMarkerMaterial.SetFloat("_Surface", 1f);
                clueMarkerMaterial.SetFloat("_Blend", 0f);
                clueMarkerMaterial.SetFloat("_ZWrite", 0f);
                clueMarkerMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                clueMarkerMaterial.renderQueue = 3000;
            }

            return clueMarkerMaterial;
        }

        private void CreateClueGlowRing()
        {
            var marker = new GameObject("ClueGlowRing", typeof(MeshFilter), typeof(MeshRenderer));
            marker.transform.SetParent(transform.root, false);
            // A hollow ring remains visible around opaque objects such as the safe.
            const int segments = 48;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 12];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 v = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = v * 0.5f;
                vertices[i * 2 + 1] = v * 0.44f;
                int a = i * 2, b = ((i + 1) % segments) * 2;
                int[] face = { a, b, a+1, a+1, b, b+1, a+1, b, a, b+1, b, a+1 };
                for (int j = 0; j < 12; j++) triangles[i * 12 + j] = face[j];
            }
            var mesh = new Mesh { name = "ClueSurfaceRing" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            marker.GetComponent<MeshFilter>().sharedMesh = mesh;
            clueMarkerRenderer = marker.GetComponent<Renderer>();
            clueMarkerRenderer.sharedMaterial = GetClueMarkerMaterial();
            PlaceClueRing();
        }

        private void Start()
        {
            // All supporting scene colliders are available after Awake.
            if (clueMarkerRenderer != null) { Physics.SyncTransforms(); PlaceClueRing(); }
        }

        private void PlaceClueRing()
        {
            Bounds bounds = targetRenderer != null ? targetRenderer.bounds : new Bounds(transform.position, Vector3.one * 0.3f);
            Vector3 normal = Vector3.up;
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            float width = bounds.size.x, height = bounds.size.z;
            // Thin vertical props use their mounting face rather than the floor.
            bool wall = bounds.size.y > Mathf.Min(bounds.size.x, bounds.size.z) * 1.8f
                && Mathf.Min(bounds.size.x, bounds.size.z) < 0.3f;
            if (wall)
            {
                normal = bounds.size.x < bounds.size.z ? Vector3.right : Vector3.forward;
                // Prefer the face looking toward the interior of the room.
                if (Vector3.Dot(normal, -bounds.center) < 0f) normal = -normal;
                // Interior partitions need the exposed face, which may face away from room origin.
                Vector3 probe = bounds.center + Vector3.Scale(normal, bounds.extents) + normal * 0.04f;
                foreach (var obstacle in Physics.OverlapSphere(probe, 0.015f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (obstacle == targetCollider || obstacle.transform.IsChildOf(transform)) continue;
                    normal = -normal;
                    break;
                }
                center = bounds.center + Vector3.Scale(normal, bounds.extents);
                width = bounds.size.x < bounds.size.z ? bounds.size.z : bounds.size.x;
                height = bounds.size.y;
            }
            else
            {
                float nearest = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(bounds.center, Vector3.down, bounds.extents.y + 0.4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == targetCollider || hit.collider.transform.IsChildOf(transform)) continue;
                    if (hit.distance < nearest) { nearest = hit.distance; center.y = hit.point.y; }
                }
            }
            var marker = clueMarkerRenderer.transform;
            marker.position = center + normal * 0.018f;
            marker.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            // Circumscribe the rectangle so the rim is not buried in its corners.
            float diameter = Mathf.Max(0.55f, Mathf.Sqrt(width * width + height * height) + 0.16f);
            marker.localScale = Vector3.one * diameter;
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

            if (clueMarkerRenderer != null)
            {
                clueMarkerRenderer.enabled = false;
            }

            if (worldLabel != null)
            {
                worldLabel.SetCollected(true);
            }
        }
    }
}
