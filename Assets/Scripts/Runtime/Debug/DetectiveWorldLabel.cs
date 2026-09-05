using TMPro;
using UnityEngine;

namespace Detective
{
    public sealed class DetectiveWorldLabel : MonoBehaviour
    {
        private TextMeshPro label;
        private TestInteractable interactable;
        private bool visible = true;

        public void Initialize(TestInteractable target)
        {
            interactable = target;
            EnsureLabel();
            RefreshText();
        }

        public void SetVisible(bool shouldBeVisible)
        {
            visible = shouldBeVisible;
            EnsureLabel();
            label.gameObject.SetActive(visible);
        }

        public void SetCollected(bool collected)
        {
            EnsureLabel();
            label.text = collected ? $"{interactable.DisplayName} [Collected]" : interactable.DisplayName;
        }

        private void LateUpdate()
        {
            if (label == null || !label.gameObject.activeSelf || Camera.main == null)
            {
                return;
            }

            label.transform.rotation = Quaternion.LookRotation(label.transform.position - Camera.main.transform.position, Vector3.up);
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            GameObject labelObject = new GameObject("DebugWorldLabel");
            labelObject.transform.SetParent(transform, false);
            Renderer targetRenderer = GetComponent<Renderer>();
            float height = targetRenderer != null ? targetRenderer.bounds.size.y : 1f;
            labelObject.transform.localPosition = Vector3.up * (height + 1.4f);
            labelObject.transform.localScale = Vector3.one * 0.3f;

            label = labelObject.AddComponent<TextMeshPro>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 10f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.25f;
            label.outlineColor = Color.black;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.gameObject.SetActive(visible);
        }

        private void RefreshText()
        {
            if (label != null && interactable != null)
            {
                label.text = interactable.IsCollected ? $"{interactable.DisplayName} [Collected]" : interactable.DisplayName;
            }
        }
    }
}