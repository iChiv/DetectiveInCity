using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public sealed class DetectiveEndingUI : MonoBehaviour
    {
        private static readonly Dictionary<EndingType, string[]> BuiltinLines = new()
        {
            {
                EndingType.A_PerfectTruth,
                new[]
                {
                    "风衣男从怀里掏出一张照片，边角被雨泡得发皱。他把它拍在桌上。",
                    "“你查了我一整晚。但那天晚上，其实我也在查。”",
                    "照片上是警局高层和死者的合影，背景是一片集装箱堆场，拍摄时间在三个月前。",
                    "“死者撞见了他们的交易。然后他们动的手，嫁祸给你。”",
                    "你盯着照片看了很久。“你早就知道？”",
                    "“我猜到了。”",
                    "“猜到了你不说？”",
                    "风衣男没接话。他从口袋里摸出烟，叼在嘴里，没点着。雨敲着窗户。",
                    "“可是你连自己都不信，我说了有什么用。”",
                    "他把烟收起来，站起来，拉了拉风衣领子。“天快亮了。我们还有一小时。”",
                    "他推开门走了出去。外面雨小了一些。你跟上去，没再继续问。"
                }
            },
            {
                EndingType.D_YouAreKiller,
                new[]
                {
                    "你想起来了。一点一点想起来了。先是门牌号，然后是客厅那盏一闪一闪的灯，再然后是死者的脸——他站在你面前，背过身去。",
                    "你抓起桌上的刀，刺进了他的胸口。他对你毫无防备，所以没有任何挣扎的痕迹。",
                    "你慌了，夺门而逃。后来的事你都不记得了。",
                    "风衣男蹲在你面前，手按在你肩膀上。“你终于想起来了。”",
                    "你没看他。你看着地上的水洼。警徽不知道什么时候从手里滑出去了，躺在水里，编号那面朝上。",
                    "你的腿止不住地颤抖，跪在了地上。",
                    "他盯着你看了很久，轻声说：“……你记错了。可你连这个都不肯信了。”",
                    "然后转头离去。"
                }
            },
            {
                EndingType.B_Justice,
                new[]
                {
                    "你举起警徽：“现在你被捕了。”",
                    "风衣男没有反抗。你把他铐在暖气片上，拨通警局电话。",
                    "“NC-2077，请求支援。嫌疑人已控制。”",
                    "电话那头沉默很久：“……你真的确定吗？他可是你的人。”",
                    "“以前是。”",
                    "风衣男回头看你：“你终于做对了一次。”"
                }
            },
            {
                EndingType.C_StreetExecution,
                new[]
                {
                    "你把录音笔搁在桌上，按下播放键。风衣男的声音从里面传出来，混着电流杂音。他没看录音笔，一直看着你。",
                    "“你杀了人。你知道规矩的。”",
                    "他笑了一下。不是那种嘲讽的笑，就是嘴角动了动。“那你和我又有什么区别。”",
                    "你没回答。你走过去，抓住他胳膊，把他从椅子上拽起来。他没反抗，任由你拖着走。",
                    "天台的门锁是坏的，你不耐烦的一脚把它踹开。雨很大。你们站在天台边缘，谁都没说话。",
                    "“你放手吧。”他说。",
                    "你看着他。他闭上眼睛。你松了手。",
                    "没有喊叫，没有挣扎。雨吞掉了所有声音。",
                    "你站在边缘，手还悬在半空。雨慢慢小了。"
                }
            },
            {
                EndingType.E_Unsolved,
                new[]
                {
                    "“你还是什么都没想起来。唉，算了吧。”他走入雨里。你也没有拦住他。",
                    "天亮时，你站在十字路口，手里只有几张碎纸片。"
                }
            }
        };

        private static readonly Dictionary<EndingType, string> BuiltinFinalImages = new()
        {
            { EndingType.A_PerfectTruth, "你和风衣男一前一后走进雨里，天快亮了。" },
            { EndingType.D_YouAreKiller, "你跪在地上，警徽躺在水洼里，编号那面朝上。" },
            { EndingType.B_Justice, "警车驶入晨光，你站在雨中，手里攥着染血纸条。" },
            { EndingType.C_StreetExecution, "你站在天台边缘，手还悬在半空。雨慢慢小了。" },
            { EndingType.E_Unsolved, "你独自走在街上，雨停了，但衣服还是湿的。" }
        };

        private static readonly Color DefaultPanelTint = new Color(0.05f, 0.05f, 0.06f, 0.95f);

        private readonly Queue<string> lineQueue = new();

        [SerializeField] private GameObject root;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private TextMeshProUGUI lineText;
        [SerializeField] private TextMeshProUGUI finalText;
        [SerializeField] private CanvasGroup settlementGroup;
        [SerializeField] private TextMeshProUGUI settlementTitle;
        [SerializeField] private TextMeshProUGUI settlementStats;

        private DetectiveEndingDefinition current;
        private bool finalShown;
        private bool settlementShown;
        private Transform endingButtons;
        private Image settlementPanelImage;
        private static DetectiveEndingUI instance;

        public static DetectiveEndingUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DetectiveEndingUI>(FindObjectsInactive.Include);
                }

                return instance;
            }
            private set => instance = value;
        }

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (backgroundButton != null)
            {
                // Narrative advances only on mouse-down in Update, never on Button release.
                backgroundButton.onClick.RemoveListener(Advance);
            }

            if (root != null)
            {
                endingButtons = root.transform.Find("EndingButtons");
                if (endingButtons != null)
                {
                    BindEndingButton(endingButtons, "RestartButton", RestartInvestigation);
                    BindEndingButton(endingButtons, "EndingMenuButton", BackToTitle);
                    endingButtons.gameObject.SetActive(false);
                }
            }

            if (settlementGroup != null)
            {
                settlementPanelImage = settlementGroup.GetComponent<Image>();
            }

            root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayEnding(EndingType endingType)
        {
            if (DetectiveMindPanelUI.Instance != null && DetectiveMindPanelUI.Instance.IsOpen)
            {
                DetectiveMindPanelUI.Instance.ClosePanel();
            }

            current = LoadDefinition(endingType);
            lineQueue.Clear();
            foreach (string line in current.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineQueue.Enqueue(line);
                }
            }

            finalShown = false;
            DetectiveEndingTableau.Hide(root.transform);
            settlementShown = false;
            IsShowing = true;
            endingOpenedFrame = Time.frameCount;
            InteractionController.DialogueBlock = true;
            settlementGroup.alpha = 0f;
            settlementGroup.interactable = false;
            settlementGroup.blocksRaycasts = false;
            if (endingButtons != null)
            {
                endingButtons.gameObject.SetActive(false);
            }

            finalText.text = string.Empty;
            lineText.text = lineQueue.Count > 0 ? lineQueue.Dequeue() : string.Empty;
            // EndingPanel 在构建时是 inactive 的（UIRoot active=false），必须连同父链一起激活
            Transform activateChain = root.transform;
            while (activateChain != null)
            {
                if (!activateChain.gameObject.activeSelf)
                {
                    activateChain.gameObject.SetActive(true);
                }
                activateChain = activateChain.parent;
            }
            root.SetActive(true);
        }

        public static string GetEndingName(EndingType endingType)
        {
            return endingType switch
            {
                EndingType.A_PerfectTruth => "A · 完美真相",
                EndingType.D_YouAreKiller => "D · 你是凶手",
                EndingType.B_Justice => "B · 正义执行",
                EndingType.C_StreetExecution => "C · 都市规则",
                _ => "E · 悬案"
            };
        }

        private int endingOpenedFrame;

        // 单一输入入口：只接受打开结局之后的新一次左键按下。
        private void Update()
        {
            if (!IsShowing || Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && Time.frameCount > endingOpenedFrame)
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (!IsShowing || settlementShown)
            {
                return;
            }

            if (lineQueue.Count > 0)
            {
                lineText.text = lineQueue.Dequeue();
                return;
            }

            if (!finalShown)
            {
                finalShown = true;
                DetectiveEndingTableau.Show(root.transform, current.EndingType);
                lineText.text = string.Empty;
                finalText.text = $"【{current.FinalImageText}】";
                return;
            }

            ShowSettlement();
        }

        private void ShowSettlement()
        {
            settlementShown = true;
            DetectiveGeneratedArt.Backdrop(settlementGroup.transform,"ui_settlement",.3f);
            DetectiveEndingTableau.Hide(root.transform);
            settlementTitle.text = string.IsNullOrWhiteSpace(current.EndingTitle)
                ? GetEndingName(current.EndingType)
                : current.EndingTitle;
            finalText.text = string.Empty;
            if (settlementPanelImage != null)
            {
                Color tint = current.PanelTint;
                if (tint.a <= 0f)
                {
                    tint = DefaultPanelTint;
                }

                settlementPanelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.98f);
                settlementTitle.color = Color.Lerp(Color.white, tint, .45f);
            }

            int logic = DetectiveGameState.GetVoice(DetectiveVoiceType.Logic);
            int empathy = DetectiveGameState.GetVoice(DetectiveVoiceType.Empathy);
            int authority = DetectiveGameState.GetVoice(DetectiveVoiceType.Authority);
            int madness = DetectiveGameState.GetVoice(DetectiveVoiceType.Madness);
            settlementStats.text =
                $"收集线索：{DetectiveInvestigationState.CollectedClueCount}/{DetectiveEndingResolver.TotalClueCount}\n" +
                $"声音等级：逻辑 {logic} / 共情 {empathy} / 权威 {authority} / 疯狂 {madness}\n" +
                $"专注力：{Mathf.RoundToInt(DetectiveGameState.Focus)}%\n" +
                $"结局：{GetEndingName(current.EndingType)}";
            settlementGroup.alpha = 1f;
            settlementGroup.interactable = true;
            settlementGroup.blocksRaycasts = true;

            if (endingButtons != null)
            {
                endingButtons.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[DetectiveEndingUI] EndingPanel 下未找到 EndingButtons 按钮组（再次调查/回到标题不可用）。", this);
            }
        }

        private void RestartInvestigation()
        {
            DetectiveGameState.ResetAll();
            DetectiveTitleMenu.AutoStartOnce = true;
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        private static void BackToTitle()
        {
            DetectiveGameState.ResetAll();
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        private void BindEndingButton(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            Transform child = parent.Find(name);
            var button = child != null ? child.GetComponent<Button>() : null;
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
            else
            {
                Debug.LogWarning($"[DetectiveEndingUI] EndingButtons 下未找到 {name} 按钮。", this);
            }
        }

        private DetectiveEndingDefinition LoadDefinition(EndingType endingType)
        {
            foreach (DetectiveEndingDefinition definition in Resources.LoadAll<DetectiveEndingDefinition>("Detective/Endings"))
            {
                if (definition.EndingType == endingType)
                {
                    return definition;
                }
            }

            var fallback = ScriptableObject.CreateInstance<DetectiveEndingDefinition>();
            fallback.name = $"Ending_{endingType}_Fallback";
            var lines = BuiltinLines.TryGetValue(endingType, out string[] builtin) ? builtin : System.Array.Empty<string>();
            var finalImage = BuiltinFinalImages.TryGetValue(endingType, out string image) ? image : string.Empty;
            typeof(DetectiveEndingDefinition)
                .GetField("lines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(fallback, lines);
            typeof(DetectiveEndingDefinition)
                .GetField("finalImageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(fallback, finalImage);
            return fallback;
        }
    }
}
