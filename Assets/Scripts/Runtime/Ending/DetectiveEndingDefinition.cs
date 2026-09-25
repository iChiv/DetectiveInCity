using UnityEngine;

namespace Detective
{
    [CreateAssetMenu(fileName = "EndingDefinition", menuName = "Detective/Content/Ending Definition")]
    public sealed class DetectiveEndingDefinition : ScriptableObject
    {
        [SerializeField] private EndingType endingType;
        [SerializeField, TextArea(2, 6)] private string[] lines;
        [SerializeField, TextArea(1, 3)] private string finalImageText;
        [SerializeField] private string endingTitle;
        [SerializeField] private Color panelTint = new Color(0.05f, 0.05f, 0.06f, 0.95f);

        public EndingType EndingType => endingType;
        public string[] Lines => lines ?? System.Array.Empty<string>();
        public string FinalImageText => finalImageText;
        public string EndingTitle => endingTitle;
        public Color PanelTint => panelTint;
    }
}
