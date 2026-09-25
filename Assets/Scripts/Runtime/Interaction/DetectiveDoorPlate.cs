using UnityEngine;

namespace Detective
{
    // 门牌/小告示牌：点击后弹 toast 的纯检视交互物（找公寓玩法用）。
    // 不依赖对话资产、不占用线索 ID，也不参与线索收集。
    [RequireComponent(typeof(Collider))]
    public sealed class DetectiveDoorPlate : MonoBehaviour, IInteractable
    {
        [SerializeField] private string plateLabel = "门牌";
        [SerializeField, TextArea] private string toastMessage = "";
        [SerializeField] private float interactionRange = 2.2f;

        public string InteractionId => name;
        public string DisplayName => plateLabel;
        public string InteractionLabel => "检视";
        public Transform InteractionPoint => transform;
        public float InteractionRange => Mathf.Max(0.5f, interactionRange);
        public bool CanInteract => enabled && gameObject.activeInHierarchy;

        public void Interact(GameObject interactor)
        {
            if (DetectiveToastUI.Instance != null && !string.IsNullOrWhiteSpace(toastMessage))
            {
                DetectiveToastUI.Instance.Show(toastMessage);
                return;
            }

            Debug.Log($"[DetectiveDoorPlate] {interactor.name} 检视了 {plateLabel}。", this);
        }
    }
}
