using UnityEngine;
using UnityEditor;
using static Detective.EditorTools.DetectiveCityBuilder;
namespace Detective.EditorTools
{
    public static class DetectiveHomeStoryInstaller
    {
        static void Set(Object obj,string field,Object value){var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Part(Transform parent,string name,Vector3 local,Vector3 size,Color color)
        {
            var p=CreateCube(parent,name,parent.position+local,size,color);Object.DestroyImmediate(p.GetComponent<Collider>());
            p.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild=true;
        }
        public static void Apply(Transform root)
        {
            ResetMaterialCaches();
            var note=root.Find("LeavingHome");if(note!=null)note.gameObject.SetActive(false);
            var exit=root.Find("ExitDoor").GetComponent<DetectiveDoorTeleport>();Set(exit,"leavingDialogue",AssetDatabase.LoadAssetAtPath<DetectiveDialogueDefinition>("Assets/Data/Dialogues/dlg_leaving_home.asset"));
            if(root.Find("WaitingTrenchcoatMan")!=null)return;
            var actor=new GameObject("WaitingTrenchcoatMan");actor.transform.SetParent(root,false);actor.transform.position=new Vector3(.5f,0,.8f);
            var visual=new GameObject("Visual");visual.transform.SetParent(actor.transform,false);
            Color coat=new(.13f,.15f,.18f);
            Part(visual.transform,"Chair",new(0,.38f,0),new(.8f,.75f,.8f),new(.3f,.23f,.16f));
            Part(visual.transform,"Coat",new(0,1.05f,0),new(.65f,.8f,.4f),coat);
            Part(visual.transform,"Head",new(0,1.65f,-.04f),new(.35f,.38f,.35f),new(.62f,.53f,.46f));
            Part(visual.transform,"LeftLeg",new(-.2f,.4f,-.45f),new(.2f,.7f,.35f),coat);
            Part(visual.transform,"RightLeg",new(.2f,.4f,-.45f),new(.2f,.7f,.35f),coat);
            Part(visual.transform,"LeftArm",new(-.38f,1,-.15f),new(.18f,.55f,.25f),coat);
            Part(visual.transform,"RightArm",new(.38f,1,-.15f),new(.18f,.55f,.25f),coat);
            var col=actor.AddComponent<BoxCollider>();col.center=new Vector3(0,1,0);col.size=new Vector3(.9f,2,1.2f);
            actor.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild=true;
            var npc=actor.AddComponent<DetectiveWaitingOpponent>();Set(npc,"trigger",root.GetComponentInChildren<DetectiveFinalDuelTrigger>());Set(npc,"visual",visual);
            var label=new GameObject("Name").AddComponent<TMPro.TextMeshPro>();label.transform.SetParent(visual.transform,false);label.font=DetectiveUIWidgets.GetFont();label.text="风衣男";label.fontSize=4;label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(7,2);label.rectTransform.anchoredPosition3D=new Vector3(0,2.2f,0);label.transform.localScale=Vector3.one*.4f;label.outlineWidth=.2f;label.outlineColor=Color.black;
            visual.SetActive(false);col.enabled=false;
        }
    }
}
