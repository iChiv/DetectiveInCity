using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Detective
{
    public sealed class DetectivePauseMenu : MonoBehaviour
    {
        private DetectiveInputActions inputActions;
        private GameObject pausePanel;
        private GameObject helpText;
        private bool isOpen;

        private void Start()
        {
            GameObject canvas = GameObject.Find("UICanvas");
            Transform panelTransform = canvas != null ? canvas.transform.Find("PausePanel") : null;
            if (panelTransform == null)
            {
                Debug.LogWarning("[DetectivePauseMenu] UICanvas 下未找到 PausePanel，暂停菜单不可用。", this);
                enabled = false;
                return;
            }

            pausePanel = panelTransform.gameObject;
            // 标题/暂停面板沉到 UICanvas 层级最底：游戏内弹层（对话/对决/结局）渲染时自然盖过它们，
            // 无需嵌套 Canvas 覆盖排序（嵌套 Canvas 会破坏面板内按钮的事件系统射线）。
            pausePanel.transform.SetAsFirstSibling();
            helpText = FindChild(panelTransform, "HelpText");
            BindButton(panelTransform, "ResumeButton", Close);
            BindButton(panelTransform, "MenuButton", BackToTitle);
            BindButton(panelTransform, "HelpButton", ToggleHelp);
            pausePanel.SetActive(false);
        }

        private void OnEnable()
        {
            inputActions = new DetectiveInputActions();
            inputActions.Detective.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Detective.Disable();
            inputActions?.Dispose();
            inputActions = null;
        }

        private void Update()
        {
            if (DetectiveInspectionUI.IsShowing || DetectiveInspectionUI.LastClosedFrame == Time.frameCount) return;
            if (inputActions == null || pausePanel == null)
            {
                return;
            }

            if (!inputActions.Detective.Cancel.WasPressedThisFrame())
            {
                return;
            }

            if (isOpen)
            {
                Close();
            }
            else if (CanOpen())
            {
                Open();
            }
        }

        private bool CanOpen()
        {
            if (DetectiveTitleMenu.Instance != null && DetectiveTitleMenu.Instance.IsTitleVisible)
            {
                return false;
            }

            return !IsOtherSystemActive();
        }

        private void Open()
        {
            isOpen = true;
            pausePanel.SetActive(true);
            InteractionController.DialogueBlock = true;
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            pausePanel.SetActive(false);
            if (!IsOtherSystemActive())
            {
                InteractionController.DialogueBlock = false;
            }
        }

        private void ToggleHelp()
        {
            if (helpText != null)
            {
                helpText.SetActive(!helpText.activeSelf);
            }
        }

        private static void BackToTitle()
        {
            DetectiveGameState.ResetAll();
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        private static bool IsOtherSystemActive()
        {
            bool dialogueActive = DetectiveDialogueRunner.Instance != null && DetectiveDialogueRunner.Instance.IsDialogueActive;
            bool duelActive = DetectiveDuelRunner.Instance != null && (DetectiveDuelRunner.Instance.IsDuelActive || DetectiveDuelRunner.Instance.IsConfirmationPending);
            bool endingActive = DetectiveEndingUI.Instance != null && DetectiveEndingUI.Instance.IsShowing;
            bool loading = DetectiveRegionLoader.Instance != null && DetectiveRegionLoader.Instance.CurrentRegionId == null;
            return dialogueActive || duelActive || endingActive || loading;
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
                Debug.LogWarning($"[DetectivePauseMenu] PausePanel 下未找到 {name} 按钮。", this);
            }
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.gameObject : null;
        }
    }
}
