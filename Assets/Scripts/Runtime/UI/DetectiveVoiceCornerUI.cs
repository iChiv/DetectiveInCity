using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveVoiceCornerUI : MonoBehaviour
    {
        private const float IdleAlpha = 0.35f;
        private const float BubbleDuration = 10f;

        [SerializeField] private VoiceEntryRef[] entryRefs;

        private readonly VoiceEntry[] entries = new VoiceEntry[4];

        [Serializable]
        public sealed class VoiceEntryRef
        {
            public RectTransform root;
            public Image circle;
            public CanvasGroup bubbleGroup;
            public TextMeshProUGUI bubbleText;
            public Button bubbleButton;
        }

        private void OnEnable()
        {
            DetectiveGameState.OnVoiceChanged += RefreshLevels;
            RefreshLevels(DetectiveVoiceType.None, 0);
        }

        private void OnDisable()
        {
            DetectiveGameState.OnVoiceChanged -= RefreshLevels;
        }

        private void RefreshLevels(DetectiveVoiceType voice, int level)
        {
            for (int i = 0; i < entryRefs.Length; i++)
            {
                var circle = entryRefs[i].circle;
                if (circle == null) continue;
                string[] portraits={"voice_logic","voice_empathy","voice_authority","voice_madness"};
                if(DetectiveGeneratedArt.Load(portraits[i])!=null)
                {
                    var maskTransform=circle.transform.Find("PortraitMask");
                    if(maskTransform==null)
                    {
                        var maskImage=DetectiveUIWidgets.CreateImage(circle.transform,"PortraitMask",Color.white);
                        maskImage.sprite=DetectiveUIWidgets.GetCircleSprite();maskImage.raycastTarget=false;
                        DetectiveUIWidgets.Stretch(maskImage.rectTransform,2,2,2,2);
                        var mask=maskImage.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;
                        maskTransform=maskImage.transform;
                    }
                    DetectiveGeneratedArt.Image(maskTransform,"Portrait",portraits[i],Vector2.zero,Vector2.one,1,false);
                }

                var text = circle.transform.Find("Level")?.GetComponent<TextMeshProUGUI>();
                if (text == null)
                {
                    text = DetectiveUIWidgets.CreateText(circle.transform, "Level", "", 24, Color.white);
                    DetectiveUIWidgets.Stretch(text.rectTransform, 0, 0, 0, 0);
                    text.alignment = TextAlignmentOptions.Center;
                    text.fontStyle = FontStyles.Bold;
                }
                text.transform.SetAsLastSibling();
                text.rectTransform.anchorMin=new Vector2(.5f,0);text.rectTransform.anchorMax=new Vector2(1,0);
                text.rectTransform.pivot=new Vector2(.5f,0);text.rectTransform.anchoredPosition=new Vector2(0,-5);text.rectTransform.sizeDelta=new Vector2(16,35);
                text.outlineWidth=.3f;text.outlineColor=Color.black;
                text.text = DetectiveGameState.GetVoice((DetectiveVoiceType)(i + 1)).ToString();
                if (i == 0 && DetectiveGameState.HasFlag("status_calm")) text.text += "<size=14>\n冷静 +1</size>";
            }
        }

        private bool wasDuel;
        private Vector2[] originalPositions;
        private Vector2[] originalMin;
        private Vector2[] originalMax;
        private Vector2[] originalPivot;

        private void LateUpdate()
        {
            bool duel = DetectiveDuelRunner.Instance != null && DetectiveDuelRunner.Instance.IsDuelActive;
            if (duel == wasDuel) return;
            wasDuel = duel;
            if (duel) transform.SetAsLastSibling();
            for (int i = 0; i < entryRefs.Length; i++)
            {
                var r = entryRefs[i];
                if (r.root == null) continue;
                if (duel)
                {
                    r.root.anchorMin = r.root.anchorMax = new Vector2(0.125f + i * 0.25f, 1f);
                    r.root.pivot = new Vector2(0.5f, 1f);
                    r.root.anchoredPosition = new Vector2(0, -18f);
                    var bubble = (RectTransform)r.bubbleGroup.transform;
                    bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0f);
                    bubble.pivot = new Vector2(0.5f, 1f);
                    bubble.anchoredPosition = new Vector2(0,-4f);
                    bubble.sizeDelta = new Vector2(370, 160);
                }
                else
                {
                    r.root.anchorMin = originalMin[i]; r.root.anchorMax = originalMax[i];
                    r.root.pivot = originalPivot[i]; r.root.anchoredPosition = originalPositions[i];
                    var bubble = (RectTransform)r.bubbleGroup.transform;
                    bool left = originalMin[i].x < 0.5f;
                    bubble.anchorMin = bubble.anchorMax = new Vector2(left ? 0 : 1,1);
                    bubble.pivot = bubble.anchorMin;
                    bubble.anchoredPosition = new Vector2(left ? 76 : -76,12);
                    bubble.sizeDelta = new Vector2(340,160);
                    entries[i]?.HideBubble();
                }
            }
        }

        private void Awake()
        {
            originalPositions = new Vector2[entryRefs.Length];
            originalMin = new Vector2[entryRefs.Length]; originalMax = new Vector2[entryRefs.Length]; originalPivot = new Vector2[entryRefs.Length];
            for (int i = 0; i < entries.Length && i < entryRefs.Length; i++)
            {
                VoiceEntryRef refs = entryRefs[i];
                if (refs.root == null)
                {
                    continue;
                }

                originalPositions[i] = refs.root.anchoredPosition;
                originalMin[i] = refs.root.anchorMin; originalMax[i] = refs.root.anchorMax; originalPivot[i] = refs.root.pivot;
                refs.bubbleText.textWrappingMode = TextWrappingModes.Normal;
                refs.bubbleText.enableAutoSizing = true;
                refs.bubbleText.fontSizeMin = 17; refs.bubbleText.fontSizeMax = 22;
                refs.bubbleText.color = new Color(1f, 0.96f, 0.88f);
                var entry = new VoiceEntry(this, refs.root, refs.circle, refs.bubbleGroup, refs.bubbleText);
                entries[i] = entry;
                if (refs.bubbleButton != null)
                {
                    refs.bubbleButton.onClick.AddListener(entry.HideBubble);
                }
            }
        }

        public void ShowVoiceLine(DetectiveVoiceType voice, string text)
        {
            int index = (int)voice - 1;
            if (index < 0 || index >= entries.Length || entries[index] == null)
            {
                return;
            }

            entries[index].Show(text);
        }

        private sealed class VoiceEntry
        {
            private readonly DetectiveVoiceCornerUI owner;
            private readonly RectTransform rootRect;
            private readonly Image circle;
            private readonly CanvasGroup bubbleGroup;
            private readonly TextMeshProUGUI bubbleText;
            private readonly Color baseColor;
            private Coroutine showRoutine;

            public VoiceEntry(DetectiveVoiceCornerUI owner, RectTransform rootRect, Image circle, CanvasGroup bubbleGroup, TextMeshProUGUI bubbleText)
            {
                this.owner = owner;
                this.rootRect = rootRect;
                this.circle = circle;
                this.bubbleGroup = bubbleGroup;
                this.bubbleText = bubbleText;
                baseColor = circle.color;
            }

            public void Show(string text)
            {
                bubbleText.text = text ?? string.Empty;
                bubbleGroup.alpha = 1f;
                bubbleGroup.interactable = true;
                bubbleGroup.blocksRaycasts = true;
                rootRect.localScale = Vector3.one;
                Color highlight = baseColor;
                highlight.a = 1f;
                circle.color = highlight;

                if (showRoutine != null)
                {
                    owner.StopCoroutine(showRoutine);
                }

                showRoutine = owner.StartCoroutine(HideAfterDelay());
            }

            public void HideBubble()
            {
                if (showRoutine != null)
                {
                    owner.StopCoroutine(showRoutine);
                    showRoutine = null;
                }

                bubbleGroup.alpha = 0f;
                bubbleGroup.interactable = false;
                bubbleGroup.blocksRaycasts = false;
                rootRect.localScale = Vector3.one;
                circle.color = baseColor;
            }

            private IEnumerator HideAfterDelay()
            {
                yield return new WaitForSeconds(BubbleDuration);
                float elapsed = 0f;
                const float fade = 0.5f;
                while (elapsed < fade)
                {
                    elapsed += Time.deltaTime;
                    bubbleGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fade);
                    yield return null;
                }

                HideBubble();
            }
        }
    }
}
