using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Detective.EditorTools;

namespace Detective.EditorTools
{
    public static class DetectiveContentGenerator
    {
        private const string ClueFolder = "Assets/Resources/Detective/Clues";
        private const string RecipeFolder = "Assets/Resources/Detective/Recipes";
        private const string DialogueFolder = "Assets/Data/Dialogues";
        private const string DuelFolder = "Assets/Data/Duels";
        private const string CharacterFolder = "Assets/Data/Characters";

        [MenuItem("Detective/Generate Content")]
        public static void GenerateAll()
        {
            EnsureFolder(ClueFolder);
            EnsureFolder(RecipeFolder);
            EnsureFolder(DialogueFolder);
            EnsureFolder(DuelFolder);
            EnsureFolder(CharacterFolder);

            GenerateCharacters();
            GenerateClues();
            GenerateRecipes();
            GenerateDialogues();
            GenerateDuels();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DetectiveContentGenerator] 内容生成完成。");
        }

        private static void GenerateCharacters()
        {
            foreach (DetectiveContentData.CharacterEntry entry in DetectiveContentData.Characters)
            {
                string path = $"{CharacterFolder}/{entry.FileName}.asset";
                var asset = LoadOrCreate<DetectiveCharacterDefinition>(path);
                SetField(asset, "characterId", entry.CharacterId);
                SetField(asset, "displayName", entry.DisplayName);
                SetField(asset, "description", entry.Description);
                EditorUtility.SetDirty(asset);
            }
        }

        private static void GenerateClues()
        {
            foreach (DetectiveContentData.ClueEntry entry in DetectiveContentData.Clues)
            {
                string path = $"{ClueFolder}/{entry.Id}.asset";
                var asset = LoadOrCreate<DetectiveClueDefinition>(path);
                SetField(asset, "clueId", entry.Id);
                SetField(asset, "title", entry.Title);
                SetField(asset, "description", entry.Description);
                EditorUtility.SetDirty(asset);
            }
        }

        private static void GenerateRecipes()
        {
            foreach (DetectiveContentData.RecipeEntry entry in DetectiveContentData.Recipes)
            {
                string path = $"{RecipeFolder}/{entry.FileName}.asset";
                var asset = LoadOrCreate<DetectiveReasoningRecipe>(path);
                SetField(asset, "recipeId", entry.RecipeId);
                SetField(asset, "requiredClueIds", entry.RequiredClueIds);
                SetField(asset, "resultText", entry.ResultText);
                SetField(asset, "grantedClueId", entry.GrantedClueId);
                SetField(asset, "voiceChanges", BuildVoiceChanges(entry.VoiceChanges));
                SetField(asset, "failFocusCost", entry.FailFocusCost);
                SetField(asset, "onceOnly", entry.OnceOnly);
                SetField(asset, "freeSuccess", entry.FreeSuccess);
                EditorUtility.SetDirty(asset);
            }
        }

        private static void GenerateDialogues()
        {
            var characters = new Dictionary<string, DetectiveCharacterDefinition>();
            foreach (DetectiveContentData.CharacterEntry entry in DetectiveContentData.Characters)
            {
                characters[entry.CharacterId] = AssetDatabase.LoadAssetAtPath<DetectiveCharacterDefinition>($"{CharacterFolder}/{entry.FileName}.asset");
            }

            foreach (DetectiveContentData.DialogueEntry entry in DetectiveContentData.Dialogues)
            {
                string path = $"{DialogueFolder}/{entry.FileName}.asset";
                var asset = LoadOrCreate<DetectiveDialogueDefinition>(path);
                SetField(asset, "characterDefinition", characters.TryGetValue(entry.CharacterId ?? string.Empty, out var character) ? character : null);
                SetField(asset, "openingLines", entry.OpeningLines);
                SetField(asset, "voicePopups", BuildDialoguePopups(entry.Popups));
                SetField(asset, "options", BuildDialogueOptions(entry.Options));
                SetField(asset, "grantedClueIds", entry.GrantedClueIds);
                SetField(asset, "completionFlag", entry.CompletionFlag);
                SetField(asset, "repeatLines", entry.RepeatLines);
                EditorUtility.SetDirty(asset);
            }
        }

