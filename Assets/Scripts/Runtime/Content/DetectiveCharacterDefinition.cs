using UnityEngine;

namespace Detective
{
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Detective/Content/Character Definition")]
    public sealed class DetectiveCharacterDefinition : ScriptableObject
    {
        [SerializeField] private string characterId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private string factionId;

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public string Description => description;
        public string FactionId => factionId;
    }
}
