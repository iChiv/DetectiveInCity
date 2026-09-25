using TMPro;
using UnityEngine;

namespace Detective
{
    // A world marker with a fixed position above its target. The target mesh may be stretched,
    // so the text lives under the unscaled region root instead of inheriting that mesh scale.
    public sealed class DetectiveWorldLabel : MonoBehaviour
    {
        private TextMeshPro label;
        private TestInteractable interactable;
        private Renderer targetRenderer;
        private bool visible = true;

        public void Initialize(TestInteractable target)
        {
            interactable = target;
            targetRenderer = GetComponent<Renderer>();
            EnsureLabel();
            RefreshText();
        }

        public void SetVisible(bool shouldBeVisible)
        {
            visible = shouldBeVisible;
            EnsureLabel();
            label.gameObject.SetActive(visible && gameObject.activeInHierarchy);
        }

        public void SetCollected(bool collected)
        {
            EnsureLabel();
            label.text = collected ? $"{interactable.DisplayName}（已收集）" : interactable.DisplayName;
            UpdateBackingWidth();
        }

        private void OnEnable()
        {
            if (label != null)
            {
                label.gameObject.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (label != null)
            {
                Destroy(label.gameObject);
            }
        }

        private void LateUpdate()
        {
            if (label == null || !label.gameObject.activeSelf)
            {
                return;
            }

            float top = targetRenderer != null ? targetRenderer.bounds.max.y : transform.position.y + 1f;
            label.transform.position = new Vector3(transform.position.x, top + 0.35f, transform.position.z);
            if (Camera.main != null)
            {
                label.transform.rotation = Quaternion.LookRotation(
                    label.transform.position - Camera.main.transform.position, Vector3.up);
            }
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            var labelObject = new GameObject("DebugWorldLabel");
            labelObject.transform.SetParent(transform.root, false);
            labelObject.transform.localScale = Vector3.one * 0.3f;
            label = labelObject.AddComponent<TextMeshPro>();
            label.font = DetectiveUIWidgets.GetFont();
            label.fontSize = 6f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.93f, 0.72f);
            label.outlineWidth = 0.35f;
            label.outlineColor = Color.black;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(7f, 1.9f);

            labelObject.SetActive(visible && gameObject.activeInHierarchy);
        }

        private void UpdateBackingWidth()
        {
            if (label != null)
            {
                label.rectTransform.sizeDelta = new Vector2(7f, 1.9f);
            }
        }

        private void RefreshText()
        {
            if (label != null && interactable != null)
            {
                label.text = interactable.IsCollected ? $"{interactable.DisplayName}（已收集）" : interactable.DisplayName;
                UpdateBackingWidth();
            }
        }
    }
}
