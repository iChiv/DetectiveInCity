using UnityEngine;

namespace Detective
{
    public interface IInteractable
    {
        string InteractionId { get; }
        string DisplayName { get; }
        string InteractionLabel { get; }
        Transform InteractionPoint { get; }
        float InteractionRange { get; }
        bool CanInteract { get; }
        void Interact(GameObject interactor);
    }
}
