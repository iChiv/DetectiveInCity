using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveGameHUD : MonoBehaviour
    {
        private static readonly Color FocusNormalColor = new Color32(0x5D, 0xBB, 0x63, 0xFF);
        private static readonly Color FocusLowColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);
        private const float LowFocusThreshold = 30f;

        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI focusPercentText;
        [SerializeField] private Image focusFill;

        private void OnEnable()
        {
            DetectiveGameState.OnTimeChanged += HandleTimeChanged;
            DetectiveGameState.OnFocusChanged += HandleFocusChanged;
            Refresh();
        }

        private void OnDisable()
        {
            DetectiveGameState.OnTimeChanged -= HandleTimeChanged;
            DetectiveGameState.OnFocusChanged -= HandleFocusChanged;
        }

        public void Refresh()
        {
            RefreshTime();
            RefreshFocus();
        }

        private void HandleTimeChanged(int totalMinutes)
        {
            RefreshTime();
        }

        private void HandleFocusChanged(float focus)
        {
            RefreshFocus();
        }

        private void RefreshTime()
        {
            if (timeText != null)
            {
                timeText.text = DetectiveGameState.TimeText;
            }
        }

        private void RefreshFocus()
        {
            if (focusFill == null)
            {
                return;
            }

            float focus = DetectiveGameState.Focus;
            focusFill.fillAmount = focus / DetectiveGameState.MaxFocus;
            focusFill.color = focus < LowFocusThreshold ? FocusLowColor : FocusNormalColor;
            if (focusPercentText != null)
            {
                focusPercentText.text = Mathf.RoundToInt(focus).ToString();
            }
        }
    }
}
