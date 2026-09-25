using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    [CreateAssetMenu(fileName = "ReasoningRecipe", menuName = "Detective/Content/Reasoning Recipe")]
    public sealed class DetectiveReasoningRecipe : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string[] requiredClueIds;
        [SerializeField, TextArea(2, 5)] private string resultText;
        [SerializeField] private string grantedClueId;
        [SerializeField] private DetectiveDialogueDefinition.VoiceChange[] voiceChanges;
        [SerializeField] private int failFocusCost = 10;
        [SerializeField] private bool onceOnly = true;
        [SerializeField] private bool freeSuccess;

        public string RecipeId => recipeId;
        public IReadOnlyList<string> RequiredClueIds => requiredClueIds ?? System.Array.Empty<string>();
        public string ResultText => resultText;
        public string GrantedClueId => grantedClueId;
        public IReadOnlyList<DetectiveDialogueDefinition.VoiceChange> VoiceChanges => voiceChanges ?? System.Array.Empty<DetectiveDialogueDefinition.VoiceChange>();
        public int FailFocusCost => failFocusCost;
        public bool OnceOnly => onceOnly;
        public bool FreeSuccess => freeSuccess;

        public string EffectiveId => string.IsNullOrWhiteSpace(recipeId) ? name : recipeId;

        public bool Matches(IReadOnlyList<string> clueIds)
        {
            if (clueIds == null || clueIds.Count != RequiredClueIds.Count)
            {
                return false;
            }

            var set = new HashSet<string>(RequiredClueIds);
            foreach (string clueId in clueIds)
            {
                if (!set.Contains(clueId))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Intersects(IReadOnlyList<string> clueIds)
        {
            if (clueIds == null)
            {
                return false;
            }

            foreach (string required in RequiredClueIds)
            {
                foreach (string clueId in clueIds)
                {
                    if (required == clueId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
