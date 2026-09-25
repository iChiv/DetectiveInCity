using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using static Detective.EditorTools.DetectiveCityBuilder;
namespace Detective.EditorTools
{
    public static class DetectiveApartmentStoryInstaller
    {
        public static void Apply(Transform root)
        {
            ResetMaterialCaches();
            var spare=root.Find("SpareKey");if(spare!=null)Object.DestroyImmediate(spare.gameObject);
            var safe=root.Find("Safe");var gate=safe.GetComponent<DetectiveFlagGatedObject>();if(gate!=null)Object.DestroyImmediate(gate);
            if(root.Find("BedroomDoor")!=null)return;
            var door=new GameObject("BedroomDoor");door.transform.SetParent(root,false);door.transform.position=new Vector3(-1,0,-.5f);
            var col=door.AddComponent<BoxCollider>();col.center=new Vector3(0,1.1f,0);col.size=new Vector3(2.4f,2.2f,.18f);
            var obstacle=door.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=col.center;obstacle.size=col.size;obstacle.carving=true;
            door.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild=true;
            var pivot=new GameObject("Hinge");pivot.transform.SetParent(door.transform,false);pivot.transform.localPosition=new Vector3(-1.2f,0,0);
            var panel=CreateCube(pivot.transform,"Panel",pivot.transform.position+new Vector3(1.2f,1.1f,0),new Vector3(2.4f,2.2f,.15f),new Color(.3f,.25f,.2f));Object.DestroyImmediate(panel.GetComponent<Collider>());
            var label=new GameObject("Label").AddComponent<TMPro.TextMeshPro>();label.transform.SetParent(door.transform,false);label.font=DetectiveUIWidgets.GetFont();label.text="卧室门";label.fontSize=4;label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(7,2);label.rectTransform.anchoredPosition3D=new Vector3(0,2.5f,-.12f);label.transform.localScale=Vector3.one*.4f;
            var behavior=door.AddComponent<DetectiveRoomDoor>();var so=new SerializedObject(behavior);so.FindProperty("panel").objectReferenceValue=pivot.transform;so.FindProperty("label").objectReferenceValue=label.gameObject;so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
