using UnityEngine;

namespace Detective
{
    [RequireComponent(typeof(Collider))]
    public sealed class DetectiveFinalDuelTrigger : MonoBehaviour
    {
        [SerializeField] private DetectiveDuelDefinition duelDefinition;
        [SerializeField] private int minClueCount = 10;
        [SerializeField] private string doneFlag = "final_duel_triggered";
        [SerializeField] private string degradedRequiredClueId;
        [SerializeField] private string degradedEncounterFlag;
        [SerializeField] private DetectiveDialogueDefinition degradedDialogue;
        [SerializeField] private string requiredFlag;
        [SerializeField] private string arrivalFromRegion;
        private bool degradedDialoguePending;
        private bool waitUntilPlayerLeaves;

        private void Update()
        {
            if (!waitUntilPlayerLeaves) return;
            var player = FindFirstObjectByType<DetectiveClickMover>();
            if (player == null) return;
            var bounds = GetComponent<Collider>().bounds;
            bounds.Expand(0.8f);
            if (!bounds.Contains(player.transform.position)) waitUntilPlayerLeaves = false;
        }

        public bool IsFinalEncounter => doneFlag == "final_duel_triggered";
        public bool CanStartAutomaticEncounter =>
            (IsFinalEncounter && DetectiveGameState.HasFlag("final_duel_deferred"))
            || (string.IsNullOrEmpty(requiredFlag) || DetectiveGameState.HasFlag(requiredFlag))
            && (!IsFinalEncounter || HasCompletedResidentialInvestigation);

        public static bool HasCompletedResidentialInvestigation =>
            DetectiveGameState.HasFlag("dlg_wife_decision_done")
            && (DetectiveInvestigationState.IsCollected(DetectiveClueIds.MirrorBruise)
                || DetectiveInvestigationState.IsCollected(DetectiveClueIds.SleepingPillsEmpty)
                || DetectiveInvestigationState.IsCollected(DetectiveClueIds.DismissalNotice)
                || DetectiveInvestigationState.IsCollected(DetectiveClueIds.YourDiaryDoubt));
        [SerializeField, TextArea(2, 3)] private string degradedMessage = "风衣男撑着黑伞拦住你：“你不该查下去的。”（专注力 -10，找到关键证据再来。）";
        [SerializeField] private int degradedFocusCost = 10;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<DetectiveClickMover>() != null) TryStartEncounter();
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.GetComponentInParent<DetectiveClickMover>() != null) TryStartEncounter();
        }

        public void HandleRegionArrival(string previousRegion)
        {
            if (!string.IsNullOrEmpty(arrivalFromRegion) && previousRegion == arrivalFromRegion)
            {
                TryStartEncounter();
            }
        }

        private void TryStartEncounter()
        {
            if (DetectiveRegionLoader.Instance != null && DetectiveRegionLoader.Instance.IsLoading)
            {
                return;
            }

            if (waitUntilPlayerLeaves || !CanStartAutomaticEncounter) return;

            if (InteractionController.DialogueBlock
                || (DetectiveDialogueRunner.Instance != null && DetectiveDialogueRunner.Instance.IsDialogueActive)
                || (DetectiveDuelRunner.Instance != null && DetectiveDuelRunner.Instance.IsDuelActive)
                || (DetectiveEndingUI.Instance != null && DetectiveEndingUI.Instance.IsShowing))
            {
                return;
            }

            if (DetectiveGameState.HasFlag(doneFlag))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(degradedRequiredClueId)
                && !DetectiveInvestigationState.IsCollected(degradedRequiredClueId))
            {
                if (degradedDialoguePending
                    || (!string.IsNullOrWhiteSpace(degradedEncounterFlag)
                        && DetectiveGameState.HasFlag(degradedEncounterFlag)))
                {
                    return;
                }

                DetectiveDialogueRunner dialogueRunner = DetectiveDialogueRunner.Instance;
                if (degradedDialogue != null && dialogueRunner != null && !dialogueRunner.IsDialogueActive)
                {
                    degradedDialoguePending = true;
                    DetectiveGameState.AddFocus(-degradedFocusCost);
                    dialogueRunner.StartDialogue(degradedDialogue);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(degradedEncounterFlag))
                {
                    DetectiveGameState.SetFlag(degradedEncounterFlag);
                }
                if (DetectiveToastUI.Instance != null)
                {
                    DetectiveToastUI.Instance.Show(degradedMessage, 4f);
                }
                DetectiveGameState.AddFocus(-degradedFocusCost);
                return;
            }

            if (DetectiveInvestigationState.CollectedClueCount < minClueCount)
            {
                return;
            }

            ForceStartDuel();
        }

        public void ForceStartDuel()
        {
            if (DetectiveGameState.HasFlag(doneFlag) || DetectiveInvestigationState.CollectedClueCount < minClueCount)
            {
                return;
            }

            if (duelDefinition == null)
            {
                Debug.LogWarning("[DetectiveFinalDuelTrigger] 未配置对决定义。", this);
                return;
            }

            DetectiveDuelRunner runner = DetectiveDuelRunner.Instance != null
                ? DetectiveDuelRunner.Instance
                : FindFirstObjectByType<DetectiveDuelRunner>();
            if (runner == null || runner.IsDuelActive)
            {
                return;
            }

            runner.RequestDuel(duelDefinition, () => DetectiveGameState.SetFlag(doneFlag), () =>
            {
                waitUntilPlayerLeaves = true;
                if (IsFinalEncounter) DetectiveGameState.SetFlag("final_duel_deferred");
            });
        }
    }
}
