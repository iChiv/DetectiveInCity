using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Detective
{
    public static class DetectiveGeneratedArt
    {
        const string Root="Detective/Art/Generated/";
        static readonly Dictionary<string,Texture2D> textures=new();
        public static Texture2D Load(string id)
        {
            if(string.IsNullOrEmpty(id))return null;
            if(textures.TryGetValue(id,out var t)&&t!=null)return t;
            t=Resources.Load<Texture2D>(Root+id);if(t!=null)textures[id]=t;return t;
        }
        public static RawImage Image(Transform parent,string name,string id,Vector2 min,Vector2 max,float opacity=1,bool contain=true)
        {
            var t=parent.Find(name);RawImage image;
            if(t==null){var go=new GameObject(name,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(parent,false);image=go.GetComponent<RawImage>();}
            else image=t.GetComponent<RawImage>();
            image.texture=Load(id);image.raycastTarget=false;image.color=new Color(1,1,1,opacity);image.gameObject.SetActive(image.texture!=null);
            var r=image.rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var aspect=image.GetComponent<AspectRatioFitter>();
            if(contain){if(aspect!=null)aspect.enabled=true;if(aspect==null)aspect=image.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=image.texture!=null?(float)image.texture.width/image.texture.height:1;}
            else if(aspect!=null)aspect.enabled=false;
            return image;
        }
        // A frame supplies the bounds; the nested image fits without stretching or stealing clicks.
        public static RawImage Framed(Transform parent,string name,string id,Vector2 min,Vector2 max,float opacity=1)
        {
            var frame=parent.Find(name) as RectTransform;
            if(frame==null){frame=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();frame.SetParent(parent,false);}
            frame.anchorMin=min;frame.anchorMax=max;frame.offsetMin=frame.offsetMax=Vector2.zero;
            return Image(frame,"Art",id,Vector2.zero,Vector2.one,opacity);
        }
        public static void Backdrop(Transform parent,string id,float opacity=.25f)
        {
            var image=Image(parent,"GeneratedBackdrop",id,Vector2.zero,Vector2.one,opacity,false);image.transform.SetAsFirstSibling();
            var mask=parent.Find("Mask");if(mask!=null)image.transform.SetSiblingIndex(mask.GetSiblingIndex()+1);
        }
        public static string Portrait(string speaker)
        {
            return speaker switch
            {
                "神秘女子"=>"portrait_woman", "酒保"=>"portrait_bartender", "保安"=>"portrait_guard",
                "乞丐"=>"portrait_beggar", "咖啡师"=>"portrait_barista", "瘦猴"=>"portrait_monkey",
                "狙击手"=>"portrait_sniper", "死者妻子"=>"portrait_wife", "风衣男"=>"portrait_trenchcoat",
                "你"=>"portrait_hero", _=>null
            };
        }
        public static string District(string region)
        {
            if(!DetectiveRegionCatalog.TryGetRegion(region,out var info))return null;
            return info.DistrictId switch {"prologue"=>"district_alley","redlight"=>"district_redlight","financial"=>"district_financial","industrial"=>"district_industrial","residential"=>"district_residential",_=>null};
        }
        public static string Evidence(string clue)=>clue switch
        {
            DetectiveClueIds.Badge=>"evidence_badge",DetectiveClueIds.BloodyNote=>"evidence_bloody_note",
            DetectiveClueIds.DoorRecord=>"evidence_door_record",DetectiveClueIds.DiaryThreatened=>"evidence_diary",
            DetectiveClueIds.Recording=>"evidence_recording",DetectiveClueIds.NapkinKiller=>"evidence_napkin",
            DetectiveClueIds.DroppedKey=>"evidence_key",DetectiveClueIds.GateForced=>"evidence_gate",
            DetectiveClueIds.FormerPartners=>"evidence_partners",DetectiveClueIds.LetterWarning=>"evidence_letter",
            DetectiveClueIds.DismissalNotice=>"evidence_dismissal",DetectiveClueIds.VictimNotes=>"evidence_victim_notes",_=>null
        };
    }
}
