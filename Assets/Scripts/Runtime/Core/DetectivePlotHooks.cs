using UnityEngine;

namespace Detective
{
    public sealed class DetectivePlotHooks : MonoBehaviour
    {
        private const string EndingChoiceFlagPrefix = "ending_choice_";
        private const string FinalDuelFailedFlag = "final_duel_failed";

        private bool endingTriggered;

        private void OnEnable()
        {
            DetectiveGameState.OnTimeChanged += HandleTimeChanged;
            DetectiveGameState.OnFlagChanged += HandleFlagChanged;
            HandleTimeChanged(DetectiveGameState.TotalMinutes);
        }

        private void OnDisable()
        {
            DetectiveGameState.OnTimeChanged -= HandleTimeChanged;
            DetectiveGameState.OnFlagChanged -= HandleFlagChanged;
        }

        private void HandleTimeChanged(int totalMinutes)
        {
            if (totalMinutes >= DetectiveGameState.EndMinutes && !DetectiveGameState.HasFlag("final_duel_triggered"))
            {
                TriggerEndingOnce();
            }
        }

        private void HandleFlagChanged(string flag)
        {
            if (flag.StartsWith(EndingChoiceFlagPrefix, System.StringComparison.Ordinal)
                || flag == FinalDuelFailedFlag)
            {
                TriggerEndingOnce();
            }
        }

        private void TriggerEndingOnce()
        {
            if (endingTriggered)
            {
                return;
            }

            endingTriggered = true;
            var ending = DetectiveEndingResolver.Evaluate();
            Debug.Log($"[PlotHooks] 触发结局: {ending}, EndingUI.Instance={(DetectiveEndingUI.Instance != null ? "ok" : "null")}");
            if (DetectiveEndingUI.Instance != null)
            {
                DetectiveEndingUI.Instance.PlayEnding(ending);
            }
        }
    }
}
