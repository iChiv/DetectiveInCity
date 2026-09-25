using UnityEngine;
namespace Detective
{
    public static class DetectiveEndingTableau
    {
        public static void Show(Transform parent,EndingType ending)
        {
            string id=ending switch{EndingType.A_PerfectTruth=>"ending_a",EndingType.B_Justice=>"ending_b",EndingType.C_StreetExecution=>"ending_c",EndingType.D_YouAreKiller=>"ending_d",_=>"ending_e"};
            var image=DetectiveGeneratedArt.Framed(parent,"EndingTableau",id,new Vector2(.14f,.36f),new Vector2(.86f,.94f));
            image.transform.parent.gameObject.SetActive(true);
            // The existing final caption remains independent and readable below the artwork.
            var caption=parent.Find("FinalImageText") as RectTransform;
            if(caption!=null){caption.anchorMin=new Vector2(.12f,.15f);caption.anchorMax=new Vector2(.88f,.31f);caption.offsetMin=caption.offsetMax=Vector2.zero;}
        }
        public static void Hide(Transform parent){var t=parent.Find("EndingTableau");if(t!=null)t.gameObject.SetActive(false);}
    }
}
