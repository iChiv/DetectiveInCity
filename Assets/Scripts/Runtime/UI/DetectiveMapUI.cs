using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveMapUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button maskButton;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private DetectiveMapPoint[] points;

        private DetectiveInputActions inputActions;

        public static DetectiveMapUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveMapUI>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        private static DetectiveMapUI instance;

        private void Awake()
        {
            Instance = this;
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (maskButton != null)
            {
                maskButton.onClick.AddListener(Close);
            }

            if (points != null)
            {
                foreach (var point in points)
                {
                    if (point != null)
                    {
                        point.Bind(this);
                    }
                }
            }

            windowRoot.SetActive(false);
        }

        private void OnEnable()
        {
            inputActions = new DetectiveInputActions();
            inputActions.Detective.Enable();
            DetectiveGameState.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            inputActions?.Detective.Disable();
            inputActions?.Dispose();
            inputActions = null;
            DetectiveGameState.OnTimeChanged -= HandleTimeChanged;
        }

        private void Update()
        {
            if (DetectiveInspectionUI.IsShowing || DetectiveInspectionUI.LastClosedFrame == Time.frameCount) return;
            if (inputActions == null)
            {
                return;
            }

            bool mapPressed = inputActions.Detective.Map.WasPressedThisFrame();
            bool cancelPressed = inputActions.Detective.Cancel.WasPressedThisFrame();

            if (IsOpen)
            {
                if (mapPressed || cancelPressed)
                {
                    Close();
                }

                return;
            }

            if (!mapPressed || IsOtherSystemActive())
            {
                return;
            }

            var mindPanel = DetectiveMindPanelUI.Instance;
            if (mindPanel != null && mindPanel.IsOpen)
            {
                mindPanel.ClosePanel();
            }

            Open();
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            windowRoot.SetActive(true);
            var city=windowRoot.transform.Find("CityMap") as RectTransform;
            if(city!=null){city.anchorMin=city.anchorMax=new Vector2(.43f,.5f);city.localScale=Vector3.one*.82f;}
            ShowDistrictPreview(DetectiveRegionLoader.Instance != null ? DetectiveRegionLoader.Instance.CurrentRegionId : "D0_Alley");
            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }

            InteractionController.DialogueBlock = true;
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            foreach (var region in DetectiveRegionCatalog.Regions)
            {
                if (DetectiveGameState.HasFlag($"loc_visited_{region.RegionId}"))
                {
                    DetectiveGameState.SetFlag($"loc_seen_{region.RegionId}");
                }
            }

            windowRoot.SetActive(false);
            if (hudRoot != null)
            {
                hudRoot.SetActive(true);
            }

            if (!IsOtherSystemActive())
            {
                InteractionController.DialogueBlock = false;
            }
        }

        public void HandlePointClicked(DetectiveMapPoint point)
        {
            if (point == null || !IsOpen)
            {
                return;
            }

            if (!DetectiveGameState.HasFlag($"loc_visited_{point.RegionId}"))
            {
                return;
            }

            string current = DetectiveRegionLoader.Instance != null ? DetectiveRegionLoader.Instance.CurrentRegionId : null;
            if (point.RegionId == current)
            {
                return;
            }

            string target = point.RegionId;
            Close();
            if (DetectiveRegionLoader.Instance != null)
            {
                DetectiveRegionLoader.Instance.LoadRegion(target);
            }
        }

        public void HandlePointHover(DetectiveMapPoint point, bool highlighted)
        {
            if (point != null)
            {
                point.SetHighlight(highlighted);
                if(highlighted && DetectiveGameState.HasFlag($"loc_visited_{point.RegionId}"))ShowDistrictPreview(point.RegionId);
            }
        }

        private void ShowDistrictPreview(string region)
        {
            var image=DetectiveGeneratedArt.Framed(windowRoot.transform,"DistrictPreview",DetectiveGeneratedArt.District(region),new Vector2(.76f,.31f),new Vector2(.98f,.60f));
            var caption=windowRoot.transform.Find("DistrictPreviewName")?.GetComponent<TMP_Text>();
            if(caption==null)caption=DetectiveUIWidgets.CreateText(windowRoot.transform,"DistrictPreviewName","",24,Color.white);
            caption.rectTransform.anchorMin=new Vector2(.76f,.24f);caption.rectTransform.anchorMax=new Vector2(.98f,.31f);caption.rectTransform.offsetMin=caption.rectTransform.offsetMax=Vector2.zero;caption.alignment=TextAlignmentOptions.Center;
            caption.text=DetectiveRegionCatalog.TryGetRegion(region,out var info)?info.DisplayName:"";
        }

        private void Refresh()
        {
            timeText.text = DetectiveGameState.TimeText;
            string current = DetectiveRegionLoader.Instance != null ? DetectiveRegionLoader.Instance.CurrentRegionId : null;
            if (points == null)
            {
                return;
            }

            foreach (var point in points)
            {
                if (point == null)
                {
                    continue;
                }

                bool unlocked = DetectiveGameState.HasFlag($"loc_visited_{point.RegionId}");
                bool isNew = unlocked && !DetectiveGameState.HasFlag($"loc_seen_{point.RegionId}");
                point.SetState(unlocked, isNew, point.RegionId == current);
            }
        }

        private void HandleTimeChanged(int totalMinutes)
        {
            if (IsOpen && timeText != null)
            {
                timeText.text = DetectiveGameState.TimeText;
            }
        }

        private static bool IsOtherSystemActive()
        {
            bool dialogueActive = DetectiveDialogueRunner.Instance != null && DetectiveDialogueRunner.Instance.IsDialogueActive;
            bool duelActive = DetectiveDuelRunner.Instance != null && (DetectiveDuelRunner.Instance.IsDuelActive || DetectiveDuelRunner.Instance.IsConfirmationPending);
            bool endingActive = DetectiveEndingUI.Instance != null && DetectiveEndingUI.Instance.IsShowing;
            bool loading = DetectiveRegionLoader.Instance != null && DetectiveRegionLoader.Instance.CurrentRegionId == null;
            return dialogueActive || duelActive || endingActive || loading;
        }
    }
}
