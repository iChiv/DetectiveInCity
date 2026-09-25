using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Detective.EditorTools
{
    public static class DetectiveStoryAssetInstaller
    {
        static readonly string[] RecipeNames={"recipe_erased_camera","recipe_partner_history","recipe_failed_mission","recipe_sleeping_pills","recipe_industrial_route","recipe_letter_partner"};
        public static string Preview() => string.Join(", ",DetectiveContentData.Recipes.Where(e=>RecipeNames.Contains(e.FileName)).Select(e=>e.FileName));
        public static void ApplyCluesAndRecipes()
        {
            foreach(var entry in DetectiveContentData.Clues.Where(e=>new[]{DetectiveClueIds.DiaryThreatened,DetectiveClueIds.LetterWarning,DetectiveClueIds.YourDiaryDoubt}.Contains(e.Id)))
            {
                var asset=AssetDatabase.LoadAssetAtPath<DetectiveClueDefinition>("Assets/Resources/Detective/Clues/"+entry.Id+".asset");
                var so=new SerializedObject(asset);so.FindProperty("description").stringValue=entry.Description;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
            }
            foreach(var entry in DetectiveContentData.Recipes.Where(e=>RecipeNames.Contains(e.FileName)))
            {
                string path="Assets/Resources/Detective/Recipes/"+entry.FileName+".asset";
                var asset=AssetDatabase.LoadAssetAtPath<DetectiveReasoningRecipe>(path);if(asset!=null)continue;
                asset=ScriptableObject.CreateInstance<DetectiveReasoningRecipe>();AssetDatabase.CreateAsset(asset,path);
                var so=new SerializedObject(asset);so.FindProperty("recipeId").stringValue=entry.RecipeId;
                var ids=so.FindProperty("requiredClueIds");ids.arraySize=entry.RequiredClueIds.Length;for(int i=0;i<ids.arraySize;i++)ids.GetArrayElementAtIndex(i).stringValue=entry.RequiredClueIds[i];
                so.FindProperty("resultText").stringValue=entry.ResultText;so.FindProperty("onceOnly").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
        }
        public static void ApplyDialogueText()
        {
            string[] affected={"dlg_mysterious_woman","dlg_guard","dlg_beggar","dlg_monkey","dlg_wife","dlg_leaving_home"};
            foreach(var entry in DetectiveContentData.Dialogues.Where(d=>affected.Contains(d.FileName)))
            {
                var asset=AssetDatabase.LoadAssetAtPath<DetectiveDialogueDefinition>("Assets/Data/Dialogues/"+entry.FileName+".asset");var so=new SerializedObject(asset);var options=so.FindProperty("options");
                if(options.arraySize!=entry.Options.Length)throw new InvalidOperationException("Option count mismatch: "+entry.FileName);
                for(int i=0;i<options.arraySize;i++)
                {
                    var option=options.GetArrayElementAtIndex(i);var lines=option.FindPropertyRelative("resultLines");var source=entry.Options[i].ResultLines??Array.Empty<string>();
                    lines.arraySize=source.Length;for(int j=0;j<source.Length;j++)lines.GetArrayElementAtIndex(j).stringValue=source[j];
                    if(entry.FileName=="dlg_leaving_home" && i==0){var flags=option.FindPropertyRelative("setFlags");flags.arraySize=1;flags.GetArrayElementAtIndex(0).stringValue="home_exit_selected";}
                }
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
            }
            var duel=AssetDatabase.LoadAssetAtPath<DetectiveDuelDefinition>("Assets/Data/Duels/duel_trenchcoat_gate.asset");var ds=new SerializedObject(duel);ds.FindProperty("winResultLines").GetArrayElementAtIndex(0).stringValue="风衣男崩溃地后退，转身逃进雨里。";ds.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(duel);AssetDatabase.SaveAssets();
            // Unity serialization removes fields that no longer exist in the checked C# definitions.
            AssetDatabase.ForceReserializeAssets(affected.Select(n=>"Assets/Data/Dialogues/"+n+".asset").Concat(new[]{"Assets/Data/Duels/duel_trenchcoat_gate.asset"}));
        }
    }
}
