using UnityEngine;
namespace Detective
{
    public sealed class DetectiveWaitingOpponent : MonoBehaviour,IInteractable
    {
        [SerializeField] private DetectiveFinalDuelTrigger trigger;
        [SerializeField] private GameObject visual;
        public string InteractionId => "final_waiting_opponent";
        public string DisplayName => "风衣男";
        public string InteractionLabel => "对峙";
        public Transform InteractionPoint => transform;
        public float InteractionRange => 2f;
        public bool CanInteract => visual != null && visual.activeSelf && !DetectiveGameState.HasFlag("final_duel_triggered");
        void Update()
        {
            if(trigger==null || visual==null)return;
            bool shown=trigger.CanStartAutomaticEncounter && DetectiveInvestigationState.CollectedClueCount>=10
                || DetectiveDuelRunner.Instance!=null && (DetectiveDuelRunner.Instance.IsConfirmationPending || DetectiveDuelRunner.Instance.IsDuelActive);
            if(visual.activeSelf!=shown)visual.SetActive(shown);
            var collider=GetComponent<Collider>();if(collider!=null)collider.enabled=shown;
        }
        public void Interact(GameObject interactor){if(CanInteract)trigger.ForceStartDuel();}
    }
}
