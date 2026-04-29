using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static void WritePrefabStats(Dictionary<string, string> atlasMap)
    {
        var lines = new List<string> { UIReportFiles.PrefabAtlasStatsHeader };
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Profile.prefabRoot }))
        {
            var prefab = AssetDatabase.GUIDToAssetPath(guid);
            var deps = AssetDatabase.GetDependencies(prefab, true).Where(IsUiImage).Distinct().ToList();
            var atlases = deps.Select(d => atlasMap.TryGetValue(d, out var atlas) ? atlas : "").Where(a => a.Length > 0).Distinct().OrderBy(a => a).ToList();
            var textures = deps.Where(IsUITexture).Distinct().OrderBy(d => d).ToList();
            var large = textures.Count(IsLarge);
            lines.Add(string.Join(",", new[]
            {
                Csv(prefab),
                Csv(Owner(prefab)),
                atlases.Count.ToString(),
                textures.Count.ToString(),
                large.ToString(),
                deps.Count.ToString(),
                Csv(Join(atlases)),
                Csv(Join(textures))
            }));
        }
        File.WriteAllLines(PrefabStatsPath, lines, new UTF8Encoding(true));
    }

    static void WriteACommonUsage(List<string> assets, Dictionary<string, string> atlasMap, Dictionary<string, List<string>> prefabMap, Dictionary<string, List<string>> textMap, Dictionary<string, string> hashes)
    {
        var lines = new List<string> { UIReportFiles.ACommonUsageHeader };
        foreach (var path in assets.Where(IsACommon).OrderBy(p => p))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var prefabs = prefabMap.TryGetValue(path, out var p) ? p : new List<string>();
            var texts = textMap.TryGetValue(name.ToLowerInvariant(), out var t) ? t : new List<string>();
            atlasMap.TryGetValue(path, out var atlas);
            var owners = prefabs.Select(Owner).Distinct().OrderBy(x => x).ToList();
            lines.Add(string.Join(",", new[]
            {
                Csv(path), Csv(name), Csv(AssetDatabase.AssetPathToGUID(path)),
                tex == null ? "0" : tex.width.ToString(), tex == null ? "0" : tex.height.ToString(),
                Csv(atlas), prefabs.Count.ToString(), owners.Count.ToString(), Csv(Join(owners)), Csv(Join(prefabs)), Csv(Join(texts)), Csv(hashes[path]), Csv(ACommonAdvice(owners.Count, texts.Count, prefabs.Count))
            }));
        }
        File.WriteAllLines(ACommonUsagePath, lines, new UTF8Encoding(true));
    }

    static void WriteUITextureSizeReport(List<string> assets, Dictionary<string, List<string>> prefabMap, Dictionary<string, List<string>> textMap, Dictionary<string, string> hashes)
    {
        var lines = new List<string> { UIReportFiles.TextureSizeReportHeader };
        foreach (var path in assets.Where(IsUITexture).OrderBy(p => p))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var prefabs = prefabMap.TryGetValue(path, out var p) ? p : new List<string>();
            var texts = textMap.TryGetValue(name.ToLowerInvariant(), out var t) ? t : new List<string>();
            var w = tex == null ? 0 : tex.width;
            var h = tex == null ? 0 : tex.height;
            var area = w * h;
            var size = SizeClass(path, w, h);
            lines.Add(string.Join(",", new[]
            {
                Csv(path), Csv(name), Csv(AssetDatabase.AssetPathToGUID(path)), w.ToString(), h.ToString(), area.ToString(), new FileInfo(path).Length.ToString(), tex == null ? "0" : UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex).ToString(),
                prefabs.Count.ToString(), Csv(Join(texts)), Csv(hashes[path]), Csv(size), Csv(UITextureAdvice(size, prefabs.Count, texts.Count))
            }));
        }
        File.WriteAllLines(UITextureSizePath, lines, new UTF8Encoding(true));
    }

    static void WriteDuplicateReport(List<string> assets, Dictionary<string, string> atlasMap, Dictionary<string, List<string>> prefabMap, Dictionary<string, List<string>> textMap, Dictionary<string, string> hashes)
    {
        var lines = new List<string> { UIReportFiles.DuplicateImageReportHeader };
        foreach (var group in assets.GroupBy(p => hashes[p]).Where(g => g.Count() > 1).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
        {
            var paths = group.OrderBy(p => p).ToList();
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(paths[0]);
            var prefabs = paths.SelectMany(p => prefabMap.TryGetValue(p, out var refs) ? refs : new List<string>()).Distinct().OrderBy(p => p).ToList();
            var texts = paths.SelectMany(p => textMap.TryGetValue(Path.GetFileNameWithoutExtension(p).ToLowerInvariant(), out var refs) ? refs : new List<string>()).Distinct().OrderBy(p => p).ToList();
            var atlases = paths.Select(p => atlasMap.TryGetValue(p, out var atlas) ? atlas : "").Where(a => a.Length > 0).Distinct().OrderBy(a => a).ToList();
            lines.Add(string.Join(",", new[]
            {
                Csv(group.Key), paths.Count.ToString(), tex == null ? "0" : tex.width.ToString(), tex == null ? "0" : tex.height.ToString(), paths.Sum(p => new FileInfo(p).Length).ToString(), Csv(Join(paths)), Csv(Join(paths.Select(AssetDatabase.AssetPathToGUID).ToList())), Csv(Join(atlases)), Csv(Join(prefabs)), Csv(Join(texts)), Csv(DuplicateAdvice(paths, prefabs.Count, texts.Count))
            }));
        }
        File.WriteAllLines(DuplicatePath, lines, new UTF8Encoding(true));
    }

    static void WriteReuseIndex(List<string> assets, Dictionary<string, string> atlasMap, Dictionary<string, List<string>> prefabMap, Dictionary<string, List<string>> textMap, Dictionary<string, string> hashes)
    {
        var dups = assets.GroupBy(p => hashes[p]).ToDictionary(g => g.Key, g => g.OrderBy(p => p).ToList());
        var lines = new List<string> { UIReportFiles.ReuseIndexHeader };
        foreach (var path in assets.Where(p => IsUIAtlas(p) || IsUITexture(p)).OrderBy(p => p))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var texts = textMap.TryGetValue(name.ToLowerInvariant(), out var t) ? t : new List<string>();
            var prefabs = prefabMap.TryGetValue(path, out var p) ? p.Distinct().OrderBy(x => x).ToList() : new List<string>();
            var owners = prefabs.Select(Owner).Distinct().OrderBy(x => x).ToList();
            atlasMap.TryGetValue(path, out var atlas);
            var atlasOwner = string.IsNullOrEmpty(atlas) ? "" : AssetOwner(atlas);
            var owner = string.IsNullOrEmpty(atlasOwner) ? AssetOwner(path) : atlasOwner;
            var w = tex == null ? 0 : tex.width;
            var h = tex == null ? 0 : tex.height;
            var size = SizeClass(path, w, h);
            var same = dups[hashes[path]].Where(x => x != path).ToList();
            lines.Add(string.Join(",", new[]
            {
                Csv(path), Csv(name), Csv(AssetDatabase.AssetPathToGUID(path)), w.ToString(), h.ToString(), Csv(size), Csv(ImageKind(path, atlas)), Csv(atlas), Csv(owner),
                prefabs.Count.ToString(), owners.Count.ToString(), Csv(Join(owners)), Csv(Join(prefabs)), Csv(Join(texts)), Csv(hashes[path]), (same.Count + 1).ToString(), Csv(Join(same)),
                Csv(ReuseAdvice(path, size, owner, owners, texts.Count, same.Count)), Csv(ReuseReason(path, size, owner, owners, texts.Count, same.Count))
            }));
        }
        File.WriteAllLines(ReuseIndexPath, lines, new UTF8Encoding(true));
    }

    static void WritePrefabImageDetails(Dictionary<string, string> atlasMap, Dictionary<string, List<string>> textMap, Dictionary<string, string> hashes)
    {
        var lines = new List<string> { UIReportFiles.PrefabImageDetailsHeader };
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Profile.prefabRoot }))
        {
            var prefab = AssetDatabase.GUIDToAssetPath(guid);
            var prefabOwner = Owner(prefab);
            foreach (var path in AssetDatabase.GetDependencies(prefab, true).Where(IsUiImage).Distinct().OrderBy(p => p))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var texts = textMap.TryGetValue(name.ToLowerInvariant(), out var t) ? t : new List<string>();
                atlasMap.TryGetValue(path, out var atlas);
                var imageOwner = AssetOwner(path);
                var atlasOwner = string.IsNullOrEmpty(atlas) ? "" : AssetOwner(atlas);
                var owner = string.IsNullOrEmpty(atlasOwner) ? imageOwner : atlasOwner;
                var w = tex == null ? 0 : tex.width;
                var h = tex == null ? 0 : tex.height;
                lines.Add(string.Join(",", new[]
                {
                    Csv(prefab), Csv(prefabOwner), Csv(path), Csv(imageOwner), Csv(ImageKind(path, atlas)), Csv(name), Csv(AssetDatabase.AssetPathToGUID(path)), w.ToString(), h.ToString(), Csv(SizeClass(path, w, h)), Csv(atlas), Csv(atlasOwner), Csv(Join(texts)), Csv(hashes.TryGetValue(path, out var hash) ? hash : ""), Csv(OwnerMatch(prefabOwner, owner, texts.Count))
                }));
            }
        }
        File.WriteAllLines(PrefabImageDetailsPath, lines, new UTF8Encoding(true));
    }

    static void WritePrefabAtlasBreakdown(Dictionary<string, string> atlasMap)
    {
        var lines = new List<string> { UIReportFiles.PrefabAtlasBreakdownHeader };
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Profile.prefabRoot }))
        {
            var prefab = AssetDatabase.GUIDToAssetPath(guid);
            var prefabOwner = Owner(prefab);
            var groups = AssetDatabase.GetDependencies(prefab, true)
                .Where(IsUiImage)
                .Distinct()
                .Where(p => atlasMap.ContainsKey(p))
                .GroupBy(p => atlasMap[p])
                .OrderBy(g => g.Key);
            foreach (var group in groups)
            {
                var atlasOwner = AssetOwner(group.Key);
                lines.Add(string.Join(",", new[]
                {
                    Csv(prefab), Csv(prefabOwner), Csv(group.Key), Csv(atlasOwner), group.Count().ToString(), Csv(OwnerMatch(prefabOwner, atlasOwner, 0)), Csv(JoinAll(group.OrderBy(p => p)))
                }));
            }
        }
        File.WriteAllLines(PrefabAtlasBreakdownPath, lines, new UTF8Encoding(true));
    }
}
}
