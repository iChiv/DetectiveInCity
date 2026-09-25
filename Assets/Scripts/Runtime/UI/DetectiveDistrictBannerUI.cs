using System.Collections;
using UnityEngine;
using TMPro;

namespace Detective
{
    public sealed class DetectiveDistrictBannerUI : MonoBehaviour
    {
        private const float DefaultDuration = 2.5f;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI label;

        private Coroutine fadeRoutine;
        private static DetectiveDistrictBannerUI instance;

        public static DetectiveDistrictBannerUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveDistrictBannerUI>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        private void Awake()
        {
            Instance = this;
            if (group != null)
            {
                group.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string districtName, float duration = DefaultDuration)
        {
            if (label == null || group == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            label.text = $"—— {districtName} ——";
            group.alpha = 1f;
            fadeRoutine = StartCoroutine(FadeOut(duration));
        }

        // 过场文案（如"你连夜穿过半个城市"）：保留当前横幅内容，delay 秒后切换并单独计时淡出。
        public void ShowLine(string text, float duration = DefaultDuration, float delay = DefaultDuration)
        {
            if (label == null || group == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(ShowLineRoutine(text ?? string.Empty, delay, duration));
        }

        private IEnumerator ShowLineRoutine(string text, float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            label.text = text;
            group.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            const float fade = 0.6f;
            elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(1f, 0f, elapsed / fade);
                yield return null;
            }

            group.alpha = 0f;
            fadeRoutine = null;
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return new WaitForSeconds(duration);
            float elapsed = 0f;
            const float fade = 0.6f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(1f, 0f, elapsed / fade);
                yield return null;
            }

            group.alpha = 0f;
            fadeRoutine = null;
        }
    }
}
