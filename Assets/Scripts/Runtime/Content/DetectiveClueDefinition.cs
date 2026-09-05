using UnityEngine;

namespace Detective
{
    [CreateAssetMenu(fileName = "ClueDefinition", menuName = "Detective/Content/Clue Definition")]
    public sealed class DetectiveClueDefinition : ScriptableObject
    {
        [SerializeField] private string clueId;
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 8)] private string description;
        [SerializeField] private string sourceId;
        [SerializeField] private string[] tags;

        public string ClueId => clueId;
        public string Title => title;
        public string Description => description;
        public string SourceId => sourceId;
        public string[] Tags => tags;
    }
}
