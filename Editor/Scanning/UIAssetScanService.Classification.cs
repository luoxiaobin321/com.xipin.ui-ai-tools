using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static Tuple<string, string> GetAdvice(string path, string name, Texture tex, string atlas, int prefabRefs, int textRefs)
    {
        var max = tex == null ? 0 : Mathf.Max(tex.width, tex.height);
        var area = tex == null ? 0 : tex.width * tex.height;
        var lower = name.ToLowerInvariant();
        if (IsArtUI(path))
            return Tuple.Create("设计目录", "Art/UI不进运行时Bundle");
        if (IsUIAtlas(path))
            return string.IsNullOrEmpty(atlas) ? Tuple.Create("补进图集", "在UIAtlas目录但未被SpriteAtlas收集") : Tuple.Create("保留图集", "已被SpriteAtlas收集");
        if (textRefs > 0 || path.Contains("/Map/"))
            return Tuple.Create("UITexture", "存在脚本或配置按名引用风险");
        if (IsUITexture(path) && (lower.Contains("renwu") || lower.Contains("lihui") || lower.Contains("juese") || lower.Contains("role")))
            return Tuple.Create("UITexture", "疑似人物或插画");
        if (max >= 512 || area >= 262144)
            return Tuple.Create("UITexture", "尺寸偏大");
        if (lower.Contains("item") || lower.Contains("head") || lower.Contains("icon") || lower.Contains("avatar"))
            return Tuple.Create("图标图集", "疑似列表高频小图");
        if (IsUITexture(path) && prefabRefs > 0)
            return Tuple.Create("检查迁入功能图集", "小图且被prefab直接引用");
        return Tuple.Create("人工确认", "证据不足");
    }

    static string Feature(string path)
    {
        return OwnerInRoot(path, Profile.uiTextureRoot);
    }

    static string Owner(string prefab)
    {
        var owner = OwnerInRoot(prefab, Profile.prefabRoot);
        if (IsActivityPath(prefab, Profile.prefabRoot) && !string.IsNullOrEmpty(owner))
            return owner;
        return Path.GetFileNameWithoutExtension(prefab);
    }

    static string TargetAtlas(string path, string feature)
    {
        var dir = IsActivityPath(path, Profile.uiTextureRoot) ? $"{Root(Profile.uiAtlasRoot)}/Activity/{feature}" : $"{Root(Profile.uiAtlasRoot)}/{feature}";
        if (!Directory.Exists(dir))
            return $"{dir}/atlas_{feature.ToLowerInvariant()}.spriteatlasv2";
        var atlas = Directory.GetFiles(dir, "*.spriteatlasv2", SearchOption.AllDirectories).OrderBy(p => p).FirstOrDefault();
        return string.IsNullOrEmpty(atlas) ? $"{dir}/atlas_{feature.ToLowerInvariant()}.spriteatlasv2" : atlas.Replace('\\', '/');
    }

    static string AssetOwner(string path)
    {
        if (IsACommon(path))
            return "ACommon";
        var owner = OwnerInRoot(path, Profile.uiAtlasRoot);
        if (!string.IsNullOrEmpty(owner))
            return owner;
        owner = OwnerInRoot(path, Profile.uiTextureRoot);
        return string.IsNullOrEmpty(owner) ? OwnerInRoot(path, Profile.artUIRoot) : owner;
    }

    static string ImageKind(string path, string atlas)
    {
        if (!string.IsNullOrEmpty(atlas))
            return "AtlasSprite";
        if (IsUITexture(path))
            return "UITexture";
        if (IsArtUI(path))
            return "ArtUI";
        return "";
    }

    static string OwnerMatch(string prefabOwner, string assetOwner, int textCount)
    {
        if (textCount > 0)
            return "DynamicRisk";
        if (assetOwner == "ACommon")
            return "Shared";
        return SameOwner(prefabOwner, assetOwner) ? "Match" : "CrossFeature";
    }

    static bool SameFeature(string feature, string prefab)
    {
        if (string.IsNullOrEmpty(feature))
            return true;
        var owner = Owner(prefab);
        return SameOwner(owner, feature);
    }

    static bool SameOwner(string owner, string feature)
    {
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(feature))
            return false;
        owner = NormalizeOwner(owner);
        feature = NormalizeOwner(feature);
        return owner.IndexOf(feature, StringComparison.OrdinalIgnoreCase) >= 0 || feature.IndexOf(owner, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string NormalizeOwner(string value)
    {
        value = value.ToLowerInvariant().Replace("sprite_", "").Replace("atlas_", "").Replace("altas_", "");
        return value.StartsWith("ui") ? value.Substring(2) : value;
    }

    static string ACommonAdvice(int ownerCount, int textCount, int prefabCount)
    {
        if (textCount > 0)
            return "保留或人工确认按名加载";
        if (ownerCount > 1)
            return "保留通用候选";
        if (prefabCount > 0)
            return "审计是否功能专属";
        return "审计未引用";
    }

    static string SizeClass(string path, int width, int height)
    {
        var lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (Mathf.Max(width, height) >= 512 || width * height >= 262144 || lower.Contains("renwu") || lower.Contains("lihui") || lower.Contains("juese") || lower.Contains("role"))
            return "Large";
        if (Mathf.Max(width, height) <= 256 && width * height <= 65536)
            return "Small";
        return "Medium";
    }

    static string UITextureAdvice(string size, int prefabCount, int textCount)
    {
        if (textCount > 0)
            return "保留按名加载";
        if (size == "Small" && prefabCount > 0)
            return "审计迁入功能图集";
        if (size == "Small")
            return "审计动态或未引用";
        return "保留UITexture候选";
    }

    static Tuple<string, string> LooseTextureAdvice(string size, int textCount, int prefabCount, string target)
    {
        if (textCount > 0)
            return Tuple.Create("保留UITexture", "存在脚本或配置按名引用风险");
        if (size == "Large")
            return Tuple.Create("保留UITexture", "尺寸偏大或疑似背景插画");
        if (prefabCount > 1)
            return Tuple.Create("审计共享图集", "多个prefab直接引用，需确认是否进ACommon或共享功能图集");
        if (!string.IsNullOrEmpty(target) && !File.Exists(target))
            return Tuple.Create("暂缓迁入", "目标功能图集不存在，单张小图新建图集收益低");
        return Tuple.Create("审计迁入功能图集", "单prefab直接引用且非大图");
    }

    static string TargetAtlasForLoose(string path)
    {
        if (StartsWithRoot(path, $"{Root(Profile.uiTextureRoot)}/Common"))
            return $"{Root(Profile.uiAtlasRoot)}/ACommon/atlas_common/atlas_common.spriteatlasv2";
        var feature = Feature(path);
        return string.IsNullOrEmpty(feature) ? "" : TargetAtlas(path, feature);
    }

    static string DuplicateAdvice(List<string> paths, int prefabCount, int textCount)
    {
        if (textCount > 0)
            return "按名加载重复，人工确认";
        if (paths.Any(IsUITexture) && paths.Any(IsUIAtlas))
            return "图集与散图重复，按引用决定保留";
        if (prefabCount == 0)
            return "无prefab引用，审计删除候选";
        return "同内容重复，审计合并";
    }

    static string LogPath(string file)
    {
        return UIReportFiles.GetPath(Profile.logRoot, file);
    }

    static string Sha1(string path)
    {
        using (var sha1 = SHA1.Create())
            return BitConverter.ToString(sha1.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static bool IsArtUI(string path)
    {
        return StartsWithRoot(path, Profile.artUIRoot);
    }

    static bool IsUIAtlas(string path)
    {
        return StartsWithRoot(path, Profile.uiAtlasRoot);
    }

    static bool IsUITexture(string path)
    {
        return StartsWithRoot(path, Profile.uiTextureRoot);
    }

    static bool IsACommon(string path)
    {
        return StartsWithRoot(path, $"{Root(Profile.uiAtlasRoot)}/ACommon");
    }

    static bool IsActivityPath(string path, string root)
    {
        return StartsWithRoot(path, $"{Root(root)}/Activity");
    }

    static bool IsUITextureKey(string texture)
    {
        return texture.StartsWith("Texture:" + Root(Profile.uiTextureRoot), StringComparison.OrdinalIgnoreCase);
    }

    static string OwnerInRoot(string path, string root)
    {
        path = Root(path);
        root = Root(root);
        if (!StartsWithRoot(path, root) || path.Length <= root.Length)
            return "";
        var parts = path.Substring(root.Length + 1).Split('/');
        return parts.Length > 1 && parts[0] == "Activity" ? parts[1] : parts[0];
    }

    static bool StartsWithRoot(string path, string root)
    {
        path = Root(path);
        root = Root(root);
        return path.Equals(root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
    }

    static string Root(string path)
    {
        return (path ?? "").Replace('\\', '/').TrimEnd('/');
    }

    static bool IsLarge(string path)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
        var lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        return tex != null && (Mathf.Max(tex.width, tex.height) >= 512 || tex.width * tex.height >= 262144 || lower.Contains("renwu") || lower.Contains("lihui") || lower.Contains("juese") || lower.Contains("role"));
    }

    static bool IsUiImage(string path)
    {
        return IsImage(path) && !IsMapImage(path) && (IsArtUI(path) || IsUIAtlas(path) || IsUITexture(path));
    }

    static bool IsMapImage(string path)
    {
        return Profile.excludeMapFromUITriage && StartsWithRoot(path, Profile.mapTextureRoot);
    }

    static bool IsImage(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd";
    }
}
}
