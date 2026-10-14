using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace ScheduleISpanish;

internal sealed class UpdateNotice : IDisposable
{
    private const string LabelName="ScheduleTranslate.UpdateLabel";
    private GameObject? root;
    private Texture2D? texture;
    private Sprite? rounded;
    private Texture2D? borderTexture;
    private Sprite? borderSprite;
    private TextMeshProUGUI? detailLabel,buttonLabel;
    private Button? updateButton;
    internal static bool IsLabel(TMP_Text label)=>label.gameObject.name==LabelName;
    private static readonly Color Ink=new(0.96f,0.97f,0.99f,1);
    private static readonly Color Muted=new(0.75f,0.78f,0.82f,1);
    private static readonly Color Blue=new(0.13f,0.48f,0.91f,1);

    internal static UpdateNotice Create(string version,bool spanish,UpdateSettings settings,Action update,Action dismiss)
    {
        var notice=new UpdateNotice();
        try { notice.Build(version,spanish,settings,update,dismiss); return notice; }
        catch { notice.Dispose(); throw; }
    }

    private void Build(string version,bool spanish,UpdateSettings settings,Action update,Action dismiss)
    {
        root=new GameObject("ScheduleTranslate.UpdateNotice");
        Object.DontDestroyOnLoad(root);
        var canvas=root.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=3000;
        var scaler=root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        root.AddComponent<GraphicRaycaster>();
        rounded=MakeRoundedSprite();

        var shadow=Panel(root.transform,"Shadow",new Color(0,0,0,0.08f));
        PositionCard(shadow,new Vector2(-26,25));
        var card=Panel(root.transform,"Card",new Color(0.06f,0.075f,0.095f,settings.NoticeOpacity));
        // Empty panel space passes clicks through; only the two buttons receive input.
        PositionCard(card,new Vector2(-28,28));
        borderSprite=MakeRoundedSprite(true);
        var border=Panel(root.transform,"Border",new Color(0.85f,0.89f,0.95f,0.25f));
        border.GetComponent<Image>().sprite=borderSprite;
        PositionCard(border,new Vector2(-28,28));
        var accent=Panel(card,"Accent",new Color(0.16f,0.65f,1,0.95f));
        Bounds(accent,0,12,3,142);
        Label(card,"SCHEDULE I TRANSLATE",22,124,300,22,11,Muted,FontStyles.Normal);
        Label(card,spanish ? "Nueva actualización" : "New update available",22,91,348,30,22,Ink,FontStyles.Bold);
        string detail=version.Contains("TEST",StringComparison.Ordinal) ?
            (spanish ? "Versión 1.4.7 · Vista previa" : "Version 1.4.7 · Preview") :
            (spanish ? "Versión " : "Version ")+version+(spanish ? " disponible" : " available");
        detailLabel=Label(card,detail,22,66,354,24,15,Muted);
        var download=Panel(card,"Download",Blue);
        Bounds(download,22,18,218,36);
        updateButton=AddButton(download,update);
        buttonLabel=Label(download,settings.AllowInGameUpdates ? (spanish ? "Actualizar" : "Update") :
            (spanish ? "Ver descarga" : "View download"),0,0,218,36,15,Color.white,FontStyles.Bold,TextAlignmentOptions.Center);
        var close=Panel(card,"Close",new Color(1,1,1,0));
        Bounds(close,354,122,34,34);
        AddButton(close,()=> { dismiss(); Dispose(); });
        Label(close,"×",0,0,34,34,22,Muted,FontStyles.Normal,TextAlignmentOptions.Center);
        foreach(var rect in new[]{shadow,card,border}) rect.localScale=new Vector3(settings.NoticeScale,settings.NoticeScale,1);

    }

    private RectTransform Panel(Transform parent,string name,Color color)
    {
        var node=new GameObject(name);
        node.transform.SetParent(parent,false);
        var image=node.AddComponent<Image>();
        image.sprite=rounded;
        image.type=Image.Type.Sliced;
        image.color=color;
        image.raycastTarget=false;
        return image.rectTransform;
    }

