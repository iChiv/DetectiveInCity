using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveMindPanelUI : MonoBehaviour
    {
        private const string RecipeDoneFlagPrefix = "recipe_done_";
        private const float JudgeDelay = 0.15f;
        private const float BannerDuration = 4f;
        private const int SlotCount = 2;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI focusText;
        [SerializeField] private RectTransform gridContent;
        [SerializeField] private RectTransform[] slotRects;
        [SerializeField] private Button reasonButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private Button bannerButton;
        [SerializeField] private GameObject cardTemplate;

        private readonly DetectiveMindClueCard[] slotCards = new DetectiveMindClueCard[SlotCount];
        private readonly List<DetectiveMindClueCard> gridCards = new();

        private DetectiveInputActions inputActions;
        private RectTransform dragLayer;
        private DetectiveMindClueCard draggedCard;
        private Vector2 dragOffset;
        private List<DetectiveReasoningRecipe> recipes;
        private Coroutine bannerRoutine;
        private Coroutine judgeRoutine;
        private Button cluesTabButton;
        private Button resultsTabButton;
        private GameObject cluesViewport;
        private GameObject resultsViewport;
        private TextMeshProUGUI resultsText;
        private GameObject gridLabel;
        private GameObject slotLabel;
        private bool showingResults;
        private static DetectiveMindPanelUI instance;

        public static DetectiveMindPanelUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveMindPanelUI>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            Instance = this;
            dragLayer = transform.Find("Window/DragLayer") as RectTransform;
            cluesTabButton = transform.Find("Window/CluesTab")?.GetComponent<Button>();
            resultsTabButton = transform.Find("Window/ResultsTab")?.GetComponent<Button>();
            cluesViewport = transform.Find("Window/Viewport")?.gameObject;
            resultsViewport = transform.Find("Window/ResultsViewport")?.gameObject;
            resultsText = transform.Find("Window/ResultsViewport/ResultsText")?.GetComponent<TextMeshProUGUI>();
            gridLabel = transform.Find("Window/GridLabel")?.gameObject;
            slotLabel = transform.Find("Window/SlotLabel")?.gameObject;
            if (cluesTabButton != null) cluesTabButton.onClick.AddListener(() => SelectTab(false));
            if (resultsTabButton != null) resultsTabButton.onClick.AddListener(() => SelectTab(true));
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (reasonButton != null)
            {
                reasonButton.onClick.AddListener(HandleReasonClicked);
            }

            if (clearButton != null)
            {
                clearButton.onClick.AddListener(ClearSlots);
            }

            if (bannerButton != null)
            {
                bannerButton.onClick.AddListener(HideBanner);
            }

            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (IsOpen)
            {
                InteractionController.DialogueBlock = false;
            }
        }

        private void OnEnable()
        {
            inputActions = new DetectiveInputActions();
            inputActions.Detective.Enable();
            DetectiveGameState.OnReasoningDiscovered += HandleReasoningDiscovered;
        }

        private void OnDisable()
        {
            DetectiveGameState.OnReasoningDiscovered -= HandleReasoningDiscovered;
            inputActions?.Detective.Disable();
            inputActions?.Dispose();
            inputActions = null;
        }

        private void Update()
        {
            if (DetectiveInspectionUI.IsShowing || DetectiveInspectionUI.LastClosedFrame == Time.frameCount) return;
            if (inputActions == null || !inputActions.Detective.MindPanel.WasPressedThisFrame())
            {
                return;
            }

            if (IsOpen)
            {
                ClosePanel();
            }
            else if (!IsOtherSystemActive())
            {
                Open();
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            panelRoot.SetActive(true);
            DetectiveGeneratedArt.Backdrop(panelRoot.transform, "ui_mind", .28f);
            ClearSlots();
            RebuildGrid();
            SelectTab(false);
            RefreshFocusText();
            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }

            InteractionController.DialogueBlock = true;
            if (!DetectiveGameState.HasFlag("tutorial_mind_opened"))
            {
                DetectiveGameState.SetFlag("tutorial_mind_opened");
                ShowBanner("单击线索查看全文；拖动卡片到右侧槽位，再点击推理。\n先试试把染血纸条单独放入槽位。此教学组合不消耗专注力。", Color.white);
            }
        }

        public void ShowClueDetails(string clueId)
        {
            var clue = Resources.Load<DetectiveClueDefinition>("Detective/Clues/" + clueId);
            if (clue == null) return;
            DetectiveInspectionUI.Get()?.Read(clue.Title, clue.Description + "\n\n来源：" + DetectiveClueSources.Get(clueId)
                + "\n\n关闭详情后，可以拖动这张卡片进入推理槽。槽中卡片可拖回列表，或点击清空。 ", DetectiveGeneratedArt.Evidence(clueId));
        }

        public void ClosePanel()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            panelRoot.SetActive(false);
            ClearSlots();
            DestroyGridCards();
            HideBanner();

            if (judgeRoutine != null)
            {
                StopCoroutine(judgeRoutine);
                judgeRoutine = null;
            }

            if (hudRoot != null)
            {
                hudRoot.SetActive(true);
            }

            if (!IsOtherSystemActive() && !(DetectiveDuelRunner.Instance != null && DetectiveDuelRunner.Instance.IsConfirmationPending))
            {
                InteractionController.DialogueBlock = false;
            }
        }

        private void SelectTab(bool results)
        {
            if (results && resultsViewport == null) return;
            if (results && !showingResults) ClearSlots();
            showingResults = results;
            if (cluesViewport != null) cluesViewport.SetActive(!results);
            if (resultsViewport != null) resultsViewport.SetActive(results);
            if (gridLabel != null) gridLabel.SetActive(!results);
            if (slotLabel != null) slotLabel.SetActive(!results);
            if (dragLayer != null) dragLayer.gameObject.SetActive(!results);
            if (reasonButton != null) reasonButton.gameObject.SetActive(!results);
            if (clearButton != null) clearButton.gameObject.SetActive(!results);
            foreach (RectTransform slot in slotRects)
            {
                if (slot != null) slot.gameObject.SetActive(!results);
            }
            SetTabColor(cluesTabButton, !results);
            SetTabColor(resultsTabButton, results);
            if (results) RefreshResultsText();
        }

        private static void SetTabColor(Button button, bool selected)
        {
            if (button != null && button.targetGraphic is Image image)
            {
                image.color = selected ? new Color(0.42f, 0.32f, 0.15f, 1f)
                    : new Color(0.16f, 0.18f, 0.23f, 1f);
            }
        }

        private void HandleReasoningDiscovered(string recipeId)
        {
            if (showingResults) RefreshResultsText();
        }

        private void RefreshResultsText()
        {
            if (resultsText == null) return;
            var lines = new List<string>();
            foreach (DetectiveReasoningRecipe recipe in GetRecipes())
            {
                if (!DetectiveGameState.HasReasoningResult(recipe.EffectiveId)) continue;
                var inputs = new List<string>();
                foreach (string clueId in recipe.RequiredClueIds)
                {
                    inputs.Add(DetectiveClueCatalog.GetTitle(clueId));
                }
                lines.Add($"<color=#E9C778>{string.Join(" ＋ ", inputs)}</color>\n{recipe.ResultText}");
            }
            resultsText.text = lines.Count == 0
                ? "尚无推理结果。把线索拖进右侧槽位并点击【推理】；成功的结论会保存在这里。"
                : string.Join("\n\n", lines);
        }

        public void BeginDragCard(DetectiveMindClueCard card, PointerEventData eventData)
        {
            if (draggedCard != null || card == null)
            {
                return;
            }

            draggedCard = card;
            card.transform.SetParent(dragLayer, true);
            var canvasGroup = card.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0.85f;
            canvasGroup.blocksRaycasts = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            dragOffset = card.RectTransform.anchoredPosition - local;
        }

        public void DragCard(DetectiveMindClueCard card, PointerEventData eventData)
        {
            if (card != draggedCard)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local);
            card.RectTransform.anchoredPosition = local + dragOffset;
        }

        public void EndDragCard(DetectiveMindClueCard card, PointerEventData eventData)
        {
            if (card != draggedCard)
            {
                return;
            }

            draggedCard = null;
            var canvasGroup = card.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            int slotIndex = FindSlotUnderPointer(eventData.position);
            if (slotIndex >= 0 && slotCards[slotIndex] == null)
            {
                PlaceCardInSlot(card, slotIndex);
            }
            else
            {
                ReturnCardToGrid(card);
            }
        }

        public void ReturnCardToGrid(DetectiveMindClueCard card)
        {
            if (card == null)
            {
                return;
            }

            if (card.InSlot && card.SlotIndex >= 0 && card.SlotIndex < SlotCount && slotCards[card.SlotIndex] == card)
            {
                slotCards[card.SlotIndex] = null;
            }

            card.InSlot = false;
            card.SlotIndex = -1;
            card.transform.SetParent(gridContent, false);
            card.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.RectTransform.anchoredPosition = Vector2.zero;
        }

        public void ClearSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotCards[i] != null)
                {
                    ReturnCardToGrid(slotCards[i]);
                }
            }
        }

        private void HandleReasonClicked()
        {
            if (judgeRoutine != null)
            {
                return;
            }

            var ids = new List<string>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotCards[i] != null)
                {
                    ids.Add(slotCards[i].ClueId);
                }
            }

            if (ids.Count == 0)
            {
                ShowBanner("先把线索卡片拖进右侧推理槽位。", new Color(1f, 1f, 1f, 0.8f));
                return;
            }

            judgeRoutine = StartCoroutine(JudgeAfterDelay());
        }

        private static bool IsOtherSystemActive()
        {
            bool dialogueActive = DetectiveDialogueRunner.Instance != null && DetectiveDialogueRunner.Instance.IsDialogueActive;
            bool duelActive = DetectiveDuelRunner.Instance != null && DetectiveDuelRunner.Instance.IsDuelActive;
            bool endingActive = DetectiveEndingUI.Instance != null && DetectiveEndingUI.Instance.IsShowing;
            return dialogueActive || duelActive || endingActive;
        }

        private void RebuildGrid()
        {
            DestroyGridCards();
            foreach (string clueId in DetectiveInvestigationState.CollectedClueIds)
            {
                var cardGo = Instantiate(cardTemplate, gridContent);
                cardGo.SetActive(true);
                cardGo.name = $"Clue_{clueId}";
                var card = cardGo.AddComponent<DetectiveMindClueCard>();
                card.Initialize(this, clueId, DetectiveClueCatalog.GetTitle(clueId));
                gridCards.Add(card);
            }
        }

        private void DestroyGridCards()
        {
            foreach (DetectiveMindClueCard card in gridCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            gridCards.Clear();
        }

        private void RefreshFocusText()
        {
            focusText.text = $"专注力：{Mathf.RoundToInt(DetectiveGameState.Focus)}";
        }

        private int FindSlotUnderPointer(Vector2 screenPoint)
        {
            for (int i = 0; i < SlotCount && i < slotRects.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slotRects[i], screenPoint, null))
                {
                    return i;
                }
            }

            return -1;
        }

        private void PlaceCardInSlot(DetectiveMindClueCard card, int slotIndex)
        {
            if (card.InSlot && card.SlotIndex >= 0 && card.SlotIndex < SlotCount && slotCards[card.SlotIndex] == card)
            {
                slotCards[card.SlotIndex] = null;
            }

            slotCards[slotIndex] = card;
            card.InSlot = true;
            card.SlotIndex = slotIndex;
            card.transform.SetParent(slotRects[slotIndex], false);
            RectTransform slotRect = slotRects[slotIndex];
            RectTransform cardRect = card.RectTransform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = slotRect.rect.size;
        }

        private IEnumerator JudgeAfterDelay()
        {
            yield return new WaitForSeconds(JudgeDelay);

            var ids = new List<string>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotCards[i] != null)
                {
                    ids.Add(slotCards[i].ClueId);
                }
            }

            if (ids.Count == 0)
            {
                judgeRoutine = null;
                yield break;
            }

            DetectiveReasoningRecipe recipe = FindMatchingRecipe(ids);
            if (recipe != null)
            {
                bool firstDiscovery = DetectiveGameState.RecordReasoningResult(recipe.EffectiveId);
                ShowBanner(recipe.ResultText, new Color32(0x5D, 0xBB, 0x63, 0xFF));
                if (!string.IsNullOrWhiteSpace(recipe.GrantedClueId))
                {
                    DetectiveGameState.CollectClue(recipe.GrantedClueId);
                }

                foreach (DetectiveDialogueDefinition.VoiceChange change in recipe.VoiceChanges)
                {
                    if (firstDiscovery) DetectiveGameState.AddVoice(change.voice, change.delta);
                }

                if (recipe.OnceOnly)
                {
                    DetectiveGameState.SetFlag(RecipeDoneFlagPrefix + recipe.EffectiveId);
                }
            }
            else
            {
                DetectiveReasoningRecipe candidate = FindCandidateRecipe(ids);
                int cost = candidate != null ? (candidate.FreeSuccess ? 0 : candidate.FailFocusCost) : 10;
                if (cost > 0)
                {
                    DetectiveGameState.AddFocus(-cost);
                    ShowBanner($"这些线索拼不出结论（专注力 -{cost}）", new Color32(0xC0, 0x39, 0x2B, 0xFF));
                }
                else
                {
                    ShowBanner("这些线索拼不出结论", new Color32(0xC0, 0x39, 0x2B, 0xFF));
                }
            }

            ClearSlots();
            RebuildGrid();
            RefreshFocusText();
            judgeRoutine = null;
        }

        private List<DetectiveReasoningRecipe> GetRecipes()
        {
            if (recipes == null)
            {
                recipes = new List<DetectiveReasoningRecipe>(Resources.LoadAll<DetectiveReasoningRecipe>("Detective/Recipes"));
            }

            return recipes;
        }

        private bool IsConsumed(DetectiveReasoningRecipe recipe)
        {
            return recipe.OnceOnly && DetectiveGameState.HasFlag(RecipeDoneFlagPrefix + recipe.EffectiveId);
        }

        private DetectiveReasoningRecipe FindMatchingRecipe(IReadOnlyList<string> clueIds)
        {
            foreach (DetectiveReasoningRecipe recipe in GetRecipes())
            {
                if (recipe.Matches(clueIds))
                {
                    return recipe;
                }
            }

            return null;
        }

        private DetectiveReasoningRecipe FindCandidateRecipe(IReadOnlyList<string> clueIds)
        {
            foreach (DetectiveReasoningRecipe recipe in GetRecipes())
            {
                if (!IsConsumed(recipe) && IsSubset(recipe, clueIds))
                {
                    return recipe;
                }
            }

            foreach (DetectiveReasoningRecipe recipe in GetRecipes())
            {
                if (!IsConsumed(recipe) && recipe.Intersects(clueIds))
                {
                    return recipe;
                }
            }

            return null;
        }

        private static bool IsSubset(DetectiveReasoningRecipe recipe, IReadOnlyList<string> clueIds)
        {
            foreach (string required in recipe.RequiredClueIds)
            {
                bool found = false;
                foreach (string clueId in clueIds)
                {
                    if (required == clueId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private void ShowBanner(string text, Color color)
        {
            bannerText.text = text ?? string.Empty;
            bannerText.textWrappingMode = TextWrappingModes.Normal;
            bannerText.enableAutoSizing = false;
            bannerText.fontSize = 26;
            var rect = (RectTransform)bannerGroup.transform;
            float available = ((RectTransform)panelRoot.transform).rect.width;
            float width = Mathf.Min(1200f, Mathf.Max(400f, available - 120f));
            float height = bannerText.GetPreferredValues(bannerText.text, width - 48f, Mathf.Infinity).y + 40f;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, Mathf.Max(150f,height));
            bannerText.color = color;
            bannerGroup.alpha = 1f;
            bannerGroup.interactable = true;
            bannerGroup.blocksRaycasts = true;

            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = null; // Keep the result visible until the player dismisses it.
        }

        private void HideBanner()
        {
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
                bannerRoutine = null;
            }

            if (bannerGroup != null)
            {
                bannerGroup.alpha = 0f;
                bannerGroup.interactable = false;
                bannerGroup.blocksRaycasts = false;
            }
        }

        private IEnumerator HideBannerAfterDelay()
        {
            yield return new WaitForSeconds(BannerDuration);
            float elapsed = 0f;
            const float fade = 0.4f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                bannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fade);
                yield return null;
            }

            HideBanner();
        }
    }
}
