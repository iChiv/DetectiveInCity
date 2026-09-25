using UnityEngine;

namespace Detective.EditorTools
{
    internal static class DetectiveContentData
    {
        public readonly struct PopupEntry
        {
            public readonly DetectiveVoiceType Voice;
            public readonly string Text;

            public PopupEntry(DetectiveVoiceType voice, string text)
            {
                Voice = voice;
                Text = text;
            }
        }

        public readonly struct VoiceDeltaEntry
        {
            public readonly DetectiveVoiceType Voice;
            public readonly int Delta;

            public VoiceDeltaEntry(DetectiveVoiceType voice, int delta)
            {
                Voice = voice;
                Delta = delta;
            }
        }

        public sealed class OptionEntry
        {
            public string Text;
            public DetectiveVoiceType VoiceTag;
            public string[] GrantedClueIds;
            public VoiceDeltaEntry[] VoiceChanges;
            public int TimeAdvanceMinutes;
            public int TimeAdvanceToMinutes = -1;
            public string[] SetFlags;
            public string[] RequireFlags;
            public string[] ForbiddenFlags;
            public VoiceDeltaEntry RequireVoiceLevel;
            public int RequireClueCountMin = -1;
            public int RequireClueCountMax = -1;
            public int FocusDelta;
            public bool StartFinalDuel;
            public string[] ResultLines;
            public bool IsTerminal = true;
        }

        public sealed class DialogueEntry
        {
            public string FileName;
            public string CharacterId;
            public string[] GrantedClueIds;
            public string CompletionFlag;
            public string[] RepeatLines;
            public string[] OpeningLines;
            public PopupEntry[] Popups;
            public OptionEntry[] Options;
        }

        public readonly struct ClueEntry
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Description;

            public ClueEntry(string id, string title, string description)
            {
                Id = id;
                Title = title;
                Description = description;
            }
        }

        public sealed class RecipeEntry
        {
            public string FileName;
            public string RecipeId;
            public string[] RequiredClueIds;
            public string ResultText;
            public string GrantedClueId;
            public VoiceDeltaEntry[] VoiceChanges;
            public int FailFocusCost = 10;
            public bool OnceOnly = true;
            public bool FreeSuccess;
        }

        public readonly struct CardReactionEntry
        {
            public readonly string ClueId;
            public readonly string Reply;
            public readonly bool Advance;
            public readonly int FocusDelta;
            public readonly VoiceDeltaEntry[] VoiceChanges;
            public readonly string SetFlag;
            public readonly VoiceDeltaEntry[] RequireVoices;
            public readonly string[] RequireClues;
            public readonly string LockedReply;

            public CardReactionEntry(
                string clueId,
                string reply,
                bool advance = false,
                int focusDelta = 0,
                VoiceDeltaEntry[] voiceChanges = null,
                string setFlag = null,
                VoiceDeltaEntry[] requireVoices = null,
                string[] requireClues = null,
                string lockedReply = null)
            {
                ClueId = clueId;
                Reply = reply;
                Advance = advance;
                FocusDelta = focusDelta;
                VoiceChanges = voiceChanges;
                SetFlag = setFlag;
                RequireVoices = requireVoices;
                RequireClues = requireClues;
                LockedReply = lockedReply;
            }
        }

        public readonly struct DuelStatementEntry
        {
            public readonly string Text;
            public readonly string PhaseLabel;
            public readonly PopupEntry[] PressHints;
            public readonly CardReactionEntry[] Reactions;
            public readonly string SkipReply;

            public DuelStatementEntry(
                string text,
                string phaseLabel = null,
                PopupEntry[] pressHints = null,
                CardReactionEntry[] reactions = null,
                string skipReply = null)
            {
                Text = text;
                PhaseLabel = phaseLabel;
                PressHints = pressHints;
                Reactions = reactions;
                SkipReply = skipReply;
            }
        }

        public sealed class DuelEntry
        {
            public string FileName;
            public string DuelId;
            public string Title;
            public string OpponentName;
            public string[] OpeningLines;
            public PopupEntry[] VoicePopups;
            public DuelStatementEntry[] Statements;
            public int DefaultWrongFocusCost = 10;
            public int MaxMistakes = 3;
            public string[] WinClues;
            public string[] WinResultLines;
            public string[] FailResultLines;
            public string[] OnWinSetFlags;
            public string[] OnFailSetFlags;
        }

        public readonly struct CharacterEntry
        {
            public readonly string FileName;
            public readonly string CharacterId;
            public readonly string DisplayName;
            public readonly string Description;

            public CharacterEntry(string fileName, string characterId, string displayName, string description = "")
            {
                FileName = fileName;
                CharacterId = characterId;
                DisplayName = displayName;
                Description = description;
            }
        }

        public static readonly CharacterEntry[] Characters =
        {
            new("NPC_MysteriousWoman", "char_mysterious_woman", "神秘女子", "撑透明伞的风衣女人，似乎在等人。"),
            new("NPC_Bartender", "char_bartender", "酒保", "右眼是机械义眼的秃头老酒保。"),
            new("NPC_Caller", "char_caller", "陌生声音", "电话亭里的匿名警告者。"),
            new("NPC_Guard", "char_guard", "保安", "年轻紧张的公寓保安。"),
            new("NPC_Beggar", "char_beggar", "乞丐", "裹着毯子的老妇人。"),
            new("NPC_Barista", "char_barista", "咖啡师", "认识你的年轻女性。"),
            new("NPC_Monkey", "char_monkey", "瘦猴", "瘾君子线人。"),
            new("NPC_Sniper", "char_sniper", "狙击手", "只露出轮廓的神秘人。"),
            new("NPC_Wife", "char_wife", "死者妻子", "哭泣的妻子。"),
            new("NPC_Trenchcoat", "char_trenchcoat", "风衣男", "你的前搭档。"),
            new("NPC_Narrator", "char_narrator", "检视", ""),
        };

