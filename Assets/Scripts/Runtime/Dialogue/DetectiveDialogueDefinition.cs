using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "Detective/Content/Dialogue Definition")]
    public sealed class DetectiveDialogueDefinition : ScriptableObject
    {
        [SerializeField] private DetectiveCharacterDefinition characterDefinition;
        [SerializeField, TextArea(2, 4)] private string[] openingLines;
        [SerializeField] private VoicePopup[] voicePopups;
        [SerializeField] private DialogueOption[] options;
        [SerializeField] private string[] grantedClueIds;
        [SerializeField] private string completionFlag;
        [SerializeField, TextArea(1, 3)] private string[] repeatLines;

        public DetectiveCharacterDefinition CharacterDefinition => characterDefinition;
        public IReadOnlyList<string> OpeningLines => openingLines ?? System.Array.Empty<string>();
        public IReadOnlyList<VoicePopup> VoicePopups => voicePopups ?? System.Array.Empty<VoicePopup>();
        public IReadOnlyList<DialogueOption> Options => options ?? System.Array.Empty<DialogueOption>();
        public IReadOnlyList<string> GrantedClueIds => grantedClueIds ?? System.Array.Empty<string>();
        public string CompletionFlag => completionFlag;
        public IReadOnlyList<string> RepeatLines => repeatLines ?? System.Array.Empty<string>();

        public bool IsCompleted => !string.IsNullOrWhiteSpace(completionFlag) && DetectiveGameState.HasFlag(completionFlag);

        [System.Serializable]
        public sealed class VoicePopup
        {
            public DetectiveVoiceType voice;
            [TextArea(2, 4)] public string text;
        }

        [System.Serializable]
        public sealed class DialogueOption
        {
            [TextArea(1, 3)] public string text;
            public DetectiveVoiceType voiceTag;
            public string[] grantedClueIds;
            public VoiceChange[] voiceChanges;
            public int timeAdvanceMinutes;
            public int timeAdvanceToMinutes = -1;
            public float focusDelta;
            public string[] setFlags;
            public string[] requireFlags;
            public string[] forbiddenFlags;
            public VoiceRequirement requireVoiceLevel;
            public int requireClueCountMin = -1;
            public int requireClueCountMax = -1;
            public bool startFinalDuel;
            [TextArea(2, 4)] public string[] resultLines;
            public bool isTerminal = true;
        }

        [System.Serializable]
        public sealed class VoiceChange
        {
            public DetectiveVoiceType voice;
            public int delta;
        }

        [System.Serializable]
        public sealed class VoiceRequirement
        {
            public DetectiveVoiceType voice;
            public int minLevel;
        }
    }
}
