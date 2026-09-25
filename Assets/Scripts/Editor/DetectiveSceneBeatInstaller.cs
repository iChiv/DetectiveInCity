using UnityEngine;
using UnityEditor;
using static Detective.EditorTools.DetectiveCityBuilder;
namespace Detective.EditorTools
{
    public static class DetectiveSceneBeatInstaller
    {
        static void Attach(GameObject target,DetectiveSceneBeat.Beat beat,Transform subject,Vector3 destination)
        {
            var c=target.GetComponent<DetectiveSceneBeat>();if(c==null)c=target.AddComponent<DetectiveSceneBeat>();
            var so=new SerializedObject(c);so.FindProperty("beat").enumValueIndex=(int)beat;so.FindProperty("subject").objectReferenceValue=subject;so.FindProperty("destination").vector3Value=destination;so.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void Apply(Transform root,string id)
        {
            ResetMaterialCaches();
            if(id=="D0_Alley")Attach(root.gameObject,DetectiveSceneBeat.Beat.WakeUp,root,Vector3.zero);
            if(id=="I2_Apartment")
            {
                var guard=root.Find("Guard");Attach(guard.gameObject,DetectiveSceneBeat.Beat.GuardStepsAside,guard,new Vector3(-4.5f,1,-3.5f));
                var cloth=root.Find("EvidenceCloth");
                if(cloth==null){var go=CreateCube(root,"EvidenceCloth",new Vector3(-1.2f,.35f,1.4f),new Vector3(1.9f,.025f,.85f),new Color(.83f,.84f,.85f));Object.DestroyImmediate(go.GetComponent<Collider>());go.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild=true;cloth=go.transform;}
                Attach(cloth.gameObject,DetectiveSceneBeat.Beat.CorpseCloth,cloth,Vector3.zero);
            }
            if(id=="I3_Rooftop")
            {
                var sniper=root.Find("Sniper");if(sniper.Find("Rifle")!=null)return;
                var gun=CreateCube(sniper,"Rifle",sniper.position+new Vector3(.35f,.25f,-.4f),new Vector3(.13f,.14f,1.1f),new Color(.08f,.09f,.1f));Object.DestroyImmediate(gun.GetComponent<Collider>());gun.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild=true;
            }
        }
    }
}
