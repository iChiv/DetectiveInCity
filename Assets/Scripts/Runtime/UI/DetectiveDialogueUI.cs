using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveDialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private RectTransform optionsContainer;
        [SerializeField] private GameObject optionButtonTemplate;

        public event Action ContinueRequested;

        public bool IsOptionsVisible { get; private set; }

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandlePanelClicked);
            }
        }

        public void OpenDialogue(string speakerName)
        {
            panelRoot.SetActive(true);
            var panel = speakerText.transform.parent;
            DetectiveGeneratedArt.Backdrop(panel, "ui_dialogue", .35f);
            string portrait = DetectiveGeneratedArt.Portrait(speakerName);
            var frame = DetectiveGeneratedArt.Framed(panelRoot.transform, "SpeakerPortrait", portrait, new Vector2(.035f,.30f), new Vector2(.17f,.58f));
            frame.transform.parent.gameObject.SetActive(portrait != null);

            IsOptionsVisible = false;
            speakerText.text = string.IsNullOrWhiteSpace(speakerName) ? "???" : speakerName;
            ClearOptions();
        }

        public void ShowLine(string text)
        {
            bodyText.text = text ?? string.Empty;
            hintText.enabled = true;
        }

        public void ShowPopupLine(DetectiveVoiceType voice, string text)
        {
            string voiceName = DetectiveVoiceStyle.GetName(voice);
            bodyText.text = string.IsNullOrEmpty(voiceName)
                ? text ?? string.Empty
                : $"<color={DetectiveVoiceStyle.GetHex(voice)}>【{voiceName}】</color>{text}";
            hintText.enabled = true;
        }

        public void ShowOptions(IReadOnlyList<DetectiveDialogueDefinition.DialogueOption> options, Action<DetectiveDialogueDefinition.DialogueOption> onSelected)
        {
            IsOptionsVisible = true;
            hintText.enabled = false;
            ClearOptions();

            for (int i = 0; i < options.Count; i++)
            {
                CreateOptionButton(options[i], onSelected);
            }
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
            IsOptionsVisible = false;
            ClearOptions();
        }

        private void HandlePanelClicked()
        {
            if (!IsOptionsVisible)
            {
                ContinueRequested?.Invoke();
            }
        }

        private void CreateOptionButton(DetectiveDialogueDefinition.DialogueOption option, Action<DetectiveDialogueDefinition.DialogueOption> onSelected)
        {
            var go = Instantiate(optionButtonTemplate, optionsContainer);
            go.SetActive(true);
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            string prefix = string.Empty;
            if (option.voiceTag != DetectiveVoiceType.None)
            {
                prefix = $"<color={DetectiveVoiceStyle.GetHex(option.voiceTag)}>【{DetectiveVoiceStyle.GetName(option.voiceTag)}】</color>";
            }

            label.text = prefix + (option.text ?? string.Empty);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onSelected?.Invoke(option));
        }

        private void ClearOptions()
        {
            for (int i = optionsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(optionsContainer.GetChild(i).gameObject);
            }
        }
    }
}
