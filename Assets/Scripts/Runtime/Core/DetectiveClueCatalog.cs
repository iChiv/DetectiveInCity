using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    public static class DetectiveClueCatalog
    {
        private static readonly Dictionary<string, string> titles = new();
        private static bool loaded;

        public static string GetTitle(string clueId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(clueId) && titles.TryGetValue(clueId, out string title)
                ? title
                : clueId;
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            foreach (DetectiveClueDefinition definition in Resources.LoadAll<DetectiveClueDefinition>("Detective/Clues"))
            {
                if (string.IsNullOrWhiteSpace(definition.ClueId))
                {
                    continue;
                }

                titles[definition.ClueId] = string.IsNullOrWhiteSpace(definition.Title)
                    ? definition.ClueId
                    : definition.Title;
            }
        }
    }
}
