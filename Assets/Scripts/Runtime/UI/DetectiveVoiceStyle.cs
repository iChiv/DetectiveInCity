using UnityEngine;

namespace Detective
{
    public static class DetectiveVoiceStyle
    {
        public static string GetName(DetectiveVoiceType voice)
        {
            return voice switch
            {
                DetectiveVoiceType.Logic => "逻辑",
                DetectiveVoiceType.Empathy => "共情",
                DetectiveVoiceType.Authority => "权威",
                DetectiveVoiceType.Madness => "疯狂",
                _ => string.Empty
            };
        }

        public static Color GetColor(DetectiveVoiceType voice)
        {
            return voice switch
            {
                DetectiveVoiceType.Logic => new Color32(0x4A, 0x90, 0xD9, 0xFF),
                DetectiveVoiceType.Empathy => new Color32(0x5D, 0xBB, 0x63, 0xFF),
                DetectiveVoiceType.Authority => new Color32(0xD9, 0xA8, 0x4A, 0xFF),
                DetectiveVoiceType.Madness => new Color32(0xC0, 0x39, 0x2B, 0xFF),
                _ => Color.white
            };
        }

        public static string GetHex(DetectiveVoiceType voice)
        {
            Color color = GetColor(voice);
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
    }
}
