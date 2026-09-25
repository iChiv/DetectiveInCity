using System;
using System.Collections.Generic;

namespace Detective
{
    public static class DetectiveGameState
    {
        public const int StartMinutes = 22 * 60;
        public const int EndMinutes = 30 * 60;
        public const float MaxFocus = 100f;
        public const int InitialVoiceLevel = 1;

        private static readonly Dictionary<DetectiveVoiceType, int> voiceLevels = new();
        private static readonly HashSet<string> flags = new();
        private static readonly HashSet<string> reasoningResults = new();

        public static int TotalMinutes { get; private set; } = StartMinutes;
        public static float Focus { get; private set; } = MaxFocus;

        public static event Action<int> OnTimeChanged;
        public static event Action<float> OnFocusChanged;
        public static event Action<DetectiveVoiceType, int> OnVoiceChanged;
        public static event Action<string> OnClueCollected;
        public static event Action<string> OnFlagChanged;
        public static event Action<string> OnReasoningDiscovered;

        public static string TimeText
        {
            get
            {
                int minutes = TotalMinutes % (24 * 60);
                return $"{minutes / 60:00}:{minutes % 60:00}";
            }
        }

        public static void AdvanceTime(int minutes)
        {
            if (minutes == 0)
            {
                return;
            }

            TotalMinutes = Math.Clamp(TotalMinutes + minutes, StartMinutes, EndMinutes);
            OnTimeChanged?.Invoke(TotalMinutes);
        }

        public static void AddFocus(float delta)
        {
            if (delta == 0f)
            {
                return;
            }

            Focus = Math.Clamp(Focus + delta, 0f, MaxFocus);
            OnFocusChanged?.Invoke(Focus);
        }

        public static int GetVoice(DetectiveVoiceType voice)
        {
            if (voice == DetectiveVoiceType.None) return 0;
            int value = voiceLevels.TryGetValue(voice, out int level) ? level : InitialVoiceLevel;
            return value + (voice == DetectiveVoiceType.Logic && HasFlag("status_calm") ? 1 : 0);
        }

        public static void AddVoice(DetectiveVoiceType voice, int delta)
        {
            if (voice == DetectiveVoiceType.None || delta == 0)
            {
                return;
            }

            int current = voiceLevels.TryGetValue(voice, out int level) ? level : InitialVoiceLevel;
            int next = Math.Max(0, current + delta);
            voiceLevels[voice] = next;
            OnVoiceChanged?.Invoke(voice, GetVoice(voice));
        }

        public static void SetFlag(string flag)
        {
            if (!string.IsNullOrWhiteSpace(flag) && flags.Add(flag))
            {
                OnFlagChanged?.Invoke(flag);
                if (flag == "status_calm") OnVoiceChanged?.Invoke(DetectiveVoiceType.Logic, GetVoice(DetectiveVoiceType.Logic));
            }
        }

        public static void RemoveFlag(string flag)
        {
            if (!string.IsNullOrWhiteSpace(flag) && flags.Remove(flag))
            {
                OnFlagChanged?.Invoke(flag);
                if (flag == "status_calm") OnVoiceChanged?.Invoke(DetectiveVoiceType.Logic, GetVoice(DetectiveVoiceType.Logic));
            }
        }

        public static bool HasFlag(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) && flags.Contains(flag);
        }

        public static bool HasReasoningResult(string recipeId)
        {
            return !string.IsNullOrWhiteSpace(recipeId) && reasoningResults.Contains(recipeId);
        }

        public static bool RecordReasoningResult(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || !reasoningResults.Add(recipeId))
            {
                return false;
            }

            OnReasoningDiscovered?.Invoke(recipeId);
            return true;
        }

        public static bool CollectClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId))
            {
                return false;
            }

            if (!DetectiveInvestigationState.RegisterClue(clueId))
            {
                return false;
            }

            OnClueCollected?.Invoke(clueId);
            return true;
        }

        public static void ResetAll()
        {
            TotalMinutes = StartMinutes;
            Focus = MaxFocus;
            voiceLevels.Clear();
            flags.Clear();
            reasoningResults.Clear();
            DetectiveInvestigationState.Clear();

            OnTimeChanged?.Invoke(TotalMinutes);
            OnFocusChanged?.Invoke(Focus);
            foreach (DetectiveVoiceType voice in Enum.GetValues(typeof(DetectiveVoiceType)))
                if (voice != DetectiveVoiceType.None) OnVoiceChanged?.Invoke(voice, GetVoice(voice));
        }
    }
}