        private static void GenerateDuels()
        {
            foreach (DetectiveContentData.DuelEntry entry in DetectiveContentData.Duels)
            {
                string path = $"{DuelFolder}/{entry.FileName}.asset";
                var asset = LoadOrCreate<DetectiveDuelDefinition>(path);
                SetField(asset, "duelId", entry.DuelId);
                SetField(asset, "title", entry.Title);
                SetField(asset, "opponentName", entry.OpponentName);
                SetField(asset, "openingLines", entry.OpeningLines);
                SetField(asset, "voicePopups", BuildDuelPopups(entry.VoicePopups));
                SetField(asset, "statements", BuildDuelStatements(entry.Statements));
                SetField(asset, "defaultWrongFocusCost", entry.DefaultWrongFocusCost);
                SetField(asset, "maxMistakes", entry.MaxMistakes);
                SetField(asset, "winClues", entry.WinClues);
                SetField(asset, "winResultLines", entry.WinResultLines);
                SetField(asset, "failResultLines", entry.FailResultLines);
                SetField(asset, "onWinSetFlags", entry.OnWinSetFlags);
                SetField(asset, "onFailSetFlags", entry.OnFailSetFlags);
                EditorUtility.SetDirty(asset);
            }
        }

        private static Array BuildDialoguePopups(DetectiveContentData.PopupEntry[] popups)
        {
            Type popupType = typeof(DetectiveDialogueDefinition).GetNestedType("VoicePopup");
            if (popups == null || popups.Length == 0)
            {
                return Array.CreateInstance(popupType, 0);
            }

            var array = Array.CreateInstance(popupType, popups.Length);
            for (int i = 0; i < popups.Length; i++)
            {
                var popup = Activator.CreateInstance(popupType);
                SetPublicField(popup, "voice", popups[i].Voice);
                SetPublicField(popup, "text", popups[i].Text);
                array.SetValue(popup, i);
            }

            return array;
        }

        private static Array BuildDuelPopups(DetectiveContentData.PopupEntry[] popups)
        {
            Type popupType = typeof(DetectiveDuelDefinition).GetNestedType("VoicePopup");
            if (popups == null || popups.Length == 0)
            {
                return Array.CreateInstance(popupType, 0);
            }

            var array = Array.CreateInstance(popupType, popups.Length);
            for (int i = 0; i < popups.Length; i++)
            {
                var popup = Activator.CreateInstance(popupType);
                SetPublicField(popup, "voice", popups[i].Voice);
                SetPublicField(popup, "text", popups[i].Text);
                array.SetValue(popup, i);
            }

            return array;
        }

        private static Array BuildDialogueOptions(DetectiveContentData.OptionEntry[] options)
        {
            Type optionType = typeof(DetectiveDialogueDefinition).GetNestedType("DialogueOption");
            if (options == null || options.Length == 0)
            {
                return Array.CreateInstance(optionType, 0);
            }

            var array = Array.CreateInstance(optionType, options.Length);
            for (int i = 0; i < options.Length; i++)
            {
                DetectiveContentData.OptionEntry source = options[i];
                var option = Activator.CreateInstance(optionType);
                SetPublicField(option, "text", source.Text);
                SetPublicField(option, "voiceTag", source.VoiceTag);
                SetPublicField(option, "grantedClueIds", source.GrantedClueIds);
                SetPublicField(option, "voiceChanges", BuildVoiceChanges(source.VoiceChanges));
                SetPublicField(option, "timeAdvanceMinutes", source.TimeAdvanceMinutes);
                SetPublicField(option, "timeAdvanceToMinutes", source.TimeAdvanceToMinutes);
                SetPublicField(option, "setFlags", source.SetFlags);
                SetPublicField(option, "requireFlags", source.RequireFlags);
                SetPublicField(option, "forbiddenFlags", source.ForbiddenFlags);
                SetPublicField(option, "requireVoiceLevel", BuildVoiceRequirement(source.RequireVoiceLevel));
                SetPublicField(option, "requireClueCountMin", source.RequireClueCountMin);
                SetPublicField(option, "requireClueCountMax", source.RequireClueCountMax);
                SetPublicField(option, "focusDelta", (float)source.FocusDelta);
                SetPublicField(option, "startFinalDuel", source.StartFinalDuel);
                SetPublicField(option, "resultLines", source.ResultLines);
                SetPublicField(option, "isTerminal", source.IsTerminal);
                array.SetValue(option, i);
            }

            return array;
        }

