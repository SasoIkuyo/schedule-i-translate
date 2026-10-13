using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleISpanish;

// Keep the game's Text component for references, layout measurement and callbacks.
// A child renders its content using the game's existing SDF font.
internal static class PhoneText
{
    internal static bool Enabled=true;
    private const string ChildName="ScheduleTranslate.PhoneText";
    private static readonly Dictionary<int,TextMeshProUGUI> mirrors=new();
    private static int errors;
    internal static bool IsMirror(TMP_Text label) => label.gameObject.name==ChildName;

    internal static bool IsPhone(Text label)
    {
        if(label.canvas==null || label.canvas.renderMode!=RenderMode.WorldSpace) return false;
        // Quantity buttons use a special oversized font with intentional overflow.
        // The SDF mirror's autosize floor and ellipsis clip these 40x40 glyphs.
        // Preserve the native Text mesh, font and overflow on symbol buttons.
        if((label.text.Trim() is "+" or "-" or "−") && label.GetComponentInParent<Button>()!=null) return false;
        // Editable fields need the original glyph/caret geometry maintained by InputField.
        if(label.GetComponentInParent<InputField>()!=null || label.GetComponentInParent<Dropdown>()!=null || label.GetComponentInParent<Dropdown.DropdownItem>()!=null) return false;
        var parent=label.transform;
        for(int i=0;i<20 && parent!=null;i++,parent=parent.parent)
        {
            // Keep the counteroffer's amount selectors and product preview native.
            // Their widths, editing and immediate refresh are controlled by the game.
            if(parent.name=="CounterofferInterface") return false;
            // Dropdown instantiates its template under a detached Dropdown List canvas.
            if(parent.name=="Dropdown List") return false;
            if(parent.name=="AppsCanvas") return true;
        }
        return false;
    }

    internal static void Prepare(Text label)
    {
        if(!Enabled || !label.isActiveAndEnabled) return;
        try
        {
            if(!IsPhone(label))
            {
                // A clone can move from a mirrored template into a native control.
                var stale=label.transform.Find(ChildName);
                if(stale!=null && stale.gameObject.activeSelf) { stale.gameObject.SetActive(false); label.SetVerticesDirty(); }
                mirrors.Remove(label.GetInstanceID());
                return;
            }
            int id=label.GetInstanceID();
            if(mirrors.TryGetValue(id,out var existing) && existing!=null) { existing.gameObject.SetActive(true); existing.enabled=true; Sync(label,existing); return; }
            if(TMP_Settings.defaultFontAsset==null) return; // Original font remains visible.
            if(mirrors.Count>=512)
            {
                foreach(var key in mirrors.Where(p=>p.Value==null).Select(p=>p.Key).ToArray()) mirrors.Remove(key);
                if(mirrors.Count>=512) return;
            }
            // Unity clones children with their prefab. Reuse that visual child instead
            // of leaving an old template label underneath a second mirror.
            var inherited=label.transform.Find(ChildName);
            var mirror=inherited==null ? null : inherited.GetComponent<TextMeshProUGUI>();
            if(mirror==null)
            {
                var child=new GameObject(ChildName);
                child.layer=label.gameObject.layer;
                child.transform.SetParent(label.transform,false);
                mirror=child.AddComponent<TextMeshProUGUI>();
                // Rendering child must not participate in the parent's layout group.
                child.AddComponent<LayoutElement>().ignoreLayout=true;
            }
            mirror.font=TMP_Settings.defaultFontAsset;
            mirror.raycastTarget=false;
            mirror.maskable=label.maskable;
            mirror.rectTransform.anchorMin=Vector2.zero;
            mirror.rectTransform.anchorMax=Vector2.one;
            mirror.rectTransform.offsetMin=mirror.rectTransform.offsetMax=Vector2.zero;
            mirrors[id]=mirror;
            mirror.gameObject.SetActive(true);
            Sync(label,mirror);
        }
        catch(Exception ex) { if(errors++<3) MelonLoader.MelonLogger.Warning("Phone font fallback: "+ex.Message); }
    }

    internal static void Populate(Text __instance,VertexHelper __0)
    {
        if(!Enabled) return;
        try
        {
            if(!mirrors.TryGetValue(__instance.GetInstanceID(),out var mirror) || mirror==null) return;
            if(!IsPhone(__instance)) { Prepare(__instance); return; }
            Sync(__instance,mirror);
            // Clear only after the replacement is ready. No duplicate text meshes.
            if(mirror.isActiveAndEnabled) __0.Clear();
        }
        catch(Exception ex) { if(errors++<3) MelonLoader.MelonLogger.Warning("Phone text: "+ex.Message); }
    }