        public static readonly ClueEntry[] Clues =
        {
            new(DetectiveClueIds.Badge, "警徽：编号NC-2077", "你捡起一枚沾着雨水的警徽。编号：NC-2077。"),
            new(DetectiveClueIds.BloodyNote, "染血纸条：“今晚23:00，老地方”", "一张被雨水泡皱的纸条，字迹模糊但可辨认：“今晚23:00，老地方。”"),
            new(DetectiveClueIds.MysteriousWomanTestimony, "神秘女子证词", "“你当时不在酒吧——23:10 左右，一个穿风衣的男人和另一个人在街口吵得可凶了。其中一个……是你。”"),
            new(DetectiveClueIds.MyAnxiety, "我的焦虑", "“你昨晚很焦虑，一直在看手表。你以前从不这样。”"),
            new(DetectiveClueIds.SheKnowsMe, "她认识我", "神秘女子与你并非素不相识。"),
            new(DetectiveClueIds.BarTimeline, "酒吧时间线", "“23:30 左右。你喝了一杯，接了个电话就走了，脸色很难看。”"),
            new(DetectiveClueIds.TrenchcoatMan, "风衣男", "他和一个穿风衣的男人来过，两人吵得很凶，风衣男还用酒瓶重重拍了桌子。"),
            new(DetectiveClueIds.AnonymousThreat, "匿名威胁", "“我给你最后一次警告。天亮之前收手，不然你连后悔都来不及。”"),
            new(DetectiveClueIds.DoorRecord, "门禁：22:45有人进入", "22:45 有人进入，刷卡记录显示是你的警徽编号。"),
            new(DetectiveClueIds.BulletHole9mm, "弹孔：口径9mm", "9mm 口径弹孔，嵌在墙里。"),
            new(DetectiveClueIds.CorpseChestWound, "尸体：胸口致命伤，无挣扎", "死者胸口一刀毙命，无挣扎痕迹。你认识他。"),
            new(DetectiveClueIds.MemoryBlank, "记忆空白", "“我真的不记得了……”"),
            new(DetectiveClueIds.SurveillanceBlindspot, "监控盲区", "公寓走廊区域监控被删。"),
            new(DetectiveClueIds.VictimNoPain, "死者无痛苦", "死者死前并未经历太多痛苦。"),
            new(DetectiveClueIds.TakeoutBox, "餐盒：外卖，未送达", "外卖未送达，收据上写着死者地址。"),
            new(DetectiveClueIds.TornPhoto, "碎照片：你和死者的合影", "一张你和死者的合影，被撕碎后又拼好。"),
            new(DetectiveClueIds.SleepingPills, "安眠药", "床头柜上一瓶空的安眠药。"),
            new(DetectiveClueIds.TrenchcoatItem, "风衣", "一件风衣，和神秘女子描述的一致。"),
            new(DetectiveClueIds.DiaryThreatened, "日记：死者被威胁", "他说他会帮我。但我发现他根本就是在骗我！今晚我要和他摊牌。如果我没回来，就去找红灯区的那个女人，她知道一切。\n\n页边另记着保险箱密码：2077。"),
            new(DetectiveClueIds.SafeCode2077, "保险箱密码：2077", "日记中提到的保险箱密码。"),
            new(DetectiveClueIds.Recording, "录音：风衣男威胁", "“你答应过我不碰她的！”“但她已经知道了，我不能留。”"),
            new(DetectiveClueIds.BribedGuard, "保安被收买", "“有人给了我钱……让我删掉 23:00–23:30 的录像。”"),
            new(DetectiveClueIds.DeletedFootage, "被删录像：23:00–23:30", "被删时段正好覆盖案发窗口。"),
            new(DetectiveClueIds.TrenchcoatCame, "风衣男来过", "“是一个穿风衣的男人！他给了我一万块！”"),
            new(DetectiveClueIds.TrenchcoatKnowsYou, "风衣男认识你", "“那个人……他说他认识你。”"),
            new(DetectiveClueIds.FingerprintEvidence, "物证：指纹", "封锁现场后采集到的指纹物证。"),
            new(DetectiveClueIds.FleeingIndustrial, "风衣男跑向工业区", "昨晚 23:00，风衣男从公寓跑出来，手上沾着血，往旧工业区跑了。"),
            new(DetectiveClueIds.NapkinNotes, "餐巾纸笔记", "你点了双倍浓缩黑咖啡，一直在一张餐巾纸上写东西。"),
            new(DetectiveClueIds.SelfDoubt, "自我怀疑", "你一直在自言自语：“不是他，是我。”"),
            new(DetectiveClueIds.NapkinKiller, "餐巾纸：凶手是……", "一张写着“凶手是……”的餐巾纸，后面被撕掉。"),
            new(DetectiveClueIds.CoatManConfessed, "风衣男承认在场", "言语对决胜利：风衣男承认昨晚来过公寓。"),
            new(DetectiveClueIds.DroppedKey, "掉落的钥匙", "风衣男逃跑时留下的钥匙。可带到工业区铁门核对，或在思维面板与铁门线索组合。"),
            new(DetectiveClueIds.GateForced, "铁门：被撬开", "铁门已经被撬开，锁孔有新划痕。钥匙是否与这里有关，还需要核对。"),
            new(DetectiveClueIds.TireTracks, "车辙：摩托车", "摩托车车辙，是往仓库方向的。"),
            new(DetectiveClueIds.GraffitiTraitor, "涂鸦：“NC-2077是叛徒”", "红色喷漆写着“NC-2077 是叛徒”。"),
            new(DetectiveClueIds.TrenchcoatImpersonates, "风衣男冒充你", "“被风衣男截了。”——那批货原本属于你。"),
            new(DetectiveClueIds.TrenchcoatToResidential, "风衣男去住宅区", "昨晚风衣男拿了一批货往住宅区去了。"),
            new(DetectiveClueIds.InfoTrade, "你曾用情报交易", "“5000。但你上次说过可以用情报抵。”"),
            new(DetectiveClueIds.TrenchcoatFile, "风衣男档案：前警探", "前警探，曾在三年前被开除。"),
            new(DetectiveClueIds.YourFile, "你的档案：卧底失败", "NC-2077，三年前卧底任务失败。"),
            new(DetectiveClueIds.BloodTrail, "血迹：通向出口", "通向出口的血迹，但已经干了。"),
            new(DetectiveClueIds.LockpickTool, "工具：撬锁器", "工具箱里的一把撬锁器。"),
            new(DetectiveClueIds.FormerPartners, "风衣男是你的前搭档", "警服合影背面写着：“警局双子星。”"),
            new(DetectiveClueIds.WasUndercover, "你曾是卧底", "警局高层邮件：卧底任务立即终止。"),
            new(DetectiveClueIds.MissionFailed, "任务失败", "三年前的卧底任务以失败告终。"),
            new(DetectiveClueIds.HigherUpsInvolved, "警局高层介入", "“是你以前的上级。他说你查太深了。”"),
            new(DetectiveClueIds.YourPast, "你的过去", "NC-2077，三年前卧底失败、被开除的警员。但你还在查案。"),
            new(DetectiveClueIds.CatCollar, "猫：项圈写着NC-2077", "流浪猫脖子上的项圈写着 NC-2077。"),
            new(DetectiveClueIds.LetterWarning, "你的信：警告", "收件人：我。\n\n如果你读到这封信，说明你又忘了。现在一定要听好：风衣男确实是你的搭档，但他背叛了你。死者是唯一知道真相的人。你今晚必须找到证据。不然，下一个死的就是你。\n\n信的来源与指控仍需核实。"),
            new(DetectiveClueIds.StreetlampScratches, "路灯：七道刻痕", "灯柱上的正字刻痕，画到第七道。你不记得自己刻过它。"),
            new(DetectiveClueIds.MirrorBruise, "镜子：你脸上有伤", "左颧骨有淤青，像是被人一拳打的。你不记得这伤是怎么来的。"),
            new(DetectiveClueIds.SleepingPillsEmpty, "安眠药：已空", "空的安眠药瓶，和死者家的一样。"),
            new(DetectiveClueIds.DismissalNotice, "警局开除通知", "“NC-2077，因三年前卧底任务失败，现予以开除处理。”"),
            new(DetectiveClueIds.YourDiaryDoubt, "你的日记：怀疑搭档", "我可能真的不能再查了。他们已经发现了。但如果我不查，他就白死了。风衣男……我最好的搭档。为什么真的是你？\n\n这是你曾经的怀疑，不等于已经证实的结论。"),
            new(DetectiveClueIds.VictimThoughtYouMad, "死者认为你疯了", "“他说你疯了。他说你要毁了一切。”"),
            new(DetectiveClueIds.LastMeetingTrenchcoat, "风衣男最后见面", "“是一个穿风衣的男人。他们吵得很凶。”"),
            new(DetectiveClueIds.KeptNotes, "他一直留着笔记", "他一直在写东西，都收在他的遗物里。"),
            new(DetectiveClueIds.VictimNotes, "死者笔记：风衣男和警局高层有交易", "遗物中的笔记：风衣男和警局高层有交易。"),
        };

