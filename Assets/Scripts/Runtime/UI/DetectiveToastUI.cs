using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveToastUI : MonoBehaviour
    {
        private const float DefaultDuration = 2.5f;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI label;

        private Coroutine fadeRoutine;
        private static DetectiveToastUI instance;

        public static DetectiveToastUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveToastUI>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        private void Awake()
        {
            Instance = this;
            // Toast 渲染在所有菜单面板之上：嵌套 Canvas 覆盖排序（sibling 顺序不可靠）
            var overrideCanvas = gameObject.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 100;
            transform.SetAsLastSibling();
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

        public void Show(string message, float duration = DefaultDuration)
        {
            if (label == null || group == null)
            {
                Debug.Log($"[Toast] {message}");
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            label.text = message ?? string.Empty;
            group.alpha = 1f;
            fadeRoutine = StartCoroutine(FadeOut(duration));
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return new WaitForSeconds(duration);
            float elapsed = 0f;
            const float fade = 0.4f;
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
