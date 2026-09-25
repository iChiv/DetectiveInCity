using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Detective
{
    public sealed class DetectiveMindClueCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        private DetectiveMindPanelUI panel;
        private RectTransform rectTransform;
        private bool wasDragged;

        public string ClueId { get; private set; }
        public bool InSlot { get; set; }
        public int SlotIndex { get; set; } = -1;

        public void Initialize(DetectiveMindPanelUI owner, string clueId, string title)
        {
            panel = owner;
            ClueId = clueId;
            rectTransform = (RectTransform)transform;
            var label = GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = title;
            }
        }

        public RectTransform RectTransform => rectTransform;
        public void OnPointerDown(PointerEventData eventData) { wasDragged = false; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            wasDragged = true;
            if (panel != null)
            {
                panel.BeginDragCard(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panel != null)
            {
                panel.DragCard(this, eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (panel != null)
            {
                panel.EndDragCard(this, eventData);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (wasDragged) { wasDragged = false; return; }
            if (panel != null && !eventData.dragging)
            {
                panel.ShowClueDetails(ClueId);
            }
        }
    }
}
