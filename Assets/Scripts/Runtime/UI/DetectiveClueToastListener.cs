using UnityEngine;

namespace Detective
{
    // 订阅全局线索/声音事件，把收集反馈打到 toast，替代仅 Debug.Log 的现状。
    public sealed class DetectiveClueToastListener : MonoBehaviour
    {
        private readonly int[] lastVoiceLevels = new int[5];

        private void OnEnable()
        {
            for (int i = 1; i < lastVoiceLevels.Length; i++)
                lastVoiceLevels[i] = DetectiveGameState.GetVoice((DetectiveVoiceType)i);
            DetectiveGameState.OnClueCollected += HandleClueCollected;
            DetectiveGameState.OnVoiceChanged += HandleVoiceChanged;
        }

        private void OnDisable()
        {
            DetectiveGameState.OnClueCollected -= HandleClueCollected;
            DetectiveGameState.OnVoiceChanged -= HandleVoiceChanged;
        }

        private static void HandleClueCollected(string clueId)
        {
            if (DetectiveToastUI.Instance == null)
            {
                return;
            }

            string title = DetectiveClueCatalog.GetTitle(clueId);
            DetectiveToastUI.Instance.Show($"获得线索【{title}】");
        }

        private void HandleVoiceChanged(DetectiveVoiceType voice, int level)
        {
            int index = (int)voice;
            if (index <= 0 || index >= lastVoiceLevels.Length)
            {
                return;
            }

            bool leveledUp = level > lastVoiceLevels[index];
            lastVoiceLevels[index] = level;
            if (!leveledUp || DetectiveToastUI.Instance == null)
            {
                return;
            }

            string voiceName = DetectiveVoiceStyle.GetName(voice);
            if (!string.IsNullOrEmpty(voiceName))
            {
                DetectiveToastUI.Instance.Show($"{voiceName} 提升了");
            }
        }
    }
}
