using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static int BatchGroups(List<BatchItem> items)
    {
        return items.Count == 0 ? 0 : 1 + Switches(items.Select(i => i.BatchKey));
    }

    static int Switches(IEnumerable<string> keys)
    {
        var count = 0;
        var last = "";
        var first = true;
        foreach (var key in keys)
        {
            if (!first && key != last)
                count++;
            last = key;
            first = false;
        }
        return count;
    }

    static string TopKeys(IEnumerable<string> keys)
    {
        return JoinAll(keys.GroupBy(k => k).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Take(8).Select(g => $"{g.Key}({g.Count()})"));
    }

    static string BreakReason(BatchItem a, BatchItem b)
    {
        var list = new List<string>();
        if (a.Canvas != b.Canvas)
            list.Add("Canvas");
        if (a.Material != b.Material)
            list.Add("Material");
        if (a.Texture != b.Texture)
            list.Add("Texture");
        return JoinAll(list);
    }

    static string BreakAdvice(BatchItem a, BatchItem b, string reason)
    {
        if (reason.Contains("Material") && (a.Kind == "TextGraphic" || b.Kind == "TextGraphic"))
            return a.Kind == "TextGraphic" && b.Kind == "TextGraphic" ? "文本材质切换，统一字体材质或层级" : "文本与图片材质切换，审计层级顺序";
        if (reason.Contains("Material"))
            return "优先检查材质、Mask或特效";
        if (reason.Contains("Canvas"))
            return "确认子Canvas是否必要";
        if (a.Texture.StartsWith("Atlas:") && b.Texture.StartsWith("Atlas:"))
            return "跨图集相邻，审计层级或图片归属";
        if (a.Texture.Contains("UITexture") || b.Texture.Contains("UITexture"))
            return "散图或大图相邻，确认是否必须保留UITexture";
        return "纹理切换，结合层级确认";
    }

    static bool HasWhiteTexture(BreakInfo i)
    {
        return IsWhiteTexture(i.PrevTexture) || IsWhiteTexture(i.Texture);
    }

    static bool IsWhiteTexture(string texture)
    {
        return texture == "Builtin:White" || texture == "RuntimeTexture:UnityWhite";
    }

    static string WhiteSide(BreakInfo i)
    {
        if (IsWhiteTexture(i.PrevTexture) && IsWhiteTexture(i.Texture))
            return "Both";
        return IsWhiteTexture(i.PrevTexture) ? "Prev" : "Current";
    }

    static string WhiteKind(BreakInfo i)
    {
        if (IsWhiteTexture(i.PrevTexture) && IsWhiteTexture(i.Texture))
            return i.PrevKind + "=>" + i.Kind;
        return IsWhiteTexture(i.PrevTexture) ? i.PrevKind : i.Kind;
    }

    static string OtherKind(BreakInfo i)
    {
        if (IsWhiteTexture(i.PrevTexture) && IsWhiteTexture(i.Texture))
            return "";
        return IsWhiteTexture(i.PrevTexture) ? i.Kind : i.PrevKind;
    }

    static string TextureKind(string texture)
    {
        if (texture.StartsWith("Atlas:"))
            return "Atlas";
        if (IsUITextureKey(texture))
            return "UITexture";
        if (texture.StartsWith("Texture:"))
            return "Texture";
        if (texture == "Builtin:White")
            return "NullSpriteImage";
        if (texture == "RuntimeTexture:UnityWhite")
            return "RuntimeWhiteGraphic";
        return texture;
    }

    static int TargetScore(TargetInfo t)
    {
        return t.ImageBatchGroups * 4 + t.BreakCount + t.CrossAtlasBreaks * 3 + t.LooseTextureBreaks * 2 + t.TextBreaks * 2 + t.MaterialBreaks * 3 + t.CanvasBreaks * 3 + t.UITextureCount;
    }

    static string TargetMainIssue(TargetInfo t)
    {
        if (t.TextBreaks >= t.CrossAtlasBreaks && t.TextBreaks >= t.LooseTextureBreaks && t.TextBreaks >= t.MaterialBreaks && t.TextBreaks >= t.CanvasBreaks && t.TextBreaks > 0)
            return "文本图片交错";
        if (t.CrossAtlasBreaks >= t.LooseTextureBreaks && t.CrossAtlasBreaks >= t.MaterialBreaks && t.CrossAtlasBreaks >= t.CanvasBreaks && t.CrossAtlasBreaks > 0)
            return "跨图集相邻";
        if (t.LooseTextureBreaks >= t.MaterialBreaks && t.LooseTextureBreaks >= t.CanvasBreaks && t.LooseTextureBreaks > 0)
            return "散图或大图相邻";
        if (t.MaterialBreaks >= t.CanvasBreaks && t.MaterialBreaks > 0)
            return "材质或Mask";
        if (t.CanvasBreaks > 0)
            return "子Canvas";
        return "纹理顺序";
    }

    static string TargetNextStep(TargetInfo t)
    {
        if (t.MainIssue == "文本图片交错")
            return "按视觉层级合并文本片段或统一字体材质";
        if (t.MainIssue == "跨图集相邻")
            return "审计相邻图集归属和层级顺序";
        if (t.MainIssue == "散图或大图相邻")
            return "确认散图是否必须保留UITexture";
        if (t.MainIssue == "材质或Mask")
            return "检查材质、Mask和特效节点";
        if (t.MainIssue == "子Canvas")
            return "确认子Canvas是否必要";
        return "按层级顺序合并同纹理片段";
    }

    static bool IsTextBreak(BreakInfo info)
    {
        return info.Advice == "文本与图片材质切换，审计层级顺序" || info.Advice == "文本材质切换，统一字体材质或层级";
    }

    static string NodePath(Transform root, Transform node)
    {
        var parts = new List<string>();
        while (node != null && node != root)
        {
            parts.Add(node.name);
            node = node.parent;
        }
        parts.Add(root.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    static string CanvasKey(Transform root, Transform node)
    {
        var canvas = node.GetComponentInParent<Canvas>();
        return canvas == null ? "NoCanvas" : NodePath(root, canvas.transform);
    }

    static string MaterialKey(Graphic graphic)
    {
        if (graphic.material == null)
            return "DefaultUI";
        var path = AssetDatabase.GetAssetPath(graphic.material);
        return path.Length > 0 ? path : graphic.material.name;
    }

    static string TextMaterialKey(TMP_Text text)
    {
        var mat = text.fontSharedMaterial;
        if (mat == null)
            return "NoTextMaterial";
        var path = AssetDatabase.GetAssetPath(mat);
        return path.Length > 0 ? path : "TextMaterial:" + mat.name;
    }

    static string TextTextureKey(TMP_Text text)
    {
        var mat = text.fontSharedMaterial;
        var tex = mat == null ? null : mat.mainTexture;
        var path = tex == null ? "" : AssetDatabase.GetAssetPath(tex);
        if (path.Length > 0)
            return "TextTexture:" + path;
        var font = text.font == null ? "" : AssetDatabase.GetAssetPath(text.font);
        if (font.Length > 0)
            return "TextFont:" + font;
        return tex == null ? "NoTextTexture" : "TextTexture:" + tex.name;
    }

    static bool HasComponent(GameObject go, UIControlRole role)
    {
        return go.GetComponents<Component>().Any(c => c != null && Catalog.HasRole(c.GetType().Name, role));
    }

    static string ComponentNames(GameObject go)
    {
        return JoinAll(go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name));
    }

    static string NullSpriteAdvice(NullSpriteInfo info)
    {
        if (FunctionalNullSprite(info))
            return "功能组件占位，保留";
        if (info.ImagePlus || NullSpriteDynamicPath(info.Path) || HasAnyRole(info.ParentComponents, UIControlRole.DynamicImage))
            return "动态图片占位，保留";
        if (info.RaycastTarget && (HasAnyRole(info.Components, UIControlRole.Button) || HasAnyRole(info.ParentComponents, UIControlRole.Button)))
            return "按钮热区，人工确认";
        if (info.RaycastTarget)
            return "空图参与射线，优先检查";
        return "可删空Image候选";
    }

    static string NullSpriteReason(NullSpriteInfo info)
    {
        if (FunctionalNullSprite(info))
            return "挂有遮罩、背景、动画或引导组件";
        if (info.ImagePlus)
            return "挂有动态图片组件";
        if (HasAnyRole(info.ParentComponents, UIControlRole.DynamicImage))
            return "父节点挂有动态图片组件";
        if (NullSpriteDynamicPath(info.Path))
            return "节点路径像运行时图标";
        if (info.RaycastTarget)
            return "RaycastTarget开启";
        return "空Image无sprite且不接收射线";
    }

    static bool FunctionalNullSprite(NullSpriteInfo info)
    {
        return HasAnyRole(info.Components, UIControlRole.FunctionalEmptyImage) || HasAnyRole(info.ParentComponents, UIControlRole.FunctionalEmptyImage);
    }

    static bool NullSpriteDynamicPath(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.Contains("/topresview/") || lower.Contains("/itemview") || lower.EndsWith("/icon") || lower.EndsWith("/iconimg") || lower.Contains("/npchead/") || lower.Contains("/playerhead/");
    }

    static bool HasAnyRole(string text, UIControlRole role)
    {
        return text.Split(';').Any(x => Catalog.HasRole(x, role));
    }
}
}
