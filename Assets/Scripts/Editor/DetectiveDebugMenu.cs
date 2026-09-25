using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Detective.EditorTools
{
    // 循环可测性工具：仅在播放中可用（每个菜单项都带 validate）。
    internal static class DetectiveDebugMenu
    {
        private const string MenuRoot = "Detective/Debug/";
        private const int MaxVoiceLevel = 3;

        private static readonly string[] EndingDClues =
        {
            DetectiveClueIds.NapkinKiller,
            DetectiveClueIds.LetterWarning,
            DetectiveClueIds.MemoryBlank,
        };

        private static bool ValidatePlaying()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(MenuRoot + "Toast 自检")]
        private static void ToastSelfCheck()
        {
            var toast = DetectiveToastUI.Instance;
            Debug.Log($"[DebugMenu] ToastUI.Instance={(toast != null ? toast.gameObject.name + " active=" + toast.gameObject.activeInHierarchy : "null")}");
            if (toast != null)
            {
                toast.Show("Toast 自检：这是一条测试消息");
            }
        }

        [MenuItem(MenuRoot + "Toast 自检", true)]
        private static bool ToastSelfCheckValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/序章小巷")]
        private static void GoD0Alley() => JumpTo("D0_Alley");

        [MenuItem(MenuRoot + "跳转到区域/序章小巷", true)]
        private static bool GoD0AlleyValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/红灯区街道")]
        private static void GoD1RedLight() => JumpTo("D1_RedLight");

        [MenuItem(MenuRoot + "跳转到区域/红灯区街道", true)]
        private static bool GoD1RedLightValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/酒吧")]
        private static void GoI1Bar() => JumpTo("I1_Bar");

        [MenuItem(MenuRoot + "跳转到区域/酒吧", true)]
        private static bool GoI1BarValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/金融街街道")]
        private static void GoD2Financial() => JumpTo("D2_Financial");

        [MenuItem(MenuRoot + "跳转到区域/金融街街道", true)]
        private static bool GoD2FinancialValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/死者公寓")]
        private static void GoI2Apartment() => JumpTo("I2_Apartment");

        [MenuItem(MenuRoot + "跳转到区域/死者公寓", true)]
        private static bool GoI2ApartmentValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/咖啡店")]
        private static void GoI2Coffee() => JumpTo("I2_Coffee");

        [MenuItem(MenuRoot + "跳转到区域/咖啡店", true)]
        private static bool GoI2CoffeeValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/工业区外场")]
        private static void GoD3Industrial() => JumpTo("D3_Industrial");

        [MenuItem(MenuRoot + "跳转到区域/工业区外场", true)]
        private static bool GoD3IndustrialValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/废弃仓库")]
        private static void GoI3Warehouse() => JumpTo("I3_Warehouse");

        [MenuItem(MenuRoot + "跳转到区域/废弃仓库", true)]
        private static bool GoI3WarehouseValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/工厂地下室")]
        private static void GoI3Basement() => JumpTo("I3_Basement");

        [MenuItem(MenuRoot + "跳转到区域/工厂地下室", true)]
        private static bool GoI3BasementValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/工厂天台")]
        private static void GoI3Rooftop() => JumpTo("I3_Rooftop");

        [MenuItem(MenuRoot + "跳转到区域/工厂天台", true)]
        private static bool GoI3RooftopValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/住宅区街道")]
        private static void GoD4Residential() => JumpTo("D4_Residential");

        [MenuItem(MenuRoot + "跳转到区域/住宅区街道", true)]
        private static bool GoD4ResidentialValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/你的公寓")]
        private static void GoI4YourHome() => JumpTo("I4_YourHome");

        [MenuItem(MenuRoot + "跳转到区域/你的公寓", true)]
        private static bool GoI4YourHomeValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "跳转到区域/死者家中")]
        private static void GoI4VictimHome() => JumpTo("I4_VictimHome");

        [MenuItem(MenuRoot + "跳转到区域/死者家中", true)]
        private static bool GoI4VictimHomeValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "发放最终对决线索包")]
        private static void GrantFinalDuelCluePack()
        {
            var clueIds = new HashSet<string>();
            foreach (DetectiveContentData.DuelEntry duel in DetectiveContentData.Duels)
            {
                if (duel.DuelId != "final")
                {
                    continue;
                }

                if (duel.WinClues != null)
                {
                    foreach (string clueId in duel.WinClues)
                    {
                        AddClueId(clueIds, clueId);
                    }
                }

                if (duel.Statements == null)
                {
                    continue;
                }

                foreach (DetectiveContentData.DuelStatementEntry statement in duel.Statements)
                {
                    if (statement.Reactions == null)
                    {
                        continue;
                    }

                    foreach (DetectiveContentData.CardReactionEntry reaction in statement.Reactions)
                    {
                        AddClueId(clueIds, reaction.ClueId);
                        if (reaction.RequireClues == null)
                        {
                            continue;
                        }

                        foreach (string clueId in reaction.RequireClues)
                        {
                            AddClueId(clueIds, clueId);
                        }
                    }
                }
            }

            int granted = 0;
            foreach (string clueId in clueIds)
            {
                if (DetectiveGameState.CollectClue(clueId))
                {
                    granted++;
                }
            }

            Debug.Log($"[DetectiveDebugMenu] 最终对决线索包：新发放 {granted}/{clueIds.Count} 条（共需线索 {DetectiveInvestigationState.CollectedClueCount}）。");
        }

        [MenuItem(MenuRoot + "发放最终对决线索包", true)]
        private static bool GrantFinalDuelCluePackValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "发放结局D三件套（餐巾纸/警告信/记忆空白）")]
        private static void GrantEndingDClues()
        {
            foreach (string clueId in EndingDClues)
            {
                DetectiveGameState.CollectClue(clueId);
            }

            Debug.Log($"[DetectiveDebugMenu] 结局D三件套已发放（共需线索 {DetectiveInvestigationState.CollectedClueCount}）。");
        }

        [MenuItem(MenuRoot + "发放结局D三件套（餐巾纸/警告信/记忆空白）", true)]
        private static bool GrantEndingDCluesValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "四种声音各 +1")]
        private static void RaiseAllVoices()
        {
            foreach (DetectiveVoiceType voice in VoiceValues())
            {
                DetectiveGameState.AddVoice(voice, 1);
            }
        }

        [MenuItem(MenuRoot + "四种声音各 +1", true)]
        private static bool RaiseAllVoicesValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "四种声音直接设满（Lv.3）")]
        private static void MaxAllVoices()
        {
            foreach (DetectiveVoiceType voice in VoiceValues())
            {
                DetectiveGameState.AddVoice(voice, MaxVoiceLevel - DetectiveGameState.GetVoice(voice));
            }
        }

        [MenuItem(MenuRoot + "四种声音直接设满（Lv.3）", true)]
        private static bool MaxAllVoicesValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "时间推进 30 分钟")]
        private static void AdvanceTime30()
        {
            DetectiveGameState.AdvanceTime(30);
            Debug.Log($"[DetectiveDebugMenu] 时间推进到 {DetectiveGameState.TimeText}。");
        }

        [MenuItem(MenuRoot + "时间推进 30 分钟", true)]
        private static bool AdvanceTime30Validate() => ValidatePlaying();

        [MenuItem(MenuRoot + "触发结局/A 完美真相")]
        private static void TriggerEndingA() => DetectiveGameState.SetFlag(DetectiveEndingResolver.EndingChoiceAFlag);

        [MenuItem(MenuRoot + "触发结局/A 完美真相", true)]
        private static bool TriggerEndingAValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "触发结局/B 正义")]
        private static void TriggerEndingB() => DetectiveGameState.SetFlag(DetectiveEndingResolver.EndingChoiceBFlag);

        [MenuItem(MenuRoot + "触发结局/B 正义", true)]
        private static bool TriggerEndingBValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "触发结局/C 都市规则")]
        private static void TriggerEndingC() => DetectiveGameState.SetFlag(DetectiveEndingResolver.EndingChoiceCFlag);

        [MenuItem(MenuRoot + "触发结局/C 都市规则", true)]
        private static bool TriggerEndingCValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "触发结局/D 你是凶手")]
        private static void TriggerEndingD() => DetectiveGameState.SetFlag(DetectiveEndingResolver.EndingChoiceDFlag);

        [MenuItem(MenuRoot + "触发结局/D 你是凶手", true)]
        private static bool TriggerEndingDValidate() => ValidatePlaying();

        [MenuItem(MenuRoot + "触发结局/E 悬案")]
        private static void TriggerEndingE()
        {
            DetectiveGameState.RemoveFlag(DetectiveEndingResolver.EndingChoiceAFlag);
            DetectiveGameState.RemoveFlag(DetectiveEndingResolver.EndingChoiceBFlag);
            DetectiveGameState.RemoveFlag(DetectiveEndingResolver.EndingChoiceCFlag);
            DetectiveGameState.RemoveFlag(DetectiveEndingResolver.EndingChoiceDFlag);
            if (DetectiveEndingUI.Instance != null)
            {
                DetectiveEndingUI.Instance.PlayEnding(EndingType.E_Unsolved);
            }
        }

        [MenuItem(MenuRoot + "触发结局/E 悬案", true)]
        private static bool TriggerEndingEValidate() => ValidatePlaying();

        private static void JumpTo(string regionId)
        {
            if (DetectiveRegionLoader.Instance != null)
            {
                DetectiveRegionLoader.Instance.LoadRegion(regionId);
            }
        }

        private static void AddClueId(HashSet<string> clueIds, string clueId)
        {
            if (!string.IsNullOrWhiteSpace(clueId))
            {
                clueIds.Add(clueId);
            }
        }

        private static DetectiveVoiceType[] VoiceValues()
        {
            return new[]
            {
                DetectiveVoiceType.Logic,
                DetectiveVoiceType.Empathy,
                DetectiveVoiceType.Authority,
                DetectiveVoiceType.Madness,
            };
        }
    }
}
