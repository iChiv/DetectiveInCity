using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    // Shared, scrollable reading/password surface; created once on the persistent Core canvas.
    public sealed class DetectiveInspectionUI : MonoBehaviour
    {
        static DetectiveInspectionUI instance;
        public static bool IsShowing => instance != null && instance.window != null && instance.window.activeSelf;
        public static int LastClosedFrame { get; private set; } = -1;
        GameObject window;
        TextMeshProUGUI title, body, feedback;
        TMP_InputField code;
        Button submit;
        Action onSuccess;
        string expectedCode;
        bool previousBlock;

        public static DetectiveInspectionUI Get()
        {
            if (instance != null) return instance;
            var loader = DetectiveRegionLoader.Instance;
            if (loader == null) return null;
            instance = loader.GetComponent<DetectiveInspectionUI>();
            if (instance == null) instance = loader.gameObject.AddComponent<DetectiveInspectionUI>();
            instance.Build();
            return instance;
        }

        void Build()
        {
            if (window != null) return;
            var go = new GameObject("InspectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            window = DetectiveUIWidgets.CreateImage(go.transform,"InspectionWindow",new Color(0,0,0,.8f)).gameObject;
            DetectiveUIWidgets.Stretch((RectTransform)window.transform,0,0,0,0);
            var panel=DetectiveUIWidgets.CreateImage(window.transform,"Panel",new Color(.07f,.085f,.10f,1));
            var rect=panel.rectTransform;rect.anchorMin=new(.16f,.14f);rect.anchorMax=new(.84f,.86f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            title=DetectiveUIWidgets.CreateText(panel.transform,"Title","",34,new Color(1,.86f,.5f));
            Place(title.rectTransform,new(0,1),new(1,1),new(28,-88),new(-28,-22));
            var viewport=DetectiveUIWidgets.CreateImage(panel.transform,"Viewport",new Color(0,0,0,0));
            Place(viewport.rectTransform,new(0,0),new(1,1),new(32,200),new(-32,-106));viewport.gameObject.AddComponent<RectMask2D>();
            body=DetectiveUIWidgets.CreateText(viewport.transform,"Content","",27,Color.white);
            body.textWrappingMode=TextWrappingModes.Normal;body.overflowMode=TextOverflowModes.Overflow;
            body.rectTransform.anchorMin=new(0,1);body.rectTransform.anchorMax=Vector2.one;body.rectTransform.pivot=new(.5f,1);body.rectTransform.anchoredPosition=Vector2.zero;body.rectTransform.sizeDelta=Vector2.zero;
            var fit=body.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=body.rectTransform;scroll.horizontal=false;scroll.scrollSensitivity=32;
            var input=DetectiveUIWidgets.CreateImage(panel.transform,"Password",new Color(.16f,.18f,.21f));
            Place(input.rectTransform,new(.5f,0),new(.5f,0),new(-180,128),new(180,185));
            code=input.gameObject.AddComponent<TMP_InputField>();
            var inputText=DetectiveUIWidgets.CreateText(input.transform,"Value","",32,Color.white);DetectiveUIWidgets.Stretch(inputText.rectTransform,16,16,6,6);inputText.alignment=TextAlignmentOptions.Center;
            code.textViewport=input.rectTransform;code.textComponent=inputText;code.contentType=TMP_InputField.ContentType.IntegerNumber;code.characterLimit=4;code.lineType=TMP_InputField.LineType.SingleLine;
            feedback=DetectiveUIWidgets.CreateText(panel.transform,"Feedback","",23,new Color(1,.7f,.5f));
            Place(feedback.rectTransform,new(0,0),new(1,0),new(28,83),new(-28,126));feedback.alignment=TextAlignmentOptions.Center;
            submit=Button(panel.transform,"Submit","确认密码",new(-245,22),new(-15,77));submit.onClick.AddListener(()=>SubmitCode(code.text));
            var close=Button(panel.transform,"Close","关闭",new(15,22),new(245,77));close.onClick.AddListener(Close);
            code.onSubmit.AddListener(SubmitCode);window.SetActive(false);
        }
        static void Place(RectTransform r,Vector2 min,Vector2 max,Vector2 bottom,Vector2 top){r.anchorMin=min;r.anchorMax=max;r.offsetMin=bottom;r.offsetMax=top;}
        static Button Button(Transform parent,string name,string label,Vector2 min,Vector2 max)
        {
            var img=DetectiveUIWidgets.CreateImage(parent,name,new Color(.23f,.28f,.32f));Place(img.rectTransform,new(.5f,0),new(.5f,0),min,max);
            var b=img.gameObject.AddComponent<Button>();var t=DetectiveUIWidgets.CreateText(img.transform,"Text",label,25,Color.white);DetectiveUIWidgets.Stretch(t.rectTransform,8,8,4,4);t.alignment=TextAlignmentOptions.Center;return b;
        }
        public void Read(string heading,string text,string artId = null)
        {
            Open(heading,text);expectedCode=null;onSuccess=null;code.gameObject.SetActive(false);submit.gameObject.SetActive(false);
            var parent = title.transform.parent;
            var art=DetectiveGeneratedArt.Framed(parent,"EvidenceArtwork",artId,new Vector2(.025f,.25f),new Vector2(.39f,.82f));
            bool hasArt=art.texture!=null;art.transform.parent.gameObject.SetActive(hasArt);
            var viewport=(RectTransform)body.transform.parent;
            viewport.anchorMin=new Vector2(hasArt ? .42f : 0,0);viewport.anchorMax=Vector2.one;
            viewport.offsetMin=new Vector2(32,200);viewport.offsetMax=new Vector2(-32,-106);

        }
        public void Password(string heading,string text,string expected,Action success)
        {
            Open(heading,text);
            var old=title.transform.parent.Find("EvidenceArtwork");if(old!=null)old.gameObject.SetActive(false);
            var viewport=(RectTransform)body.transform.parent;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(32,200);viewport.offsetMax=new Vector2(-32,-106);
            expectedCode=expected;onSuccess=success;code.gameObject.SetActive(true);submit.gameObject.SetActive(true);code.text="";code.ActivateInputField();
        }
        void Open(string heading,string text)
        {
            if (!IsShowing) previousBlock=InteractionController.DialogueBlock;
            title.text=heading;body.text=text;feedback.text="";body.rectTransform.anchoredPosition=Vector2.zero;
            window.SetActive(true);InteractionController.DialogueBlock=true;
            foreach(var mover in FindObjectsByType<DetectiveClickMover>(FindObjectsSortMode.None))mover.Stop();
        }
        public void SubmitCode(string value)
        {
            if (!IsShowing || expectedCode==null) return;
            if(value!=expectedCode){feedback.text="密码不正确。可以查看已收集的线索后再试。";code.text="";code.ActivateInputField();return;}
            var success=onSuccess;onSuccess=null;Close();success?.Invoke();
        }
        public void Close()
        {
            if (!IsShowing)return;
            window.SetActive(false);LastClosedFrame=Time.frameCount;onSuccess=null;expectedCode=null;
            InteractionController.DialogueBlock=previousBlock;
        }
        void Update()
        {
            if(IsShowing && Keyboard.current!=null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))Close();
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
