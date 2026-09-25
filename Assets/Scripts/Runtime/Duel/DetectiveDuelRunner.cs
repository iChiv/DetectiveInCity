using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveDuelRunner : MonoBehaviour
    {
        private static readonly Color ReplyNormalColor = Color.white;
        private static readonly Color ReplyCorrectColor = new Color32(0x5D, 0xBB, 0x63, 0xFF);
        private static readonly Color ReplyWrongColor = new Color32(0xC0, 0x39, 0x2B, 0xFF);

        private readonly Queue<string> lineQueue = new();

        [SerializeField] private GameObject root;
        [SerializeField] private Button panelButton;
        [SerializeField] private Button retreatButton;
        [SerializeField] private Button pressButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI opponentText;
        [SerializeField] private TextMeshProUGUI thesisText;
        [SerializeField] private TextMeshProUGUI replyText;
        [SerializeField] private TextMeshProUGUI mistakesText;
        [SerializeField] private TextMeshProUGUI playedText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private RectTransform handContent;
        [SerializeField] private GameObject handCardTemplate;
        [SerializeField] private DetectiveVoiceCornerUI voiceCornerUI;

        private DetectiveDuelDefinition current;
        private int statementIndex;
        private int mistakes;
        private bool showingOpening;
        private bool showingResult;
        private static DetectiveDuelRunner instance;

        public static DetectiveDuelRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveDuelRunner>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        public bool IsDuelActive { get; private set; }
        public bool IsConfirmationPending { get; private set; }
        private System.Action confirmationAction;
        private System.Action cancelConfirmationAction;
        private CanvasGroup confirmationVisibility;

        public void RequestDuel(DetectiveDuelDefinition definition, System.Action onConfirmed, System.Action onCancelled = null)
        {
            if (definition == null || IsDuelActive || IsConfirmationPending) return;
            root.SetActive(true);
            var artPanel=root.transform.Find("Panel");
            if(artPanel!=null)DetectiveGeneratedArt.Backdrop(artPanel,"ui_dialogue",.3f);
            DetectiveGeneratedArt.Framed(root.transform,"OpponentArtwork","portrait_trenchcoat",new Vector2(.015f,.22f),new Vector2(.16f,.55f));
            DetectiveGeneratedArt.Framed(root.transform,"PlayerArtwork","portrait_hero",new Vector2(.84f,.22f),new Vector2(.985f,.55f));

            current = definition;
            confirmationAction = onConfirmed;
            cancelConfirmationAction = onCancelled;
            retreatButton.gameObject.SetActive(true);
            retreatButton.GetComponentInChildren<TMP_Text>().text = "暂不对峙";
            IsConfirmationPending = true;
            InteractionController.DialogueBlock = true;
            foreach (var mover in FindObjectsByType<DetectiveClickMover>(FindObjectsSortMode.None)) mover.Stop();
            titleText.text = "准备对峙";
            opponentText.text = definition.OpponentName;
            thesisText.enabled = false;
            replyText.color = ReplyNormalColor;
            replyText.text = "即将与" + definition.OpponentName + "对峙。请先检查收集到的线索。\n按 Tab 打开思维面板，确认准备好后开始。\n正式开始后无法撤退。";
            mistakesText.text = string.Empty;
            playedText.text = string.Empty;
            hintText.text = "Tab：检查线索    暂不对峙：继续调查    确认对峙：正式开始";
            handContent.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            pressButton.gameObject.SetActive(true);
            pressButton.GetComponentInChildren<TMP_Text>().text = "确认对峙";
            confirmationVisibility = root.GetComponent<CanvasGroup>();
            if (confirmationVisibility == null) confirmationVisibility = root.AddComponent<CanvasGroup>();
        }

        private void LateUpdate()
        {
            if (!IsConfirmationPending) return;
            bool browsing = DetectiveMindPanelUI.Instance != null && DetectiveMindPanelUI.Instance.IsOpen;
            confirmationVisibility.alpha = browsing ? 0f : 1f;
            confirmationVisibility.blocksRaycasts = !browsing;
            confirmationVisibility.interactable = !browsing;
        }

        public void ConfirmDuel()
        {
            if (!IsConfirmationPending || (DetectiveMindPanelUI.Instance != null && DetectiveMindPanelUI.Instance.IsOpen)) return;
            IsConfirmationPending = false;
            confirmationVisibility.alpha = 1f;
            confirmationVisibility.blocksRaycasts = true;
            confirmationVisibility.interactable = true;
            retreatButton.gameObject.SetActive(false);
            cancelConfirmationAction = null;
            var confirmed = confirmationAction;
            confirmationAction = null;
            handContent.gameObject.SetActive(true);
            pressButton.GetComponentInChildren<TMP_Text>().text = "追问";
            confirmed?.Invoke();
            StartDuel(current);
        }

        public void CancelConfirmation()
        {
            if (!IsConfirmationPending || (DetectiveMindPanelUI.Instance != null && DetectiveMindPanelUI.Instance.IsOpen)) return;
            IsConfirmationPending = false;
            var cancelled = cancelConfirmationAction;
            cancelConfirmationAction = null;
            confirmationAction = null;
            current = null;
            handContent.gameObject.SetActive(true);
            root.SetActive(false);
            InteractionController.DialogueBlock = false;
            cancelled?.Invoke();
        }

        private void Awake()
        {
            Instance = this;
            if (panelButton != null)
            {
                panelButton.onClick.AddListener(Advance);
            }

            if (retreatButton != null)
            {
                retreatButton.gameObject.SetActive(false);
                retreatButton.onClick.AddListener(CancelConfirmation);
            }

            pressButton = BindAuxButton(pressButton, "PressButton", PressStatement);
            skipButton = BindAuxButton(skipButton, "SkipButton", SkipStatement);

            // DuelPanel 在场景中初始关闭；首次激活会调用 Awake，不能在这里再次关闭。
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (IsDuelActive)
            {
                InteractionController.DialogueBlock = false;
            }
        }

        // 按名在 DuelPanel 下查找场景摆放的辅助按钮并绑定；找不到则隐藏对应功能。
        private Button BindAuxButton(Button button, string childName, UnityEngine.Events.UnityAction action)
        {
            if (button == null && root != null)
            {
                Transform child = root.transform.Find(childName);
                if (child != null)
                {
                    button = child.GetComponent<Button>();
                }
            }

            if (button != null)
            {
                button.onClick.AddListener(action);
            }
            else
            {
                Debug.LogWarning($"[DetectiveDuelRunner] DuelPanel 下未找到 {childName} 按钮，对应功能已隐藏。请在 Core 场景的 DuelPanel 下摆放并命名该按钮。", this);
            }

            return button;
        }

        public void StartDuel(DetectiveDuelDefinition definition)
        {
            if (definition == null || IsDuelActive)
            {
                return;
            }

            current = definition;
            IsDuelActive = true;
            statementIndex = 0;
            mistakes = 0;
            showingResult = false;
            lineQueue.Clear();

            foreach (string line in definition.OpeningLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineQueue.Enqueue(line);
                }
            }

            showingOpening = lineQueue.Count > 0;

            InteractionController.DialogueBlock = true;
            root.SetActive(true);
            var artPanel=root.transform.Find("Panel");
            if(artPanel!=null)DetectiveGeneratedArt.Backdrop(artPanel,"ui_dialogue",.3f);
            DetectiveGeneratedArt.Framed(root.transform,"OpponentArtwork","portrait_trenchcoat",new Vector2(.015f,.22f),new Vector2(.16f,.55f));
            DetectiveGeneratedArt.Framed(root.transform,"PlayerArtwork","portrait_hero",new Vector2(.84f,.22f),new Vector2(.985f,.55f));

            titleText.text = string.IsNullOrWhiteSpace(definition.Title) ? "言语对决" : definition.Title;
            opponentText.text = string.IsNullOrWhiteSpace(definition.OpponentName) ? "对手" : definition.OpponentName;
            thesisText.text = string.Empty;
            replyText.text = string.Empty;
            replyText.color = ReplyNormalColor;
            playedText.text = "证词：";
            RefreshMistakes();
            thesisText.enabled = !showingOpening;
            BuildHand();

            if (pressButton != null)
            {
                pressButton.gameObject.SetActive(!showingOpening);
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
            }

            if (showingOpening)
            {
                hintText.text = "（点击继续）";
                replyText.text = lineQueue.Dequeue();
            }
            else
            {
                EnterMainPhase();
            }
        }

        public void Advance()
        {
            if (!IsDuelActive)
            {
                return;
            }

            if (showingOpening)
            {
                if (lineQueue.Count > 0)
                {
                    replyText.text = lineQueue.Dequeue();
                }
                else
                {
                    EnterMainPhase();
                }

                return;
            }

            if (showingResult && lineQueue.Count > 0)
            {
                replyText.text = lineQueue.Dequeue();
                if (lineQueue.Count == 0)
                {
                    hintText.text = "（点击继续）";
                }
            }
            else if (showingResult)
            {
                FinishDuel();
            }
        }

        private void EnterMainPhase()
        {
            showingOpening = false;
            thesisText.enabled = true;
            hintText.text = "点击手牌出牌，或点击【追问】获取提示";

            if (pressButton != null)
            {
                pressButton.gameObject.SetActive(true);
            }

            if (voiceCornerUI != null)
            {
                foreach (DetectiveDuelDefinition.VoicePopup popup in current.VoicePopups)
                {
                    if (popup.voice != DetectiveVoiceType.None)
                    {
                        voiceCornerUI.ShowVoiceLine(popup.voice, popup.text);
                    }
                }
            }

            ShowCurrentStatement();
        }

        private DetectiveDuelDefinition.DuelStatement CurrentStatement =>
            statementIndex < current.Statements.Count ? current.Statements[statementIndex] : null;

        private void ShowCurrentStatement()
        {
            DetectiveDuelDefinition.DuelStatement statement = CurrentStatement;
            if (statement == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(statement.phaseLabel))
            {
                thesisText.text = $"“{statement.text}”";
            }
            else
            {
                thesisText.text = $"{statement.phaseLabel}\n“{statement.text}”";
            }

            RefreshPlayed();
            RefreshSkipButton();
        }

        private void PressStatement()
        {
            if (IsConfirmationPending) { ConfirmDuel(); return; }
            if (!IsDuelActive || showingOpening || showingResult)
            {
                return;
            }

            DetectiveDuelDefinition.DuelStatement statement = CurrentStatement;
            if (statement == null || statement.pressHints == null)
            {
                return;
            }

            if (voiceCornerUI != null)
            {
                foreach (DetectiveDuelDefinition.VoicePopup hint in statement.pressHints)
                {
                    if (hint.voice != DetectiveVoiceType.None)
                    {
                        voiceCornerUI.ShowVoiceLine(hint.voice, hint.text);
                    }
                }
            }
        }

        private void SkipStatement()
        {
            if (!IsDuelActive || showingOpening || showingResult)
            {
                return;
            }

            DetectiveDuelDefinition.DuelStatement statement = CurrentStatement;
            if (statement == null || string.IsNullOrWhiteSpace(statement.skipReply))
            {
                return;
            }

            DetectiveGameState.AddFocus(-10);
            replyText.color = ReplyWrongColor;
            replyText.text = $"{opponentText.text}：{statement.skipReply}（专注力 -10）";
            mistakes++;
            RefreshMistakes();

            if (mistakes >= current.MaxMistakes)
            {
                FailDuel();
                return;
            }

            statementIndex++;
            if (statementIndex >= current.Statements.Count)
            {
                WinDuel();
            }
            else
            {
                ShowCurrentStatement();
            }
        }

        private void RefreshSkipButton()
        {
            if (skipButton == null)
            {
                return;
            }

            DetectiveDuelDefinition.DuelStatement statement = CurrentStatement;
            bool canSkip = statement != null
                && !string.IsNullOrWhiteSpace(statement.skipReply)
                && !HasAnyAdvanceCard(statement);
            skipButton.gameObject.SetActive(canSkip);
        }

        private bool HasAnyAdvanceCard(DetectiveDuelDefinition.DuelStatement statement)
        {
            if (statement.reactions == null)
            {
                return false;
            }

            foreach (DetectiveDuelDefinition.CardReaction reaction in statement.reactions)
            {
                if (reaction.advance && DetectiveInvestigationState.IsCollected(reaction.clueId))
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayCard(string clueId)
        {
            if (!IsDuelActive || showingOpening || showingResult)
            {
                return;
            }

            DetectiveDuelDefinition.DuelStatement statement = CurrentStatement;
            if (statement == null)
            {
                return;
            }

            DetectiveDuelDefinition.CardReaction reaction = null;
            if (statement.reactions != null)
            {
                foreach (DetectiveDuelDefinition.CardReaction candidate in statement.reactions)
                {
                    if (candidate.clueId == clueId)
                    {
                        reaction = candidate;
                        break;
                    }
                }
            }

            if (reaction == null)
            {
                PlayGenericMiss(clueId);
                return;
            }

            if (!IsReactionUnlocked(reaction))
            {
                replyText.color = ReplyWrongColor;
                replyText.text = $"{opponentText.text}：{reaction.lockedReply}";
                return;
            }

            ApplyReaction(reaction);
        }

        private bool IsReactionUnlocked(DetectiveDuelDefinition.CardReaction reaction)
        {
            if (reaction.requireVoices != null)
            {
                foreach (DetectiveDialogueDefinition.VoiceRequirement requirement in reaction.requireVoices)
                {
                    if (requirement.voice != DetectiveVoiceType.None
                        && DetectiveGameState.GetVoice(requirement.voice) < requirement.minLevel)
                    {
                        return false;
                    }
                }
            }

            if (reaction.requireClues != null)
            {
                foreach (string requiredClueId in reaction.requireClues)
                {
                    if (!DetectiveInvestigationState.IsCollected(requiredClueId))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ApplyReaction(DetectiveDuelDefinition.CardReaction reaction)
        {
            if (reaction.voiceChanges != null)
            {
                foreach (DetectiveDialogueDefinition.VoiceChange change in reaction.voiceChanges)
                {
                    DetectiveGameState.AddVoice(change.voice, change.delta);
                }
            }

            if (reaction.focusDelta != 0)
            {
                DetectiveGameState.AddFocus(reaction.focusDelta);
            }

            bool decisive = reaction.advance || !string.IsNullOrWhiteSpace(reaction.setFlag);
            replyText.color = decisive ? ReplyCorrectColor : ReplyWrongColor;
            replyText.text = $"{opponentText.text}：{reaction.reply}";
            if (reaction.focusDelta < 0)
            {
                replyText.text += $"（专注力 {reaction.focusDelta}）";
            }

            if (!string.IsNullOrWhiteSpace(reaction.setFlag))
            {
                string endingFlag = reaction.setFlag;
                FinishDuel();
                DetectiveGameState.SetFlag(endingFlag);
                return;
            }

            if (reaction.advance)
            {
                statementIndex++;
                RefreshPlayed();
                if (statementIndex >= current.Statements.Count)
                {
                    WinDuel();
                }
                else
                {
                    ShowCurrentStatement();
                }

                return;
            }

            mistakes++;
            RefreshMistakes();
            if (mistakes >= current.MaxMistakes)
            {
                FailDuel();
            }
        }

        private void PlayGenericMiss(string clueId)
        {
            mistakes++;
            int cost = current.DefaultWrongFocusCost;
            DetectiveGameState.AddFocus(-cost);
            replyText.color = ReplyWrongColor;
            replyText.text = $"{opponentText.text}：这能说明什么？（专注力 -{cost}）";
            RefreshMistakes();

            if (mistakes >= current.MaxMistakes)
            {
                FailDuel();
            }
        }

        private void WinDuel()
        {
            foreach (string clueId in current.WinClues)
            {
                DetectiveGameState.CollectClue(clueId);
            }

            foreach (string flag in current.OnWinSetFlags)
            {
                DetectiveGameState.SetFlag(flag);
            }

            EnterResultPhase(current.WinResultLines, true);
        }

        private void FailDuel()
        {
            foreach (string flag in current.OnFailSetFlags)
            {
                DetectiveGameState.SetFlag(flag);
            }

            EnterResultPhase(current.FailResultLines, false);
        }

        private void EnterResultPhase(IReadOnlyList<string> lines, bool won)
        {
            showingResult = true;
            lineQueue.Clear();
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineQueue.Enqueue(line);
                }
            }

            if (pressButton != null)
            {
                pressButton.gameObject.SetActive(false);
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
            }

            replyText.color = won ? ReplyCorrectColor : ReplyWrongColor;
            if (lineQueue.Count > 0)
            {
                replyText.text = lineQueue.Dequeue();
                hintText.text = "（点击继续）";
            }
            else
            {
                FinishDuel();
            }
        }

        private void FinishDuel()
        {
            IsDuelActive = false;
            current = null;
            lineQueue.Clear();
            root.SetActive(false);

            bool dialogueActive = DetectiveDialogueRunner.Instance != null && DetectiveDialogueRunner.Instance.IsDialogueActive;
            if (!dialogueActive)
            {
                InteractionController.DialogueBlock = false;
            }
        }

        private void RefreshMistakes()
        {
            mistakesText.text = $"失误 {mistakes}/{current.MaxMistakes}";
        }

        private void RefreshPlayed()
        {
            int total = current.Statements.Count;
            int currentNumber = Mathf.Min(statementIndex + 1, total);
            playedText.text = total > 0 ? $"证词 {currentNumber}/{total}" : "证词：";
        }

        private void BuildHand()
        {
            for (int i = handContent.childCount - 1; i >= 0; i--)
            {
                Destroy(handContent.GetChild(i).gameObject);
            }

            foreach (string clueId in DetectiveInvestigationState.CollectedClueIds)
            {
                var go = Instantiate(handCardTemplate, handContent);
                go.SetActive(true);
                go.name = $"Hand_{clueId}";
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                label.text = DetectiveClueCatalog.GetTitle(clueId);
                label.textWrappingMode = TextWrappingModes.Normal;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16;
                label.fontSizeMax = 20;
                label.overflowMode = TextOverflowModes.Ellipsis;
                var button = go.GetComponent<Button>();
                string captured = clueId;
                button.onClick.AddListener(() => PlayCard(captured));
            }
        }
    }
}
