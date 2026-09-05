using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    public static class DetectiveInvestigationState
    {
        private static readonly HashSet<string> collectedClueIds = new();

        public static IReadOnlyCollection<string> CollectedClueIds => collectedClueIds;
        public static int CollectedClueCount => collectedClueIds.Count;

        public static bool RegisterClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId))
            {
                return false;
            }

            return collectedClueIds.Add(clueId);
        }

        public static bool IsCollected(string clueId)
        {
            return !string.IsNullOrWhiteSpace(clueId) && collectedClueIds.Contains(clueId);
        }

        public static void Clear()
        {
            collectedClueIds.Clear();
        }
    }
}