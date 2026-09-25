using UnityEngine;
using UnityEngine.UI;

namespace Detective
{
    public sealed class DetectiveTitleMenu : MonoBehaviour
    {
        public static DetectiveTitleMenu Instance { get; private set; }

        // “再次调查”用：为 true 时下一次 Start 跳过标题直接开局（消费一次）。
        public static bool AutoStartOnce;

        private GameObject titlePanel;
        private GameObject helpText;
        private GameObject gameHud;
        private GameObject voiceCorners;
        private bool gameBegun;

        public bool IsTitleVisible => titlePanel != null && titlePanel.activeSelf;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            GameObject canvas = GameObject.Find("UICanvas");
            titlePanel = canvas != null ? FindChild(canvas.transform, "TitlePanel") : null;
            if (titlePanel == null)
            {
                Debug.LogWarning("[DetectiveTitleMenu] UICanvas 下未找到 TitlePanel，跳过标题直接开局。", this);
                BeginGame();
                return;
            }

            helpText = FindChild(titlePanel.transform, "HelpText");
            var composition = titlePanel.GetComponent<DetectiveTitleComposition>();
            if (composition == null) composition = titlePanel.AddComponent<DetectiveTitleComposition>();
            composition.Apply();
            gameHud = canvas.transform.Find("GameHUD")?.gameObject;
            voiceCorners = canvas.transform.Find("VoiceCorners")?.gameObject;
            // 标题面板沉到 UICanvas 层级最底，游戏内弹层渲染时自然盖过它（替代嵌套 Canvas 覆盖排序）。
            titlePanel.transform.SetAsFirstSibling();
            BindButton(titlePanel.transform, "StartButton", BeginGame);
            BindButton(titlePanel.transform, "HelpButton", ToggleHelp);
            BindButton(titlePanel.transform, "QuitButton", QuitGame);

            if (AutoStartOnce)
            {
                AutoStartOnce = false;
                BeginGame();
                return;
            }

            InteractionController.DialogueBlock = true;
            titlePanel.SetActive(true);
            if (gameHud != null)
            {
                gameHud.SetActive(false);
            }
            if (voiceCorners != null)
            {
                voiceCorners.SetActive(false);
            }
        }

        public void BeginGame()
        {
            if (gameBegun)
            {
                return;
            }
            gameBegun = true;

            if (titlePanel != null)
            {
                titlePanel.SetActive(false);
            }
            if (gameHud != null)
            {
                gameHud.SetActive(true);
            }
            if (voiceCorners != null)
            {
                voiceCorners.SetActive(true);
            }

            InteractionController.DialogueBlock = false;
            if (DetectiveRegionLoader.Instance != null)
            {
                DetectiveRegionLoader.Instance.BeginGame();
            }
            else
            {
                Debug.LogWarning("[DetectiveTitleMenu] 场景中未找到 DetectiveRegionLoader，无法开局。", this);
            }
        }

        private void ToggleHelp()
        {
            if (helpText != null)
            {
                helpText.SetActive(!helpText.activeSelf);
            }
        }

        private static void QuitGame()
        {
            Application.Quit();
        }

        private void BindButton(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            Transform child = parent.Find(name);
            var button = child != null ? child.GetComponent<Button>() : null;
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
            else
            {
                Debug.LogWarning($"[DetectiveTitleMenu] TitlePanel 下未找到 {name} 按钮。", this);
            }
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.gameObject : null;
        }
    }
}
