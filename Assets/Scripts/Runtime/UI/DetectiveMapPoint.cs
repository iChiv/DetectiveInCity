using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveMapPoint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string regionId;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image dot;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject currentRing;

        private DetectiveMapUI owner;

        private static readonly Color LockedDotColor = new Color(0.35f, 0.35f, 0.38f, 0.9f);
        private static readonly Color UnlockedDotColor = Color.white;
        private static readonly Color CurrentLabelColor = new Color32(0xD9, 0xA8, 0x4A, 0xFF);
        private static readonly Color NormalLabelColor = new Color(1f, 1f, 1f, 0.9f);

        public string RegionId => regionId;

        public void Bind(DetectiveMapUI owner)
        {
            this.owner = owner;
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => owner.HandlePointClicked(this));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandlePointHover(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandlePointHover(this, false);
        }

        public void SetState(bool unlocked, bool isNew, bool isCurrent)
        {
            string displayName = DetectiveRegionCatalog.TryGetRegion(regionId, out var region) ? region.DisplayName : regionId;
            if (!unlocked)
            {
                dot.color = LockedDotColor;
                dot.rectTransform.sizeDelta = new Vector2(16f, 16f);
                label.text = "???";
                label.color = new Color(1f, 1f, 1f, 0.4f);
            }
            else
            {
                dot.color = isCurrent ? CurrentLabelColor : UnlockedDotColor;
                dot.rectTransform.sizeDelta = isCurrent ? new Vector2(28f, 28f) : new Vector2(24f, 24f);
                label.text = isCurrent ? $"{displayName}（当前位置）" : displayName;
                label.color = isCurrent ? CurrentLabelColor : NormalLabelColor;
            }

            if (newBadge != null)
            {
                newBadge.SetActive(unlocked && isNew);
            }

            if (currentRing != null)
            {
                currentRing.SetActive(unlocked && isCurrent);
            }
        }

        public void SetHighlight(bool highlighted)
        {
            if (dot == null)
            {
                return;
            }

            dot.rectTransform.localScale = highlighted ? Vector3.one * 1.35f : Vector3.one;
        }
    }
}
