using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Detective
{
    public sealed class DetectiveFadeUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image image;

        public static DetectiveFadeUI Instance { get; private set; }

        private Coroutine fadeRoutine;

        private void Awake()
        {
            Instance = this;
            SetBlack(false);
        }

        private void Start()
        {
            // 淡入淡出保持渲染在最顶层（Toast 100，Fade 101）
            var overrideCanvas = gameObject.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 101;
            transform.SetAsLastSibling();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetBlack(bool black)
        {
            if (group == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            group.alpha = black ? 1f : 0f;
            group.blocksRaycasts = black;
            group.interactable = black;
        }

        public IEnumerator FadeToBlack(float duration)
        {
            yield return Fade(1f, duration);
        }

        public IEnumerator FadeFromBlack(float duration)
        {
            yield return Fade(0f, duration);
        }

        private IEnumerator Fade(float target, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            float start = group.alpha;
            float elapsed = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            group.alpha = target;
            bool black = target >= 1f;
            group.blocksRaycasts = black;
            group.interactable = black;
            fadeRoutine = null;
        }
    }
}
