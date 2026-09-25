using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    [CreateAssetMenu(fileName = "DuelDefinition", menuName = "Detective/Content/Duel Definition")]
    public sealed class DetectiveDuelDefinition : ScriptableObject
    {
        [SerializeField] private string duelId;
        [SerializeField] private string title;
        [SerializeField] private string opponentName;
        [SerializeField, TextArea(2, 4)] private string[] openingLines;
        [SerializeField] private VoicePopup[] voicePopups;
        [SerializeField] private DuelStatement[] statements;
        [SerializeField] private int defaultWrongFocusCost = 10;
        [SerializeField] private int maxMistakes = 3;
        [SerializeField] private string[] winClues;
        [SerializeField, TextArea(2, 4)] private string[] winResultLines;
        [SerializeField, TextArea(2, 4)] private string[] failResultLines;
        [SerializeField] private string[] onWinSetFlags;
        [SerializeField] private string[] onFailSetFlags;

        public string DuelId => duelId;
        public string Title => title;
        public string OpponentName => opponentName;
        public IReadOnlyList<string> OpeningLines => openingLines ?? System.Array.Empty<string>();
        public IReadOnlyList<VoicePopup> VoicePopups => voicePopups ?? System.Array.Empty<VoicePopup>();
        public IReadOnlyList<DuelStatement> Statements => statements ?? System.Array.Empty<DuelStatement>();
        public int DefaultWrongFocusCost => defaultWrongFocusCost;
        public int MaxMistakes => maxMistakes;
        public IReadOnlyList<string> WinClues => winClues ?? System.Array.Empty<string>();
        public IReadOnlyList<string> WinResultLines => winResultLines ?? System.Array.Empty<string>();
        public IReadOnlyList<string> FailResultLines => failResultLines ?? System.Array.Empty<string>();
        public IReadOnlyList<string> OnWinSetFlags => onWinSetFlags ?? System.Array.Empty<string>();
        public IReadOnlyList<string> OnFailSetFlags => onFailSetFlags ?? System.Array.Empty<string>();

        public string EffectiveId => string.IsNullOrWhiteSpace(duelId) ? name : duelId;

        [System.Serializable]
        public sealed class VoicePopup
        {
            public DetectiveVoiceType voice;
            [TextArea(2, 4)] public string text;
        }

        [System.Serializable]
        public sealed class DuelStatement
        {
            [TextArea(2, 4)] public string text;
            public string phaseLabel;
            public VoicePopup[] pressHints;
            public CardReaction[] reactions;
            [TextArea(2, 4)] public string skipReply;
        }

        [System.Serializable]
        public sealed class CardReaction
        {
            public string clueId;
            [TextArea(2, 4)] public string reply;
            public bool advance;
            public int focusDelta;
            public DetectiveDialogueDefinition.VoiceChange[] voiceChanges;
            public string setFlag;
            public DetectiveDialogueDefinition.VoiceRequirement[] requireVoices;
            public string[] requireClues;
            [TextArea(2, 4)] public string lockedReply;
        }
    }
}
