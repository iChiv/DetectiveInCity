using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    public sealed class DetectiveDialogueRunner : MonoBehaviour
    {
        private readonly Queue<string> lineQueue = new();
        private readonly List<DetectiveDialogueDefinition.DialogueOption> visibleOptions = new();

        [SerializeField] private DetectiveDialogueUI dialogueUI;
        [SerializeField] private DetectiveVoiceCornerUI voiceCornerUI;
        private DetectiveDialogueDefinition currentDefinition;
        private int popupIndex;
        private bool pendingClose;
        private bool pendingFinalDuel;
        private bool repeatMode;

        public static DetectiveDialogueRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveDialogueRunner>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        public bool IsDialogueActive { get; private set; }

        private static DetectiveDialogueRunner instance;

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

            if (IsDialogueActive)
            {
                InteractionController.DialogueBlock = false;
            }
        }

        public void StartDialogue(DetectiveDialogueDefinition definition)
        {
            if (definition == null || IsDialogueActive || dialogueUI == null)
            {
                return;
            }

            currentDefinition = definition;
            IsDialogueActive = true;
            pendingClose = false;
            pendingFinalDuel = false;
            popupIndex = 0;
            repeatMode = definition.IsCompleted && definition.RepeatLines.Count > 0;
            lineQueue.Clear();

            foreach (string clueId in definition.GrantedClueIds)
            {
                DetectiveGameState.CollectClue(clueId);
            }

            IEnumerable<string> lines = repeatMode ? definition.RepeatLines : definition.OpeningLines;
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineQueue.Enqueue(line);
                }
            }

            InteractionController.DialogueBlock = true;
            dialogueUI.ContinueRequested += HandleContinueRequested;
            dialogueUI.OpenDialogue(definition.CharacterDefinition != null ? definition.CharacterDefinition.DisplayName : null);
            ShowNextContent();
        }

        public void EndDialogue()
        {
            if (!IsDialogueActive)
            {
                return;
            }

            if (currentDefinition != null
                && !string.IsNullOrWhiteSpace(currentDefinition.CompletionFlag)
                && (repeatMode || currentDefinition.Options.Count == 0))
            {
                DetectiveGameState.SetFlag(currentDefinition.CompletionFlag);
            }

            IsDialogueActive = false;
            currentDefinition = null;
            lineQueue.Clear();
            visibleOptions.Clear();
            pendingClose = false;
            repeatMode = false;
            dialogueUI.ContinueRequested -= HandleContinueRequested;
            dialogueUI.Hide();
            InteractionController.DialogueBlock = false;

            if (pendingFinalDuel)
            {
                pendingFinalDuel = false;
                DetectiveFinalDuelTrigger trigger = System.Array.Find(FindObjectsByType<DetectiveFinalDuelTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None), candidate => candidate.IsFinalEncounter);
                if (trigger != null)
                {
                    trigger.ForceStartDuel();
                }
            }
        }

        private void HandleContinueRequested()
        {
            if (!IsDialogueActive || dialogueUI.IsOptionsVisible)
            {
                return;
            }

            ShowNextContent();
        }

        private void ShowNextContent()
        {
            if (lineQueue.Count > 0)
            {
                dialogueUI.ShowLine(lineQueue.Dequeue());
                return;
            }

            if (repeatMode)
            {
                EndDialogue();
                return;
            }

            if (popupIndex < currentDefinition.VoicePopups.Count)
            {
                DetectiveDialogueDefinition.VoicePopup popup = currentDefinition.VoicePopups[popupIndex];
                popupIndex++;
                if (voiceCornerUI != null && popup.voice != DetectiveVoiceType.None)
                {
                    voiceCornerUI.ShowVoiceLine(popup.voice, popup.text);
                }

                dialogueUI.ShowPopupLine(popup.voice, popup.text);
                return;
            }

            if (pendingClose)
            {
                EndDialogue();
                return;
            }

            ShowOptions();
        }

        private void ShowOptions()
        {
            visibleOptions.Clear();
            foreach (DetectiveDialogueDefinition.DialogueOption option in currentDefinition.Options)
            {
                if (IsOptionVisible(option))
                {
                    visibleOptions.Add(option);
                }
            }

            if (visibleOptions.Count == 0)
            {
                EndDialogue();
                return;
            }

            dialogueUI.ShowOptions(visibleOptions, HandleOptionSelected);
        }

        private bool IsOptionVisible(DetectiveDialogueDefinition.DialogueOption option)
        {
            if (option.requireFlags != null)
            {
                foreach (string flag in option.requireFlags)
                {
                    if (!DetectiveGameState.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            if (option.forbiddenFlags != null)
            {
                foreach (string flag in option.forbiddenFlags)
                {
                    if (DetectiveGameState.HasFlag(flag))
                    {
                        return false;
                    }
                }
            }

            if (option.requireVoiceLevel != null
                && option.requireVoiceLevel.voice != DetectiveVoiceType.None
                && DetectiveGameState.GetVoice(option.requireVoiceLevel.voice) < option.requireVoiceLevel.minLevel)
            {
                return false;
            }

            int clueCount = DetectiveInvestigationState.CollectedClueCount;
            if (option.requireClueCountMin >= 0 && clueCount < option.requireClueCountMin)
            {
                return false;
            }

            if (option.requireClueCountMax >= 0 && clueCount > option.requireClueCountMax)
            {
                return false;
            }

            return true;
        }

        private void HandleOptionSelected(DetectiveDialogueDefinition.DialogueOption option)
        {
            if (option == null || !IsOptionVisible(option)) return;
            ApplyOptionEffects(option);
            pendingFinalDuel = option.startFinalDuel;

            if (option.isTerminal
                && currentDefinition != null
                && !string.IsNullOrWhiteSpace(currentDefinition.CompletionFlag))
            {
                DetectiveGameState.SetFlag(currentDefinition.CompletionFlag);
            }

            if (option.resultLines != null)
            {
                lineQueue.Clear();
                foreach (string line in option.resultLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lineQueue.Enqueue(line);
                    }
                }
            }

            pendingClose = option.isTerminal;

            if (lineQueue.Count > 0)
            {
                dialogueUI.OpenDialogue(currentDefinition.CharacterDefinition != null ? currentDefinition.CharacterDefinition.DisplayName : null);
                ShowNextContent();
            }
            else if (pendingClose)
            {
                EndDialogue();
            }
            else
            {
                ShowOptions();
            }
        }

        private void ApplyOptionEffects(DetectiveDialogueDefinition.DialogueOption option)
        {
            if (option.grantedClueIds != null)
            {
                foreach (string clueId in option.grantedClueIds)
                {
                    DetectiveGameState.CollectClue(clueId);
                }
            }

            if (option.voiceChanges != null)
            {
                foreach (DetectiveDialogueDefinition.VoiceChange change in option.voiceChanges)
                {
                    DetectiveGameState.AddVoice(change.voice, change.delta);
                }
            }

            if (option.timeAdvanceMinutes != 0)
            {
                DetectiveGameState.AdvanceTime(option.timeAdvanceMinutes);
            }

            if (option.timeAdvanceToMinutes >= 0)
            {
                int delta = Mathf.Max(0, option.timeAdvanceToMinutes - DetectiveGameState.TotalMinutes);
                if (delta > 0)
                {
                    DetectiveGameState.AdvanceTime(delta);
                }
            }

            if (option.focusDelta != 0f)
            {
                DetectiveGameState.AddFocus(option.focusDelta);
            }

            if (option.setFlags != null)
            {
                foreach (string flag in option.setFlags)
                {
                    DetectiveGameState.SetFlag(flag);
                }
            }
        }
    }
}