    internal static void TextChanged(Text __instance)
    {
        if(!Enabled) return;
        try
        {
            // Run after Text.text has committed the new value, before canvas rebuilding.
            // Numeric fields are just as important as translated labels here.
            if(mirrors.TryGetValue(__instance.GetInstanceID(),out var mirror) && mirror!=null)
            {
                if(!IsPhone(__instance)) { Prepare(__instance); return; }
                Sync(__instance,mirror);
            }
        }
        catch(Exception ex) { if(errors++<3) MelonLoader.MelonLogger.Warning("Phone value update: "+ex.Message); }
    }

    internal static void Disable(Text __instance)
    {
        if(mirrors.TryGetValue(__instance.GetInstanceID(),out var mirror) && mirror!=null) mirror.enabled=false;
    }

    private static void Sync(Text label,TextMeshProUGUI mirror)
    {
        // Leave scripts unsupported by the SDF font to the original dynamic font.
        mirror.enabled=label.enabled && label.text.Where(c=>c>127 && !char.IsWhiteSpace(c)).Distinct().All(c=>mirror.font.HasCharacter(c,true,false));
        mirror.text=PhoneWording.Compact(label.text);
        mirror.color=label.color;
        mirror.richText=label.supportRichText;
        mirror.fontStyle=label.fontStyle switch {
            FontStyle.Bold=>FontStyles.Bold, FontStyle.Italic=>FontStyles.Italic,
            FontStyle.BoldAndItalic=>FontStyles.Bold|FontStyles.Italic, _=>FontStyles.Normal};
        mirror.alignment=label.alignment switch {
            TextAnchor.UpperLeft=>TextAlignmentOptions.TopLeft,TextAnchor.UpperCenter=>TextAlignmentOptions.Top,
            TextAnchor.UpperRight=>TextAlignmentOptions.TopRight,TextAnchor.MiddleLeft=>TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter=>TextAlignmentOptions.Center,TextAnchor.MiddleRight=>TextAlignmentOptions.Right,
            TextAnchor.LowerLeft=>TextAlignmentOptions.BottomLeft,TextAnchor.LowerCenter=>TextAlignmentOptions.Bottom,
            _=>TextAlignmentOptions.BottomRight};
        mirror.fontSize=label.fontSize;
        mirror.fontSizeMax=label.fontSize;
        mirror.fontSizeMin=Math.Min(label.fontSize,Math.Max(12,label.fontSize*0.8f));
        mirror.enableAutoSizing=true;
        mirror.enableWordWrapping=label.horizontalOverflow==HorizontalWrapMode.Wrap;
        mirror.overflowMode=TextOverflowModes.Ellipsis;
        mirror.margin=Vector4.zero;
        FitOrderPair(label,mirror);
    }

    private static void FitOrderPair(Text label,TextMeshProUGUI mirror)
    {
        var group=label.transform;
        if(group.name=="Amount") group=group.parent;
        if(group==null || group.parent==null || group.parent.name!="Content" ||
           (group.name!="Total" && group.name!="Debt" && group.name!="Limit" && group.name!="ItemLimit")) return;
        var content=group.parent;
        // Restrict the change to the supplier's order popup.
        if(content.parent==null || content.parent.name!="Shade" || content.parent.parent==null || content.parent.parent.name!="ShopInterface") return;
        var caption=group.GetComponent<Text>();
        var valueTransform=group.Find("Amount");
        var value=valueTransform==null ? null : valueTransform.GetComponent<Text>();
        if(caption==null || value==null) return;
        float width=caption.rectTransform.rect.width;
        if(width<=0) return;
        var rect=value.rectTransform;
        rect.anchorMin=new Vector2(0.60f,0);
        rect.anchorMax=Vector2.one;
        rect.offsetMin=new Vector2(6,0); rect.offsetMax=Vector2.zero;
        value.alignment=TextAnchor.MiddleRight;
        if(label.GetInstanceID()==caption.GetInstanceID()) mirror.margin=new Vector4(0,0,width*0.44f,0);
        else mirror.alignment=TextAlignmentOptions.Right;
        mirror.enableWordWrapping=false;
    }

    internal static void Clear() => mirrors.Clear();
}
