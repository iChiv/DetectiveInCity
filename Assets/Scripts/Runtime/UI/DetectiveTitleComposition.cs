using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    // Menu C: keep the badge/paper on the right clear, use the dark left wall for navigation.
    [ExecuteAlways]
    public sealed class DetectiveTitleComposition : MonoBehaviour
    {
        RectTransform background;
        RectTransform panel;
        Vector2 lastSize;

        void OnEnable(){Apply();}
        public void Apply()
        {
            panel=transform as RectTransform;if(panel==null)return;
            var texture=Resources.Load<Texture2D>("Detective/Art/MenuBackground");if(texture==null)return;
            var existing=transform.Find("MenuArtwork");
            RawImage artwork;
            if(existing==null){var go=new GameObject("MenuArtwork",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(transform,false);artwork=go.GetComponent<RawImage>();}
            else artwork=existing.GetComponent<RawImage>();
            artwork.texture=texture;artwork.raycastTarget=false;artwork.color=Color.white;
            background=artwork.rectTransform;background.SetAsFirstSibling();background.anchorMin=background.anchorMax=new Vector2(.5f,.5f);background.pivot=new(.5f,.5f);background.anchoredPosition=Vector2.zero;
            var title=transform.Find("TitleText")?.GetComponent<TMP_Text>();
            if(title!=null){Layout(title.rectTransform,.075f,.32f,.69f,.86f);title.alignment=TextAlignmentOptions.TopLeft;title.fontSize=76;title.enableAutoSizing=true;title.fontSizeMin=35;title.fontSizeMax=76;title.color=new Color(.94f,.88f,.74f);title.fontStyle=FontStyles.Bold;title.textWrappingMode=TextWrappingModes.Normal;}
            var subtitle=transform.Find("SubtitleText")?.GetComponent<TMP_Text>();
            if(subtitle!=null){Layout(subtitle.rectTransform,.078f,.35f,.60f,.68f);subtitle.alignment=TextAlignmentOptions.TopLeft;subtitle.fontSize=23;subtitle.enableAutoSizing=true;subtitle.fontSizeMin=15;subtitle.fontSizeMax=23;subtitle.color=new Color(.73f,.76f,.77f);}
            Button("StartButton",.435f,.515f,true);
            Button("HelpButton",.335f,.415f,false);
            Button("QuitButton",.235f,.315f,false);
            var help=transform.Find("HelpText")?.GetComponent<TMP_Text>();
            if(help!=null)
            {
                Layout(help.rectTransform,.40f,.92f,.43f,.85f);help.fontSize=26;help.enableAutoSizing=true;help.fontSizeMin=18;help.fontSizeMax=26;help.alignment=TextAlignmentOptions.TopLeft;help.color=new Color(.92f,.93f,.94f);
                help.text="操作说明\n\n点击地面移动，点击人物或物件会靠近交互。\nTab：思维面板　单击线索读全文，拖入槽位推理。\nQ / E：旋转镜头　W：回中\nM：地图　Esc：暂停\n\n对峙准备时可检查线索或暂不对峙；正式开始后不能撤退。";
                var shadeTransform=transform.Find("HelpBackdrop");
                var shade=shadeTransform!=null?shadeTransform.GetComponent<Image>():DetectiveUIWidgets.CreateImage(transform,"HelpBackdrop",new Color(.025f,.035f,.05f,.94f));
                Layout(shade.rectTransform,.40f,.92f,.43f,.85f);shade.raycastTarget=false;shade.transform.SetAsLastSibling();help.transform.SetAsLastSibling();shade.gameObject.SetActive(help.gameObject.activeSelf);help.margin=new Vector4(25,22,25,22);
            }
            Resize();
        }
        void Button(string name,float bottom,float top,bool primary)
        {
            var t=transform.Find(name) as RectTransform;if(t==null)return;Layout(t,.078f,.30f,bottom,top);
            var b=t.GetComponent<UnityEngine.UI.Button>();var img=t.GetComponent<Image>();if(img!=null){img.color=primary?new Color(.15f,.13f,.105f,.92f):new Color(.035f,.055f,.075f,.85f);img.raycastTarget=true;}
            var label=t.GetComponentInChildren<TMP_Text>();if(label!=null){label.alignment=TextAlignmentOptions.MidlineLeft;label.margin=new Vector4(26,0,10,0);label.fontSize=29;label.enableAutoSizing=true;label.fontSizeMin=18;label.fontSizeMax=29;label.color=primary?new Color(1,.87f,.60f):new Color(.84f,.86f,.88f);}
            if(b!=null){var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.35f,1.25f,1.1f);colors.pressedColor=new Color(.75f,.7f,.6f);b.colors=colors;}
        }
        static void Layout(RectTransform r,float left,float right,float bottom,float top){r.anchorMin=new(left,bottom);r.anchorMax=new(right,top);r.pivot=new(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;}
        void LateUpdate()
        {
            if(panel!=null && panel.rect.size!=lastSize)Resize();
            var help=transform.Find("HelpText");var shade=transform.Find("HelpBackdrop");
            if(help!=null&&shade!=null&&shade.gameObject.activeSelf!=help.gameObject.activeSelf)shade.gameObject.SetActive(help.gameObject.activeSelf);
        }
        void Resize()
        {
            if(background==null || panel==null)return;lastSize=panel.rect.size;
            float factor=Mathf.Max(lastSize.x/1280f,lastSize.y/720f);
            background.sizeDelta=new Vector2(1280,720)*factor;
        }
    }
}
