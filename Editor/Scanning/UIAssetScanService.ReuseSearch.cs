using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static readonly HashSet<string> QueryImageMissingValueArgs = new HashSet<string>
    {
        "-batchmode",
        "-executeMethod",
        "-logFile",
        "-nographics",
        "-projectPath",
        "-quit",
        "-uiDraftJsonPath",
        "-uiInputImageFolder",
        "-uiOutputFolder",
        "-uiPrefabPath",
        "-uiPreviewPath",
        "-uiReferenceImages",
        "-uiStylePrompt",
        "-uiQueryImage"
    };

    public static void SearchReuseByImage()
    {
        var path = EditorUtility.OpenFilePanel("选择缺图裁剪图", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
            SearchReuseByImagePath(path);
    }

    public static void SearchReuseByImage(UIAIToolsProfile scanProfile)
    {
        Profile = scanProfile;
        SearchReuseByImage();
    }

    public static void SearchReuseByImage(UIAIToolsProfile scanProfile, UIControlCatalog controlCatalog)
    {
        Profile = scanProfile;
        Catalog = controlCatalog;
        SearchReuseByImage();
    }

    public static void SearchReuseByImageBatch()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-uiQueryImage");
        if (index < 0 || index + 1 >= args.Length || QueryImageMissingValueArgs.Contains(args[index + 1]))
            throw new Exception("Missing -uiQueryImage <path>");
        SearchReuseByImagePath(args[index + 1]);
    }

    public static void SearchReuseByImageBatch(UIAIToolsProfile scanProfile)
    {
        Profile = scanProfile;
        SearchReuseByImageBatch();
    }

    public static void SearchReuseByImageBatch(UIAIToolsProfile scanProfile, UIControlCatalog controlCatalog)
    {
        Profile = scanProfile;
        Catalog = controlCatalog;
        SearchReuseByImageBatch();
    }

    static void SearchReuseByImagePath(string queryPath)
    {
        Directory.CreateDirectory(Profile.logRoot);
        var query = LoadReadableImage(queryPath);
        if (query == null)
            throw new Exception("Only png/jpg/jpeg query images are supported");
        var q = ImageSample(query);
        UnityEngine.Object.DestroyImmediate(query);
        var assets = FindTextures().Where(p => IsUIAtlas(p) || IsUITexture(p)).ToList();
        var atlasMap = BuildAtlasMap();
        var prefabMap = BuildPrefabMap();
        var textMap = BuildTextMap();
        var hashes = assets.ToDictionary(p => p, Sha1);
        var dups = assets.GroupBy(p => hashes[p]).ToDictionary(g => g.Key, g => g.Count());
        var rows = new List<string[]>();
        for (int i = 0; i < assets.Count; i++)
        {
            var path = assets[i];
            EditorUtility.DisplayProgressBar("按截图查找已有图片", path, (float)i / assets.Count);
            var tex = LoadReadableImage(path);
            if (tex == null)
                continue;
            var score = ImageScore(q, ImageSample(tex));
            var w = tex.width;
            var h = tex.height;
            UnityEngine.Object.DestroyImmediate(tex);
            var name = Path.GetFileNameWithoutExtension(path);
            var prefabs = prefabMap.TryGetValue(path, out var p) ? p.Distinct().OrderBy(x => x).ToList() : new List<string>();
            var owners = prefabs.Select(Owner).Distinct().OrderBy(x => x).ToList();
            var texts = textMap.TryGetValue(name.ToLowerInvariant(), out var t) ? t : new List<string>();
            atlasMap.TryGetValue(path, out var atlas);
            var atlasOwner = string.IsNullOrEmpty(atlas) ? "" : AssetOwner(atlas);
            var owner = string.IsNullOrEmpty(atlasOwner) ? AssetOwner(path) : atlasOwner;
            var size = SizeClass(path, w, h);
            rows.Add(new[]
            {
                queryPath, path, name, AssetDatabase.AssetPathToGUID(path), w.ToString(), h.ToString(), score.ToString("0.0000", CultureInfo.InvariantCulture), size, ImageKind(path, atlas), atlas, owner,
                prefabs.Count.ToString(), owners.Count.ToString(), Join(owners), Join(prefabs), Join(texts),
                ReuseAdvice(path, size, owner, owners, texts.Count, dups[hashes[path]] - 1),
                ReuseReason(path, size, owner, owners, texts.Count, dups[hashes[path]] - 1)
            });
        }
        var lines = new List<string> { UIReportFiles.ReuseSearchResultsHeader };
        lines.AddRange(rows.OrderBy(r => float.Parse(r[6], CultureInfo.InvariantCulture)).Take(80).Select(r => string.Join(",", r.Select(Csv))));
        File.WriteAllLines(ReuseSearchPath, lines, new UTF8Encoding(true));
        UIReuseSearchResultService.ReadRows(Profile);
        EditorUtility.ClearProgressBar();
        Debug.Log($"按截图查找已有图片完成：{ReuseSearchPath}");
        if (!Application.isBatchMode)
            EditorUtility.RevealInFinder(Path.GetFullPath(ReuseSearchPath));
    }

    static string ReuseAdvice(string path, string size, string owner, List<string> owners, int textCount, int sameHashCount)
    {
        if (textCount > 0)
            return "按名加载风险，保留原路径";
        if (owner == "ACommon")
            return "可直接复用ACommon";
        if (sameHashCount > 0)
            return "同内容重复，审计合并归属";
        if (owners.Count > 1)
            return size == "Large" ? "多界面大图，保留UITexture或设计确认" : "多界面复用，审计迁ACommon或共享图集";
        if (owners.Count == 1 && !SameOwner(owners[0], owner))
            return size == "Large" ? "跨功能大图，保留UITexture或设计确认" : "跨功能借图，迁当前功能或升公共";
        if (IsUITexture(path) && size != "Large")
            return "小散图，确认非动态后迁功能图集";
        if (IsUITexture(path))
            return "大图保留UITexture";
        return "功能内使用，保留当前归属";
    }

    static string ReuseReason(string path, string size, string owner, List<string> owners, int textCount, int sameHashCount)
    {
        if (textCount > 0)
            return "脚本或配置可能按文件名加载";
        if (owner == "ACommon")
            return "已在通用图集";
        if (sameHashCount > 0)
            return "项目存在相同hash图片";
        if (owners.Count > 1)
            return "被多个界面归属使用";
        if (owners.Count == 1 && !SameOwner(owners[0], owner))
            return $"界面归属{owners[0]}与资源归属{owner}不一致";
        if (IsUITexture(path) && size != "Large")
            return "运行时散图且尺寸不大";
        if (IsUITexture(path))
            return "尺寸偏大或疑似背景插画";
        return "界面归属与资源归属一致";
    }

    static Texture2D LoadReadableImage(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return null;
        var full = Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
        if (!File.Exists(full))
            return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        return tex.LoadImage(File.ReadAllBytes(full)) ? tex : null;
    }

    static Color[] ImageSample(Texture2D tex)
    {
        var pixels = new Color[256];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
                pixels[y * 16 + x] = tex.GetPixelBilinear((x + 0.5f) / 16f, (y + 0.5f) / 16f);
        }
        return pixels;
    }

    static float ImageScore(Color[] a, Color[] b)
    {
        var sum = 0f;
        var count = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (b[i].a < 0.05f)
                continue;
            var dr = a[i].r - b[i].r;
            var dg = a[i].g - b[i].g;
            var db = a[i].b - b[i].b;
            sum += dr * dr + dg * dg + db * db;
            count++;
        }
        return count == 0 ? 999f : Mathf.Sqrt(sum / count);
    }
}
}