        public static readonly DialogueEntry[] Dialogues =
        {
            new()
            {
                FileName = "dlg_mysterious_woman",
                CharacterId = "char_mysterious_woman",
                CompletionFlag = "dlg_mysterious_woman_done",
                RepeatLines = new[] { "（女子已经走开了。）" },
                OpeningLines = new[] { "你果然还活着，呵呵。看来你运气还是一如既往的好呢。" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "她用了“果然”——她知道你会来。先问清具体情况。"),
                    new PopupEntry(DetectiveVoiceType.Empathy, "她的声音在抖，情绪不太稳定。先安抚她吧。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“你确定我昨晚真的来过？几点呢？”",
                        VoiceTag = DetectiveVoiceType.Logic,
                        GrantedClueIds = new[] { DetectiveClueIds.MysteriousWomanTestimony },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        ResultLines = new[] { "“你当时不在酒吧——23:10 左右，我看见一个穿风衣的男人和另一个人在街口吵得可凶了。其中一个……是你。”获得线索【神秘女子证词】。逻辑+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“不好意思女士，我是不是吓到你了？”",
                        VoiceTag = DetectiveVoiceType.Empathy,
                        GrantedClueIds = new[] { DetectiveClueIds.MyAnxiety, DetectiveClueIds.SheKnowsMe },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 1) },
                        ResultLines = new[] { "“你昨晚很焦虑，一直在看手表。你以前从不这样。”获得线索【我的焦虑】。", "额外获得【她认识我】。共情+1。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_bartender",
                CharacterId = "char_bartender",
                CompletionFlag = "dlg_bartender_done",
                RepeatLines = new[] { "“警官，今晚已经喝过了吧？”" },
                OpeningLines = new[] { "“警官，好久不见了啊哈哈哈哈。还是老样子？”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "他一看就认识你。让他别装傻，套出昨晚的时间线。"),
                    new PopupEntry(DetectiveVoiceType.Authority, "虽然是熟人，但要让他配合，直接亮警徽最快。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“是吗，那昨晚我几点来的？”",
                        VoiceTag = DetectiveVoiceType.Logic,
                        GrantedClueIds = new[] { DetectiveClueIds.BarTimeline },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        ResultLines = new[] { "“23:30 左右。你喝了一杯，接了个电话就走了，脸色很难看。”获得线索【酒吧时间线】。逻辑+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“警察办案。昨晚死者来过吗？”",
                        VoiceTag = DetectiveVoiceType.Authority,
                        GrantedClueIds = new[] { DetectiveClueIds.TrenchcoatMan },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Authority, 1) },
                        ResultLines = new[] { "酒保配合：“你说那个奇怪的男的？他和一个穿风衣的男人来过，两人吵得很凶，风衣男还用酒瓶重重拍了桌子。”获得线索【风衣男】。权威+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“这次喝点不一样的吧。”（无酒精）",
                        VoiceTag = DetectiveVoiceType.None,
                        TimeAdvanceMinutes = 30,
                        SetFlags = new[] { "status_calm" },
                        FocusDelta = 10,
                        ResultLines = new[] { "你接过一杯无酒精的酒，慢慢喝完。时间推进 30 分钟。获得状态【冷静】：专注力+10，离开红灯区前逻辑临时+1（冷静的头脑让你的思路更清晰）。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_phone_booth",
                CharacterId = "char_caller",
                CompletionFlag = "dlg_phone_booth_done",
                RepeatLines = new[] { "（电话里只有忙音。）" },
                OpeningLines = new[] { "（你接起电话。）陌生声音：“你还有最后7小时。我劝你还是别查了，不然你连后悔都来不及的。”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "故意挑衅。冷静，记下这个声音的特征。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "该后悔的是他吧。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“你是谁？”",
                        VoiceTag = DetectiveVoiceType.Logic,
                        GrantedClueIds = new[] { DetectiveClueIds.AnonymousThreat },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Authority, 1) },
                        TimeAdvanceToMinutes = 23 * 60,
                        ResultLines = new[] { "对方挂断。获得线索【匿名威胁】。权威+1（你稳住了对峙的气势）。", "（手机震动）警局内部消息：金融街一所高档公寓发生命案。" },
                    },
                    new OptionEntry
                    {
                        Text = "（沉默）",
                        VoiceTag = DetectiveVoiceType.None,
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        TimeAdvanceToMinutes = 23 * 60,
                        ResultLines = new[] { "你沉默地听完，把声音里的每一处细节都记了下来。逻辑+1。", "（手机震动）警局内部消息：金融街一所高档公寓发生命案。" },
                    },
                    new OptionEntry
                    {
                        Text = "“我早就知道是你。”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.AnonymousThreat },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) },
                        TimeAdvanceToMinutes = 23 * 60,
                        ResultLines = new[] { "对方冷笑：“呵呵，你真的知道吗？”获得线索【匿名威胁】。疯狂+1。", "（手机震动）警局内部消息：金融街一所高档公寓发生命案。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_corpse",
                CharacterId = "char_narrator",
                CompletionFlag = "dlg_corpse_done",
                RepeatLines = new[] { "（白布已经重新盖好。）" },
                OpeningLines = new[] { "掀开白布。死者胸口一刀毙命，无挣扎痕迹。最重要的是——你认识他。" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "一刀毙命且无挣扎，大概率是熟人作案。"),
                    new PopupEntry(DetectiveVoiceType.Empathy, "你的手在抖。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "是不是你自己干的？还记得吗？"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“我真的不记得了……”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.MemoryBlank },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) },
                        ResultLines = new[] { "获得线索【记忆空白】。疯狂+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“先去查监控吧。”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.SurveillanceBlindspot },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        ResultLines = new[] { "获得线索【监控盲区】（公寓走廊区域监控被删）。逻辑+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“他死前很痛苦吗？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.VictimNoPain },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 1) },
                        ResultLines = new[] { "获得线索【死者无痛苦】。共情+1。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_guard",
                CharacterId = "char_guard",
                CompletionFlag = "dlg_guard_done",
                RepeatLines = new[] { "保安低下头，不再说话。" },
                OpeningLines = new[] { "“警官，我真的什么都不知道。我昨晚虽然在值班，但什么都没看到。我发誓，我完全没有走神。”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "他眼神飘忽，手在抖。他在撒谎。"),
                    new PopupEntry(DetectiveVoiceType.Authority, "直接用警徽压他。他没胆子在这种情况下撒谎。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“那我问你，监控为什么被删了？”",
                        VoiceTag = DetectiveVoiceType.Logic,
                        GrantedClueIds = new[] { DetectiveClueIds.BribedGuard, DetectiveClueIds.DeletedFootage },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        ResultLines = new[] { "保安崩溃：“有人给了我钱……让我删掉 23:00–23:30 的录像。”获得线索【保安被收买】与【被删录像：23:00–23:30】。逻辑+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“别在我面前撒谎。我再问最后一次，昨晚到底谁来了？”",
                        VoiceTag = DetectiveVoiceType.Authority,
                        GrantedClueIds = new[] { DetectiveClueIds.BribedGuard, DetectiveClueIds.TrenchcoatCame },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Authority, 1) },
                        ResultLines = new[] { "保安慌张地说：“是一个穿风衣的男人！他给了我一万块！他说想办法阻挠那个警探继续查下去！”获得线索【保安被收买】与【风衣男来过】。权威+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“和我说说吧，你在害怕什么？”",
                        VoiceTag = DetectiveVoiceType.Empathy,
                        GrantedClueIds = new[] { DetectiveClueIds.BribedGuard, DetectiveClueIds.TrenchcoatKnowsYou },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 1) },
                        ResultLines = new[] { "保安低声说：“那个人……他说他认识你。他还说，别让那个警探继续查下去。”获得线索【保安被收买】与【风衣男认识你】。共情+1。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_guard_decision",
                CharacterId = "char_guard",
                CompletionFlag = "dlg_guard_decision_done",
                RepeatLines = new[] { "（你已经做了决定。）" },
                OpeningLines = new[] { "保安：“那警官，现在是要封锁现场，还是先问楼下那个乞丐啊？”（结巴）" },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "封锁现场。",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.FingerprintEvidence },
                        TimeAdvanceMinutes = 15,
                        SetFlags = new[] { "choice_lockdown" },
                        ResultLines = new[] { "你封锁了现场。时间推进 15 分钟。获得线索【物证：指纹】。" },
                    },
                    new OptionEntry
                    {
                        Text = "先找乞丐。",
                        VoiceTag = DetectiveVoiceType.None,
                        TimeAdvanceMinutes = 15,
                        SetFlags = new[] { "unlock_beggar", "choice_beggar_first" },
                        ResultLines = new[] { "你决定先找楼下的乞丐。时间推进 15 分钟。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_beggar",
                CharacterId = "char_beggar",
                CompletionFlag = "dlg_beggar_done",
                RepeatLines = new[] { "乞丐裹紧了毯子，不再理你。" },
                OpeningLines = new[] { "“好心人，给我点钱吧……我看你是警察吧。昨晚我看到好多事呢，但我现在太冷了，记不清……哎呦我的头。”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Empathy, "她是真的很冷。给她点钱吧。"),
                    new PopupEntry(DetectiveVoiceType.Logic, "她在敲诈你——但她说的大概率是真的。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "（给钱）",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.FleeingIndustrial },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 1) },
                        ResultLines = new[] { "“昨晚 23:00，一个穿风衣的男人从公寓跑出来，手上沾着血。嗯……他应该是往旧工业区跑了。”获得线索【风衣男跑向工业区】。共情+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "（不给钱）",
                        VoiceTag = DetectiveVoiceType.None,
                        ResultLines = new[] { "“那算了，你走吧。我什么都不知道，什么都记不起来了。”（永久失去该线索）" },
                    },
                    new OptionEntry
                    {
                        Text = "“我是警察，请配合办案。”",
                        VoiceTag = DetectiveVoiceType.Authority,
                        GrantedClueIds = new[] { DetectiveClueIds.FleeingIndustrial },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Authority, 1) },
                        ResultLines = new[] { "她害怕了：“我说我说！那个风衣男……他往工业区跑了！”获得线索【风衣男跑向工业区】。权威+1。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_barista",
                CharacterId = "char_barista",
                CompletionFlag = "dlg_barista_done",
                RepeatLines = new[] { "咖啡师擦着杯子，摇了摇头。" },
                OpeningLines = new[] { "“警官，其实你昨晚凌晨 2 点来过。而且你当时看起来……很糟糕。”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "凌晨 2 点——案发后两小时，你来过这里。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "你当然不记得。你什么都不记得。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“那我当时点了什么？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.NapkinNotes },
                        ResultLines = new[] { "“你点了双倍浓缩黑咖啡，一直在一张餐巾纸上写东西。”获得线索【餐巾纸笔记】。" },
                    },
                    new OptionEntry
                    {
                        Text = "“那我当时说了什么？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.SelfDoubt },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) },
                        ResultLines = new[] { "“你一直在自言自语，说什么：不是他，是我。”获得线索【自我怀疑】。疯狂+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“那张餐巾纸呢，你还有吗？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.NapkinKiller },
                        ResultLines = new[] { "“你带走了一张。垃圾桶里还有一张，我留着。”获得线索【餐巾纸：凶手是……】。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_monkey",
                CharacterId = "char_monkey",
                CompletionFlag = "dlg_monkey_done",
                RepeatLines = new[] { "瘦猴缩在角落里，不再吭声。" },
                OpeningLines = new[] { "“警官……你还欠我钱。上次那批货，你说好你会搞定的。”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "你欠他钱——看起来你是他的上家。顺着他的话套情报。"),
                    new PopupEntry(DetectiveVoiceType.Authority, "你是警察，别被他牵着走。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“老子的货呢？”",
                        VoiceTag = DetectiveVoiceType.Madness,
                        GrantedClueIds = new[] { DetectiveClueIds.TrenchcoatImpersonates },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) },
                        ResultLines = new[] { "瘦猴缩了缩脖子：“被、被风衣男截了。”——那批货原本属于你。获得线索【风衣男冒充你】。疯狂+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“我是警察。昨晚的事直接说，别给我绕弯子。”",
                        VoiceTag = DetectiveVoiceType.Authority,
                        GrantedClueIds = new[] { DetectiveClueIds.TrenchcoatToResidential },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Authority, 1) },
                        ResultLines = new[] { "瘦猴赶紧解释：“昨晚风衣男来过，拿了一批货就往住宅区去了。”获得线索【风衣男去住宅区】。权威+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“我欠你多少？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.InfoTrade },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        ResultLines = new[] { "“5000。但你上次说过可以用情报抵。”获得线索【你曾用情报交易】。逻辑+1。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_monkey_decision",
                CharacterId = "char_monkey",
                CompletionFlag = "dlg_monkey_decision_done",
                RepeatLines = new[] { "（瘦猴已经替你把消息放出去了。）" },
                OpeningLines = new[] { "瘦猴：“警官，所以你到底要我帮你查风衣男，还是查你自己？”" },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "查风衣男。",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.TrenchcoatFile },
                        TimeAdvanceMinutes = 15,
                        ResultLines = new[] { "时间推进 15 分钟。获得线索【风衣男档案：前警探，曾在三年前被开除】。" },
                    },
                    new OptionEntry
                    {
                        Text = "查自己。",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.YourFile },
                        TimeAdvanceMinutes = 15,
                        ResultLines = new[] { "时间推进 15 分钟。获得线索【你的档案：NC-2077，三年前卧底任务失败】。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_sniper",
                CharacterId = "char_sniper",
                CompletionFlag = "dlg_sniper_done",
                RepeatLines = new[] { "天台上只剩雨声。" },
                OpeningLines = new[] { "“你先别过来，冷静点。我不是来杀你的。我只是来警告你的。”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Madness, "但他在瞄准你。他随时会开枪。"),
                    new PopupEntry(DetectiveVoiceType.Logic, "他说不是来杀你的——但事实就是，他有枪。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“谁派你来的？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.HigherUpsInvolved },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Authority, 1) },
                        ResultLines = new[] { "“是你以前的上级。他说你查太深了。”获得线索【警局高层介入】。权威+1（你正面顶住了枪口）。" },
                    },
                    new OptionEntry
                    {
                        Text = "“呵呵，你知道我是谁吗？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.YourPast },
                        ResultLines = new[] { "“呵呵，你是 NC-2077，三年前卧底失败、被开除的警员。但你还在查案。”获得线索【你的过去】。" },
                    },
                    new OptionEntry
                    {
                        Text = "“别废话了，直接开枪吧。”",
                        VoiceTag = DetectiveVoiceType.None,
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 2) },
                        ResultLines = new[] { "狙击手沉默，然后：“你果然真的疯了。”他转身离开。疯狂+2。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_wife",
                CharacterId = "char_wife",
                CompletionFlag = "dlg_wife_done",
                RepeatLines = new[] { "她转过身去，不再看你。" },
                OpeningLines = new[] { "“你……你是昨晚和他吵架的那个人。你竟然还有脸来？”" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Empathy, "能理解，毕竟她失去了丈夫。还是先道歉吧。"),
                    new PopupEntry(DetectiveVoiceType.Logic, "看来她知道昨晚的事。先去问细节。"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "“真的对不起，女士。我昨晚和他吵了什么？”",
                        VoiceTag = DetectiveVoiceType.Empathy,
                        GrantedClueIds = new[] { DetectiveClueIds.VictimThoughtYouMad },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 1) },
                        ResultLines = new[] { "“他说你疯了。他说你要毁了一切。但我现在不是很信了……因为你看起来比他说的善良。”获得线索【死者认为你疯了】。共情+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“所以昨晚他见了谁？”",
                        VoiceTag = DetectiveVoiceType.Logic,
                        GrantedClueIds = new[] { DetectiveClueIds.LastMeetingTrenchcoat },
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                        ResultLines = new[] { "“是一个穿风衣的男人。他们吵得很凶。然后他就……”获得线索【风衣男最后见面】。逻辑+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“你丈夫到底在查什么？”",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.KeptNotes },
                        ResultLines = new[] { "“我也不知道具体内容。但我只知道他一直在写东西，都收在他的遗物里。”获得线索【他一直留着笔记】（提示：查看遗物）。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_wife_decision",
                CharacterId = "char_wife",
                CompletionFlag = "dlg_wife_decision_done",
                RepeatLines = new[] { "（遗物已经收走了。）" },
                OpeningLines = new[] { "她问：“所以，你要看他的遗物吗？”" },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "看遗物。",
                        VoiceTag = DetectiveVoiceType.None,
                        GrantedClueIds = new[] { DetectiveClueIds.VictimNotes },
                        TimeAdvanceMinutes = 15,
                        ResultLines = new[] { "时间推进 15 分钟。获得线索【死者笔记：风衣男和警局高层有交易】。" },
                    },
                    new OptionEntry
                    {
                        Text = "不看。",
                        VoiceTag = DetectiveVoiceType.None,
                        ResultLines = new[] { "你摇了摇头。一无所获。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_drawer_diary",
                CharacterId = "char_narrator",
                GrantedClueIds = new[] { DetectiveClueIds.DiaryThreatened, DetectiveClueIds.SafeCode2077 },
                CompletionFlag = "dlg_drawer_diary_done",
                RepeatLines = new[] { "（你已经读过这本日记了。）" },
                OpeningLines = new[] { "“他说他会帮我。但我发现他根本就是在骗我！今晚我要和他摊牌。如果我没回来，就去找红灯区的那个女人，她知道一切。”获得线索【日记：死者被威胁】，获得【保险箱密码：2077】。" },
            },
            new()
            {
                FileName = "dlg_drawer_diary_locked",
                CharacterId = "char_narrator",
                OpeningLines = new[] { "字迹太潦草了，你看不太懂。可能需要更冷静的头脑。" },
            },
            new()
            {
                FileName = "dlg_computer",
                CharacterId = "char_narrator",
                GrantedClueIds = new[] { DetectiveClueIds.WasUndercover, DetectiveClueIds.MissionFailed },
                CompletionFlag = "dlg_computer_done",
                RepeatLines = new[] { "（你已经看过这些邮件了。）" },
                OpeningLines = new[] { "邮件记录——发件人：警局高层。内容：“NC-2077，你的卧底任务立即终止。目标已发现你。建议迅速撤离。”日期：三年前。获得线索【你曾是卧底】【任务失败】。" },
            },
            new()
            {
                FileName = "dlg_computer_locked",
                CharacterId = "char_narrator",
                OpeningLines = new[] { "这是一个加密了的电脑。你的脑子现在转不动。" },
            },
            new()
            {
                FileName = "dlg_your_diary",
                CharacterId = "char_narrator",
                GrantedClueIds = new[] { DetectiveClueIds.YourDiaryDoubt },
                CompletionFlag = "dlg_your_diary_done",
                RepeatLines = new[] { "（你已经读过这本日记了。）" },
                OpeningLines = new[] { "“我可能真的不能再查了。他们已经发现了。但如果我不查，他就白死了。风衣男……我最好的搭档。为什么真的是你？”获得线索【你的日记：怀疑搭档】。" },
            },
            new()
            {
                FileName = "dlg_your_diary_locked",
                CharacterId = "char_narrator",
                OpeningLines = new[] { "这里的字迹太潦草了，你看不太懂。" },
            },
            new()
            {
                FileName = "dlg_leaving_home",
                CharacterId = "char_narrator",
                CompletionFlag = "dlg_leaving_home_done",
                RepeatLines = new[] { "（雨还在下。该走了。）" },
                OpeningLines = new[] { "你站在自家门口，手握着门把手。雨声隔着门板渗进来。接下来去哪？" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "时间不多了。直接去死者家，家属也许知道些什么。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "先去喝一杯吧。你需要缓一缓。"),
                    new PopupEntry(DetectiveVoiceType.Empathy, "心里有点不安……要不要再回屋里看一眼？"),
                },
                Options = new[]
                {
                    new OptionEntry
                    {
                        Text = "去死者家问问家属。",
                        VoiceTag = DetectiveVoiceType.Logic,
                        SetFlags = new[] { "home_exit_selected" },
                        ResultLines = new[] { "你拉开门，走进雨里。" },
                    },
                    new OptionEntry
                    {
                        Text = "先去喝一杯。",
                        VoiceTag = DetectiveVoiceType.Madness,
                        TimeAdvanceMinutes = 60,
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) },
                        SetFlags = new[] { "leaving_home_drink_used" },
                        ForbiddenFlags = new[] { "leaving_home_drink_used" },
                        IsTerminal = false,
                        ResultLines = new[] { "你在楼下便利店买了瓶酒，就着雨声灌了下去。时间推进 60 分钟。疯狂+1。" },
                    },
                    new OptionEntry
                    {
                        Text = "“我好像落了东西在屋里。”（折返）",
                        VoiceTag = DetectiveVoiceType.None,
                        RequireClueCountMin = 10,
                        StartFinalDuel = true,
                        ResultLines = new[] { "你折回屋里。黑暗里，风衣男坐在椅子上，像等了很久。" },
                    },
                    new OptionEntry
                    {
                        Text = "“我好像落了东西在屋里。”（折返）",
                        VoiceTag = DetectiveVoiceType.None,
                        RequireClueCountMax = 9,
                        VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) },
                        ResultLines = new[] { "屋里空无一人。桌上只压着一张字条：“我在等你。想清楚再来。”疯狂+1。" },
                    },
                },
            },
            new()
            {
                FileName = "dlg_graffiti",
                CharacterId = "char_narrator",
                GrantedClueIds = new[] { DetectiveClueIds.GraffitiTraitor },
                CompletionFlag = "dlg_graffiti_done",
                RepeatLines = new[] { "（你已经记下这幅涂鸦了。）" },
                OpeningLines = new[] { "红色喷漆，写着“NC-2077 是叛徒”。" },
                Popups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "NC-2077 是你的警徽编号。这是写给你看的——有人想吓退你，或者陷害你。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "这说的没错啊。你本来就是叛徒。你忘了吗？"),
                },
            },
        };

        public static readonly RecipeEntry[] Recipes =
        {
            new() { FileName="recipe_erased_camera", RecipeId="erased_camera", RequiredClueIds=new[]{DetectiveClueIds.BribedGuard,DetectiveClueIds.DeletedFootage}, ResultText="保安承认收钱删去23:00—23:30的录像。这段空白是人为制造的，监控缺失不能证明风衣男没有到场。", OnceOnly=true },
            new() { FileName="recipe_partner_history", RecipeId="partner_history", RequiredClueIds=new[]{DetectiveClueIds.FormerPartners,DetectiveClueIds.TrenchcoatFile}, ResultText="合影和前警探档案把风衣男与你的警局经历联系起来：他是旧搭档，并非偶然认识你的陌生人。这不直接证明他在本案中的立场。", OnceOnly=true },
            new() { FileName="recipe_failed_mission", RecipeId="failed_mission", RequiredClueIds=new[]{DetectiveClueIds.WasUndercover,DetectiveClueIds.DismissalNotice}, ResultText="撤离邮件与开除通知指向同一段三年前的经历：你曾执行卧底任务，随后被开除。任务失败的责任仍不能只凭通知判断。", OnceOnly=true },
            new() { FileName="recipe_sleeping_pills", RecipeId="sleeping_pills", RequiredClueIds=new[]{DetectiveClueIds.SleepingPills,DetectiveClueIds.SleepingPillsEmpty}, ResultText="你家与死者住处都有相同的安眠药瓶。这是共同用药或接触的线索，不能据此确认是谁服药，更不能直接解释全部记忆空白。", OnceOnly=true },
            new() { FileName="recipe_industrial_route", RecipeId="industrial_route", RequiredClueIds=new[]{DetectiveClueIds.FleeingIndustrial,DetectiveClueIds.TireTracks}, ResultText="乞丐指向工业区，入口车辙又通向仓库。仓库值得继续调查，但尚不能认定摩托车属于风衣男。", OnceOnly=true },
            new() { FileName="recipe_letter_partner", RecipeId="letter_partner", RequiredClueIds=new[]{DetectiveClueIds.LetterWarning,DetectiveClueIds.FormerPartners}, ResultText="照片支持信中关于旧搭档身份的说法，但不能验证背叛指控。应把已确认的身份和信中的指控分开。", OnceOnly=true },
            new()
            {
                FileName = "recipe_gate_key",
                RecipeId = "gate_key_match",
                RequiredClueIds = new[] { DetectiveClueIds.DroppedKey, DetectiveClueIds.GateForced },
                ResultText = "掉落的钥匙与工业区铁门的锁芯匹配，说明风衣男持有这里的钥匙。门已被撬开，无需再次开锁；这能联系他的行踪，但不能断定是谁撬了门。",
                OnceOnly = false,
                FreeSuccess = true,
            },
            new()
            {
                FileName = "recipe_prologue_note",
                RecipeId = "prologue_note",
                RequiredClueIds = new[] { DetectiveClueIds.BloodyNote },
                ResultText = "死者曾约你 23:00 见面",
                FreeSuccess = true,
                OnceOnly = false,
            },
            new()
            {
                FileName = "recipe_gate_note",
                RecipeId = "gate_note",
                RequiredClueIds = new[] { DetectiveClueIds.DoorRecord, DetectiveClueIds.BloodyNote },
                ResultText = "约会定在 23:00，但 22:45 就有人用你的警徽进了公寓——要么你早到了，要么有人冒用了你的身份。",
                VoiceChanges = new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) },
                FailFocusCost = 10,
                OnceOnly = true,
            },
        };

        public static readonly DuelEntry[] Duels =
        {
            new()
            {
                FileName = "duel_trenchcoat_gate",
                DuelId = "trenchcoat_gate",
                Title = "言语对决：审问风衣男",
                OpponentName = "风衣男",
                OpeningLines = new[] { "唉，我和你说过，你不该查下去的。你虽然忘了，但我记得。" },
                VoicePopups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "他说监控能证明——可监控被删了。他在虚张声势。"),
                    new PopupEntry(DetectiveVoiceType.Authority, "你是警察，不用怕，直接压他。"),
                    new PopupEntry(DetectiveVoiceType.Empathy, "他说“你忘了”的时候，语气很复杂。他不全是恶意。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "他说的对。你忘了。你到底忘了什么？"),
                },
                Statements = new[]
                {
                    new DuelStatementEntry(
                        text: "我昨晚根本没来过这里。监控完全可以证明。",
                        pressHints: new[]
                        {
                            new PopupEntry(DetectiveVoiceType.Logic, "监控 23:00–23:30 被删了——他以为没人知道。"),
                            new PopupEntry(DetectiveVoiceType.Authority, "让被买通的保安开口，他的“证明”就塌了。"),
                            new PopupEntry(DetectiveVoiceType.Empathy, "他在赌你什么都想不起来。别顺着他的话走。"),
                            new PopupEntry(DetectiveVoiceType.Madness, "他在撒谎。你闻得出来。"),
                        },
                        reactions: new[]
                        {
                            new CardReactionEntry(
                                DetectiveClueIds.BribedGuard,
                                "可你的监控，23:00 到 23:30 是空的。（风衣男开始慌）",
                                advance: true),
                            new CardReactionEntry(
                                DetectiveClueIds.DoorRecord,
                                "那也可能是别人啊。",
                                focusDelta: -10),
                            new CardReactionEntry(
                                DetectiveClueIds.FleeingIndustrial,
                                "一个乞丐的话你也信？大警官。",
                                focusDelta: -10),
                            new CardReactionEntry(
                                DetectiveClueIds.NapkinKiller,
                                "用你自己的笔记？我看你真是疯了。",
                                focusDelta: -10,
                                voiceChanges: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) }),
                        }),
                    new DuelStatementEntry(
                        text: "……就算监控没了，也没证据把我和这个案子连起来。",
                        pressHints: new[]
                        {
                            new PopupEntry(DetectiveVoiceType.Logic, "没有监控，还有别的——能直接指向他的东西。"),
                            new PopupEntry(DetectiveVoiceType.Authority, "摊牌的时候到了，把最有力的证据拍出来。"),
                            new PopupEntry(DetectiveVoiceType.Empathy, "他还在嘴硬。一句话就能击碎他。"),
                            new PopupEntry(DetectiveVoiceType.Madness, "你录下了他的声音。让他自己听听。"),
                        },
                        reactions: new[]
                        {
                            new CardReactionEntry(
                                DetectiveClueIds.Recording,
                                "你……你哪来的录音？！（风衣男崩溃）",
                                advance: true),
                        }),
                },
                DefaultWrongFocusCost = 10,
                MaxMistakes = 3,
                WinClues = new[] { DetectiveClueIds.CoatManConfessed, DetectiveClueIds.DroppedKey },
                WinResultLines = new[] { "风衣男崩溃地后退，转身逃进雨里。", "获得线索【风衣男承认在场】。他逃跑时留下了【掉落的钥匙】。" },
                FailResultLines = new[] { "风衣男冷笑一声，撑开黑伞走进雨幕。你没能留住他。" },
                OnFailSetFlags = new[] { "duel_gate_failed" },
            },
            new()
            {
                FileName = "duel_final",
                DuelId = "final",
                Title = "最终对决",
                OpponentName = "风衣男",
                OpeningLines = new[] { "呵呵，你终于想起来了？还是你现在还在装？" },
                VoicePopups = new[]
                {
                    new PopupEntry(DetectiveVoiceType.Logic, "他就是在这等你。他应该已经准备好了。"),
                    new PopupEntry(DetectiveVoiceType.Empathy, "感觉他说话带点哭腔。难道他不想这样？"),
                    new PopupEntry(DetectiveVoiceType.Authority, "你是警察，直接逮捕他。"),
                    new PopupEntry(DetectiveVoiceType.Madness, "就是你杀的人。你真忘了吗？"),
                },
                Statements = new[]
                {
                    new DuelStatementEntry(
                        text: "你真以为是我杀的？你从头到尾都在查我，可是你又有什么证据呢？",
                        phaseLabel: "第一阶段·你怀疑他",
                        pressHints: new[]
                        {
                            new PopupEntry(DetectiveVoiceType.Logic, "22:45 刷你的警徽进公寓的人——他自己心里有数。"),
                            new PopupEntry(DetectiveVoiceType.Empathy, "你写过怀疑他的日记。那本日记还在。"),
                            new PopupEntry(DetectiveVoiceType.Authority, "他威胁别人的录音，就是你的证据。"),
                            new PopupEntry(DetectiveVoiceType.Madness, "或者……什么都别问，先听听乞丐看见了什么。"),
                        },
                        skipReply: "哼，没话说了吧。（风衣男岔开了话题）",
                        reactions: new[]
                        {
                            new CardReactionEntry(
                                DetectiveClueIds.Recording,
                                "我那是为了保护你啊。",
                                advance: true),
                            new CardReactionEntry(
                                DetectiveClueIds.DoorRecord,
                                "你忘了那是拿你的警徽刷的吗？",
                                advance: true,
                                voiceChanges: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 1) }),
                            new CardReactionEntry(
                                DetectiveClueIds.YourDiaryDoubt,
                                "你还怀疑我？你忘了我们是什么关系？",
                                advance: true,
                                voiceChanges: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 1) }),
                            new CardReactionEntry(
                                DetectiveClueIds.FleeingIndustrial,
                                "一个乞丐的话你也信？",
                                focusDelta: -10),
                        }),
                    new DuelStatementEntry(
                        text: "而且你有没有想过——那天晚上，去他家的人其实是你。",
                        phaseLabel: "第二阶段·他怀疑你",
                        pressHints: new[]
                        {
                            new PopupEntry(DetectiveVoiceType.Logic, "你脸上的伤是怎么来的？镜子不会撒谎。"),
                            new PopupEntry(DetectiveVoiceType.Empathy, "他写过一封信给你。一封警告。"),
                            new PopupEntry(DetectiveVoiceType.Authority, "别让他的话带节奏。你手里有他害怕的东西。"),
                            new PopupEntry(DetectiveVoiceType.Madness, "你在咖啡店的餐巾纸上写过什么，忘了吗？"),
                        },
                        skipReply: "（风衣男摇了摇头，不想再谈这个。）",
                        reactions: new[]
                        {
                            new CardReactionEntry(
                                DetectiveClueIds.NapkinKiller,
                                "原来是你写的。看来你在咖啡店的时候就想起来了。",
                                advance: true,
                                voiceChanges: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 2) }),
                            new CardReactionEntry(
                                DetectiveClueIds.LetterWarning,
                                "好吧，这封信是我写的。但我是怕你忘了。",
                                advance: true,
                                voiceChanges: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Empathy, 2) }),
                            new CardReactionEntry(
                                DetectiveClueIds.MirrorBruise,
                                "他打碎的。和他打架的人是你。",
                                advance: true,
                                voiceChanges: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 1) }),
                            new CardReactionEntry(
                                DetectiveClueIds.MemoryBlank,
                                "你当然不记得。当时你可是喝了一整瓶。",
                                focusDelta: -10),
                        }),
                    new DuelStatementEntry(
                        text: "现在你终于知道了吧。那天晚上你去了他家，你们吵架了，你推了他。但关键的是你没杀他——你走的时候他还活着。",
                        phaseLabel: "第三阶段·真相抉择",
                        pressHints: new[]
                        {
                            new PopupEntry(DetectiveVoiceType.Logic, "死者留下的笔记能证明真正凶手是谁——只要你足够清醒。"),
                            new PopupEntry(DetectiveVoiceType.Empathy, "你是警察。你的警徽还在。"),
                            new PopupEntry(DetectiveVoiceType.Authority, "录音里他的声音……那才是他的真面目。"),
                            new PopupEntry(DetectiveVoiceType.Madness, "或者，接受你写下的那个答案。"),
                        },
                        reactions: new[]
                        {
                            new CardReactionEntry(
                                DetectiveClueIds.VictimNotes,
                                "……你从哪里找到这个的。",
                                setFlag: "ending_choice_a",
                                requireVoices: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Logic, 3) },
                                lockedReply: "你的脑子转不到那一步。"),
                            new CardReactionEntry(
                                DetectiveClueIds.Badge,
                                "（你举起警徽，编号 NC-2077。）",
                                setFlag: "ending_choice_b"),
                            new CardReactionEntry(
                                DetectiveClueIds.Recording,
                                "……原来你一直留着这个。",
                                setFlag: "ending_choice_c",
                                requireVoices: new[]
                                {
                                    new VoiceDeltaEntry(DetectiveVoiceType.Authority, 2),
                                    new VoiceDeltaEntry(DetectiveVoiceType.Madness, 2),
                                },
                                lockedReply: "你的脑子转不到那一步。"),
                            new CardReactionEntry(
                                DetectiveClueIds.NapkinKiller,
                                "……你想起来了？你真的想起来了？",
                                setFlag: "ending_choice_d",
                                requireVoices: new[] { new VoiceDeltaEntry(DetectiveVoiceType.Madness, 3) },
                                requireClues: new[] { DetectiveClueIds.LetterWarning, DetectiveClueIds.MemoryBlank },
                                lockedReply: "你的脑子转不到那一步。"),
                        }),
                },
                DefaultWrongFocusCost = 10,
                MaxMistakes = 3,
                WinResultLines = new[] { "风衣男放下了枪。" },
                FailResultLines = new[] { "风衣男摇了摇头，站起身，走进黑暗里。链条断裂，你没能压制他。" },
                OnFailSetFlags = new[] { "final_duel_failed" },
            },
        };
    }
}
