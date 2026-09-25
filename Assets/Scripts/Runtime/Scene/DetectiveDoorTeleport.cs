using UnityEngine;
using TMPro;

namespace Detective
{
    public sealed class DetectiveDoorTeleport : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionId;
        [SerializeField] private string displayName = "前往";
        [SerializeField] private string targetRegionId;
        [SerializeField] private string targetSpawnId = "default";
        [SerializeField] private float interactionRange = 1.6f;
        [SerializeField] private string exitLabel = "出口";
        [SerializeField, HideInInspector] private bool decorationScaleNormalized;

        private const float LabelFadeFullDistance = 6f;
        private const float LabelFadeMaxDistance = 10f;

        private TextMeshPro worldLabel;
        private Transform playerTransform;
        private Color labelBaseColor = Color.white;
        [SerializeField] private DetectiveDialogueDefinition leavingDialogue;
        private bool waitingForLeavingThought;

        private void Update()
        {
            if (!waitingForLeavingThought || DetectiveDialogueRunner.Instance == null || DetectiveDialogueRunner.Instance.IsDialogueActive) return;
            waitingForLeavingThought = false;
            if (DetectiveGameState.HasFlag("home_exit_selected")) Travel();
        }

        public string InteractionId => string.IsNullOrWhiteSpace(interactionId) ? name : interactionId;
        public string DisplayName => displayName;
        public string InteractionLabel => exitLabel;
        public Transform InteractionPoint => transform;
        public float InteractionRange => Mathf.Max(0.1f, interactionRange);
        public float EntryFloorHeight => GetComponent<Renderer>().bounds.min.y;

        public bool IsOnEntryLevel(float feetY) => Mathf.Abs(feetY - EntryFloorHeight) <= 0.55f;

        public bool CanInteract => enabled && gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(targetRegionId);

        private void Awake()
        {
            if (!decorationScaleNormalized)
            {
                NormalizeDecorationScale();
            }

            worldLabel = GetComponentInChildren<TextMeshPro>(true);
            if (worldLabel != null)
            {
                // Door geometry has non-uniform scale. A billboard cannot rotate correctly
                // beneath that transform, so keep the label beside the door in world space.
                Vector3 labelPosition = worldLabel.transform.position;
                Quaternion labelRotation = worldLabel.transform.rotation;
                worldLabel.transform.SetParent(transform.parent, true);
                worldLabel.transform.position = labelPosition;
                worldLabel.transform.localScale = Vector3.one * 0.4f;
                worldLabel.transform.rotation = labelRotation;
                labelBaseColor = worldLabel.color;
            }
        }

        // 旧区域场景的门框、标签和门槛按世界尺寸写入 localScale，
        // 但父门本身已经缩放。抵消父级缩放，避免整套装饰再次放大。
        public void NormalizeDecorationScale()
        {
            if (decorationScaleNormalized)
            {
                return;
            }

            Vector3 parentScale = transform.localScale;
            if (Mathf.Abs(parentScale.x) < 0.001f || Mathf.Abs(parentScale.y) < 0.001f || Mathf.Abs(parentScale.z) < 0.001f)
            {
                return;
            }

            foreach (Transform child in transform)
            {
                if (child.name != "FrameBar" && child.name != "DoorLabel" && child.name != "DoorThreshold")
                {
                    continue;
                }

                child.localPosition = new Vector3(
                    child.localPosition.x / parentScale.x,
                    child.localPosition.y / parentScale.y,
                    child.localPosition.z / parentScale.z);
                child.localScale = new Vector3(
                    child.localScale.x / parentScale.x,
                    child.localScale.y / parentScale.y,
                    child.localScale.z / parentScale.z);
                if (child.name == "FrameBar" && child.TryGetComponent(out Collider frameCollider))
                {
                    frameCollider.enabled = false;
                }
            }

            decorationScaleNormalized = true;
        }

        private void LateUpdate()
        {
            if (worldLabel == null || !worldLabel.gameObject.activeSelf)
            {
                return;
            }

            // 标签固定朝向（不广告牌），只做距离淡入：10m 外透明，6m 内全亮（门槛条常亮，不参与）。
            ResolvePlayer();
            if (playerTransform == null)
            {
                return;
            }

            Vector3 flatDoor = transform.position;
            Vector3 flatPlayer = playerTransform.position;
            flatDoor.y = 0f;
            flatPlayer.y = 0f;
            float fade = Mathf.Clamp01(
                (LabelFadeMaxDistance - Vector3.Distance(flatDoor, flatPlayer))
                / (LabelFadeMaxDistance - LabelFadeFullDistance));
            Color faded = labelBaseColor;
            faded.a *= fade;
            worldLabel.color = faded;
        }

        private void OnDestroy()
        {
            if (worldLabel != null)
            {
                Destroy(worldLabel.gameObject);
            }
        }

        // 玩家在 Core 场景常驻，但 additive 区域切换后引用可能失效，惰性重找。
        private void ResolvePlayer()
        {
            if (playerTransform != null)
            {
                return;
            }

            GameObject player = GameObject.Find("DetectivePlayer");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            if (leavingDialogue != null && !DetectiveGameState.HasFlag("dlg_leaving_home_done"))
            {
                var runner = DetectiveDialogueRunner.Instance;
                if (runner != null && !runner.IsDialogueActive)
                {
                    waitingForLeavingThought = true;
                    foreach (var mover in FindObjectsByType<DetectiveClickMover>(FindObjectsSortMode.None)) mover.Stop();
                    runner.StartDialogue(leavingDialogue);
                    return;
                }
            }
            Travel();
        }

        private void Travel()
        {
            if (DetectiveRegionLoader.Instance != null)
            {
                if (gameObject.scene.name == "I2_Apartment" && targetRegionId == "D2_Financial")
                    DetectiveGameState.SetFlag("left_I2_Apartment");
                if (gameObject.scene.name == "I4_YourHome" && targetRegionId == "D4_Residential")
                    DetectiveGameState.SetFlag("left_I4_YourHome");
                DetectiveRegionLoader.Instance.LoadRegion(targetRegionId, targetSpawnId);
            }
        }
    }
}