        private static Array BuildVoiceChanges(DetectiveContentData.VoiceDeltaEntry[] changes)
        {
            Type changeType = typeof(DetectiveDialogueDefinition).GetNestedType("VoiceChange");
            if (changes == null || changes.Length == 0)
            {
                return Array.CreateInstance(changeType, 0);
            }

            var array = Array.CreateInstance(changeType, changes.Length);
            for (int i = 0; i < changes.Length; i++)
            {
                var change = Activator.CreateInstance(changeType);
                SetPublicField(change, "voice", changes[i].Voice);
                SetPublicField(change, "delta", changes[i].Delta);
                array.SetValue(change, i);
            }

            return array;
        }

        private static object BuildVoiceRequirement(DetectiveContentData.VoiceDeltaEntry requirement)
        {
            Type requirementType = typeof(DetectiveDialogueDefinition).GetNestedType("VoiceRequirement");
            var instance = Activator.CreateInstance(requirementType);
            if (requirement.Voice == DetectiveVoiceType.None)
            {
                SetPublicField(instance, "voice", DetectiveVoiceType.None);
                SetPublicField(instance, "minLevel", 0);
            }
            else
            {
                SetPublicField(instance, "voice", requirement.Voice);
                SetPublicField(instance, "minLevel", requirement.Delta);
            }

            return instance;
        }

        private static Array BuildDuelStatements(DetectiveContentData.DuelStatementEntry[] statements)
        {
            Type statementType = typeof(DetectiveDuelDefinition).GetNestedType("DuelStatement");
            if (statements == null || statements.Length == 0)
            {
                return Array.CreateInstance(statementType, 0);
            }

            var array = Array.CreateInstance(statementType, statements.Length);
            for (int i = 0; i < statements.Length; i++)
            {
                DetectiveContentData.DuelStatementEntry source = statements[i];
                var statement = Activator.CreateInstance(statementType);
                SetPublicField(statement, "text", source.Text);
                SetPublicField(statement, "phaseLabel", source.PhaseLabel);
                SetPublicField(statement, "pressHints", BuildDuelPopups(source.PressHints));
                SetPublicField(statement, "reactions", BuildCardReactions(source.Reactions));
                SetPublicField(statement, "skipReply", source.SkipReply);
                array.SetValue(statement, i);
            }

            return array;
        }

        private static Array BuildCardReactions(DetectiveContentData.CardReactionEntry[] reactions)
        {
            Type reactionType = typeof(DetectiveDuelDefinition).GetNestedType("CardReaction");
            if (reactions == null || reactions.Length == 0)
            {
                return Array.CreateInstance(reactionType, 0);
            }

            var array = Array.CreateInstance(reactionType, reactions.Length);
            for (int i = 0; i < reactions.Length; i++)
            {
                DetectiveContentData.CardReactionEntry source = reactions[i];
                var reaction = Activator.CreateInstance(reactionType);
                SetPublicField(reaction, "clueId", source.ClueId);
                SetPublicField(reaction, "reply", source.Reply);
                SetPublicField(reaction, "advance", source.Advance);
                SetPublicField(reaction, "focusDelta", source.FocusDelta);
                SetPublicField(reaction, "voiceChanges", BuildVoiceChanges(source.VoiceChanges));
                SetPublicField(reaction, "setFlag", source.SetFlag);
                SetPublicField(reaction, "requireVoices", BuildVoiceRequirements(source.RequireVoices));
                SetPublicField(reaction, "requireClues", source.RequireClues);
                SetPublicField(reaction, "lockedReply", source.LockedReply);
                array.SetValue(reaction, i);
            }

            return array;
        }

        private static Array BuildVoiceRequirements(DetectiveContentData.VoiceDeltaEntry[] requirements)
        {
            Type requirementType = typeof(DetectiveDialogueDefinition).GetNestedType("VoiceRequirement");
            if (requirements == null || requirements.Length == 0)
            {
                return Array.CreateInstance(requirementType, 0);
            }

            var array = Array.CreateInstance(requirementType, requirements.Length);
            for (int i = 0; i < requirements.Length; i++)
            {
                var requirement = Activator.CreateInstance(requirementType);
                SetPublicField(requirement, "voice", requirements[i].Voice);
                SetPublicField(requirement, "minLevel", requirements[i].Delta);
                array.SetValue(requirement, i);
            }

            return array;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            string leaf = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new InvalidOperationException($"[DetectiveContentGenerator] 字段不存在: {target.GetType().Name}.{name}");
            }

            field.SetValue(target, value);
        }

        private static void SetPublicField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name);
            if (field == null)
            {
                throw new InvalidOperationException($"[DetectiveContentGenerator] 字段不存在: {target.GetType().Name}.{name}");
            }

            field.SetValue(target, value);
        }
    }
}
