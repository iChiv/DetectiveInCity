using UnityEngine;

namespace Detective
{
    public sealed class DetectiveTriggerRing : MonoBehaviour
    {
        [SerializeField] private GameObject ring;
        [SerializeField] private string hideFlag;
        [SerializeField] private int minCluesToShow;

        private bool revealed;

        private void Update()
        {
            if (ring == null)
            {
                return;
            }

            var encounter = GetComponent<DetectiveFinalDuelTrigger>();
            if (encounter != null && !encounter.CanStartAutomaticEncounter)
            {
                ring.SetActive(false);
                revealed = false;
                return;
            }

            if (!revealed)
            {
                // 条件显示：线索数达标前保持隐藏（如最终对决环需玩家线索 ≥10）。
                if (minCluesToShow > 0
                    && DetectiveInvestigationState.CollectedClueCount < minCluesToShow)
                {
                    return;
                }

                revealed = true;
                ring.SetActive(true);
            }

            if (!ring.activeSelf)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(hideFlag) && DetectiveGameState.HasFlag(hideFlag))
            {
                ring.SetActive(false);
            }
        }
    }
}