    private static void PositionCard(RectTransform rect,Vector2 position)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(1,0);
        rect.pivot=new Vector2(1,0);
        rect.anchoredPosition=position;
        rect.sizeDelta=new Vector2(400,166);
    }

    private static void Bounds(RectTransform rect,float x,float y,float width,float height)
    {
        rect.anchorMin=rect.anchorMax=Vector2.zero;
        rect.pivot=Vector2.zero;
        rect.anchoredPosition=new Vector2(x,y);
        rect.sizeDelta=new Vector2(width,height);
    }

    private static TextMeshProUGUI Label(Transform parent,string text,float x,float y,float width,float height,float size,Color color,
        FontStyles style=FontStyles.Normal,TextAlignmentOptions alignment=TextAlignmentOptions.Left)
    {
        var node=new GameObject(LabelName);
        node.transform.SetParent(parent,false);
        var label=node.AddComponent<TextMeshProUGUI>();
        label.font=TMP_Settings.defaultFontAsset;
        Bounds(label.rectTransform,x,y,width,height);
        label.fontSize=label.fontSizeMax=size;
        label.fontSizeMin=size*0.8f;
        label.enableAutoSizing=true;
        label.enableWordWrapping=false;
        label.overflowMode=TextOverflowModes.Ellipsis;
        label.alignment=alignment;
        label.fontStyle=style;
        label.color=color;
        label.raycastTarget=false;
        label.richText=false;
        label.text=text;
        return label;
    }

    private static Button AddButton(RectTransform rect,Action click)
    {
        var image=rect.GetComponent<Image>();
        image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();
        button.targetGraphic=image;
        var colors=button.colors;
        // Tint the existing colour; keep transitions consistent with native buttons.
        colors.normalColor=Color.white;
        colors.highlightedColor=new Color(0.91f,0.96f,1,1);
        colors.pressedColor=new Color(0.76f,0.85f,0.92f,1);
        colors.selectedColor=Color.white;
        colors.fadeDuration=0.1f;
        button.colors=colors;
        var navigation=button.navigation;
        navigation.mode=Navigation.Mode.None;
        button.navigation=navigation;
        button.onClick.AddListener((UnityAction)click);
        return button;
    }

    internal void Status(string detail,string button,bool enabled)
    {
        if(detailLabel!=null) detailLabel.text=detail;
        if(buttonLabel!=null) buttonLabel.text=button;
        if(updateButton!=null) updateButton.interactable=enabled;
    }

    private Sprite MakeRoundedSprite(bool outline=false)
    {
        const int side=32;
        const float radius=8;
        var created=new Texture2D(side,side,TextureFormat.RGBA32,false);
        if(outline) borderTexture=created; else texture=created;
        created.filterMode=FilterMode.Bilinear;
        created.wrapMode=TextureWrapMode.Clamp;
        var pixels=new Color[side*side];
        for(int y=0;y<side;y++)
        for(int x=0;x<side;x++)
        {
            float qx=Math.Abs(x+0.5f-side/2f)-(side/2f-radius);
            float dx=Math.Max(0,qx);
            float qy=Math.Abs(y+0.5f-side/2f)-(side/2f-radius);
            float dy=Math.Max(0,qy);
            float distance=MathF.Sqrt(dx*dx+dy*dy)+Math.Min(Math.Max(qx,qy),0)-radius;
            float alpha=Math.Clamp(0.5f-distance,0,1);
            if(outline) alpha*=1-Math.Clamp(-distance-0.5f,0,1);
            pixels[y*side+x]=new Color(1,1,1,alpha);
        }
        created.SetPixels(pixels);
        created.Apply();
        return Sprite.Create(created,new Rect(0,0,side,side),new Vector2(0.5f,0.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
    }

    public void Dispose()
    {
        if(root!=null) Object.Destroy(root);
        if(rounded!=null) Object.Destroy(rounded);
        if(texture!=null) Object.Destroy(texture);
        if(borderSprite!=null) Object.Destroy(borderSprite);
        if(borderTexture!=null) Object.Destroy(borderTexture);
        root=null; rounded=null; texture=null; borderSprite=null; borderTexture=null;
    }
}
