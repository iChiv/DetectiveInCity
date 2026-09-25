using UnityEngine;

namespace Detective
{
    [RequireComponent(typeof(Collider))]
    public sealed class DetectiveProximityDialogue : MonoBehaviour
    {
        [SerializeField] private DetectiveDialogueDefinition dialogueDefinition;
        [SerializeField] private bool onceOnly = true;
        [SerializeField] private string doneFlag;
        [SerializeField] private string requiredFlag;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerStay(Collider other) => OnTriggerEnter(other);

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<DetectiveClickMover>() != null) TryStartDialogue();
        }

        public void HandleRegionArrival(string previousRegion)
        {
            if (gameObject.scene.name == "D1_RedLight" && gameObject.name == "WomanProximity")
                TryStartDialogue();
        }

        private void TryStartDialogue()
        {
            if (!string.IsNullOrEmpty(requiredFlag) && !DetectiveGameState.HasFlag(requiredFlag)) return;
            if (dialogueDefinition == null) return;
            if (DetectiveRegionLoader.Instance != null && DetectiveRegionLoader.Instance.IsLoading) return;
            if (!string.IsNullOrEmpty(dialogueDefinition.CompletionFlag) && DetectiveGameState.HasFlag(dialogueDefinition.CompletionFlag)) return;

            if (onceOnly && DetectiveGameState.HasFlag(EffectiveDoneFlag))
            {
                return;
            }

            DetectiveDialogueRunner runner = DetectiveDialogueRunner.Instance != null
                ? DetectiveDialogueRunner.Instance
                : FindFirstObjectByType<DetectiveDialogueRunner>();
            if (InteractionController.DialogueBlock || runner == null || runner.IsDialogueActive)
            {
                return;
            }

            if (onceOnly)
            {
                DetectiveGameState.SetFlag(EffectiveDoneFlag);
            }

            foreach (var mover in FindObjectsByType<DetectiveClickMover>(FindObjectsSortMode.None)) mover.Stop();
            runner.StartDialogue(dialogueDefinition);
        }

        private string EffectiveDoneFlag => string.IsNullOrWhiteSpace(doneFlag) ? $"proximity_done_{gameObject.name}" : doneFlag;
    }
}
