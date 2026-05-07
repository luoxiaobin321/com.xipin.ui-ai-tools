using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static void WritePrefabDrawCallRisk(Dictionary<string, string> atlasMap, Dictionary<string, List<string>> textMap)
    {
        var risk = new List<string> { UIReportFiles.PrefabDrawCallRiskHeader };
        var detail = new List<string> { UIReportFiles.PrefabBatchSequenceHeader };
        var breaks = new List<string> { UIReportFiles.PrefabBatchBreaksHeader };
        var breakInfos = new List<BreakInfo>();
        var targets = new List<TargetInfo>();
        var loose = new Dictionary<string, LooseTextureInfo>();
        var nullSprites = new List<NullSpriteInfo>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Profile.prefabRoot }))
        {
            var prefab = AssetDatabase.GUIDToAssetPath(guid);
            var owner = Owner(prefab);
            var root = PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var items = GetBatchItems(prefab, owner, root, atlasMap);
                CollectNullSpriteImages(prefab, owner, root, nullSprites);
                var prefabBreaks = new List<BreakInfo>();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    detail.Add(string.Join(",", new[]
                    {
                        Csv(prefab), Csv(owner), i.ToString(), Csv(item.Path), Csv(item.Type), Csv(item.Source), Csv(item.Kind), Csv(item.Canvas), Csv(item.Texture), Csv(item.Material), Csv(item.BatchKey), Csv(item.ImageAsset), Csv(item.Atlas)
                    }));
                    if (!string.IsNullOrEmpty(item.ImageAsset) && IsUITexture(item.ImageAsset))
                    {
                        if (!loose.TryGetValue(item.ImageAsset, out var info))
                            loose[item.ImageAsset] = info = new LooseTextureInfo();
                        info.UseCount++;
                        info.Prefabs.Add(prefab);
                        info.Nodes.Add(prefab + "#" + item.Path);
                    }
                    if (i > 0 && item.BatchKey != items[i - 1].BatchKey)
                    {
                        var prev = items[i - 1];
                        var reason = BreakReason(prev, item);
                        var advice = BreakAdvice(prev, item, reason);
                        breaks.Add(string.Join(",", new[]
                        {
                            Csv(prefab), Csv(owner), i.ToString(), Csv(prev.Path), Csv(item.Path), Csv(prev.Type), Csv(item.Type), Csv(prev.Kind), Csv(item.Kind), Csv(reason), Csv(prev.Texture), Csv(item.Texture), Csv(prev.Material), Csv(item.Material), Csv(prev.Canvas), Csv(item.Canvas), Csv(advice)
                        }));
                        var info = new BreakInfo { Prefab = prefab, Owner = owner, Index = i, PrevPath = prev.Path, Path = item.Path, PrevType = prev.Type, Type = item.Type, PrevKind = prev.Kind, Kind = item.Kind, Reason = reason, PrevTexture = prev.Texture, Texture = item.Texture, Advice = advice };
                        breakInfos.Add(info);
                        prefabBreaks.Add(info);
                    }
                }

                var images = items.Where(i => i.Type == "Image" || i.Type == "RawImage").ToList();
                var deps = AssetDatabase.GetDependencies(prefab, true).Where(IsUiImage).Distinct().ToList();
                var atlases = deps.Select(d => atlasMap.TryGetValue(d, out var atlas) ? atlas : "").Where(a => a.Length > 0).Distinct().ToList();
                var textures = deps.Where(IsUITexture).Distinct().ToList();
                var target = new TargetInfo
                {
                    Prefab = prefab,
                    Owner = owner,
                    AtlasCount = atlases.Count,
                    UITextureCount = textures.Count,
                    LargeTextureCount = textures.Count(IsLarge),
                    ImageBatchGroups = BatchGroups(images),
                    EstimatedBatchGroups = BatchGroups(items),
                    BreakCount = prefabBreaks.Count,
                    CrossAtlasBreaks = prefabBreaks.Count(i => i.Advice == "跨图集相邻，审计层级或图片归属"),
                    LooseTextureBreaks = prefabBreaks.Count(i => i.Advice == "散图或大图相邻，确认是否必须保留UITexture"),
                    TextBreaks = prefabBreaks.Count(IsTextBreak),
                    MaterialBreaks = prefabBreaks.Count(i => i.Reason.Contains("Material") && !IsTextBreak(i)),
                    CanvasBreaks = prefabBreaks.Count(i => i.Reason.Contains("Canvas"))
                };
                target.Score = TargetScore(target);
                target.MainIssue = TargetMainIssue(target);
                target.NextStep = TargetNextStep(target);
                targets.Add(target);
                risk.Add(string.Join(",", new[]
                {
                    Csv(prefab),
                    Csv(owner),
                    items.Count.ToString(),
                    items.Count(i => i.Type == "Image").ToString(),
                    items.Count(i => i.Type == "RawImage").ToString(),
                    items.Count(i => i.Type == "Graphic").ToString(),
                    items.Count(i => i.ImagePlus).ToString(),
                    items.Count(i => i.NullSprite).ToString(),
                    items.Count(i => i.Type == "Image" && i.Texture.StartsWith("Atlas:")).ToString(),
                    items.Count(i => i.Type == "Image" && IsUITextureKey(i.Texture)).ToString(),
                    items.Where(i => i.Type == "Image" && i.Texture.StartsWith("Atlas:")).Select(i => i.Texture).Distinct().Count().ToString(),
                    items.Where(i => i.Type == "Image" && IsUITextureKey(i.Texture)).Select(i => i.Texture).Distinct().Count().ToString(),
                    items.Select(i => i.Texture).Distinct().Count().ToString(),
                    items.Select(i => i.Material).Distinct().Count().ToString(),
                    BatchGroups(items).ToString(),
                    BatchGroups(images).ToString(),
                    Switches(items.Select(i => i.Texture)).ToString(),
                    Switches(images.Select(i => i.Texture)).ToString(),
                    Switches(items.Select(i => i.Material)).ToString(),
                    root.GetComponentsInChildren<Canvas>(true).Count(c => c.gameObject.activeInHierarchy && c.transform != root.transform).ToString(),
                    root.GetComponentsInChildren<Mask>(true).Count(c => c.gameObject.activeInHierarchy && c.enabled).ToString(),
                    root.GetComponentsInChildren<RectMask2D>(true).Count(c => c.gameObject.activeInHierarchy && c.enabled).ToString(),
                    Csv(TopKeys(items.Select(i => i.Texture))),
                    Csv(TopKeys(images.Select(i => i.Texture)))
                }));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        File.WriteAllLines(PrefabDrawCallRiskPath, risk, new UTF8Encoding(true));
        File.WriteAllLines(PrefabBatchSequencePath, detail, new UTF8Encoding(true));
        File.WriteAllLines(PrefabBatchBreakPath, breaks, new UTF8Encoding(true));
        WriteBatchBreakSummary(breakInfos);
        WriteOptimizationTargets(targets);
        WriteTextureSwitchPairs(breakInfos);
        WriteWhiteTextureBreaks(breakInfos);
        WriteNullSpriteImages(nullSprites);
        WriteLooseTextureCandidates(loose, textMap);
    }

    static void CollectNullSpriteImages(string prefab, string owner, GameObject root, List<NullSpriteInfo> list)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (!image.gameObject.activeInHierarchy || !image.enabled || image.sprite != null || image.color.a <= 0.001f)
                continue;
            var rt = image.rectTransform;
            var info = new NullSpriteInfo
            {
                Prefab = prefab,
                Owner = owner,
                Path = NodePath(root.transform, image.transform),
                Node = image.name,
                Width = Mathf.RoundToInt(rt.rect.width),
                Height = Mathf.RoundToInt(rt.rect.height),
                Alpha = image.color.a,
                RaycastTarget = image.raycastTarget,
                Maskable = image.maskable,
                ImagePlus = HasComponent(image.gameObject, UIControlRole.DynamicImage),
                Components = ComponentNames(image.gameObject),
                Parent = image.transform.parent == null ? "" : NodePath(root.transform, image.transform.parent),
                ParentComponents = image.transform.parent == null ? "" : ComponentNames(image.transform.parent.gameObject)
            };
            info.Advice = NullSpriteAdvice(info);
            info.Reason = NullSpriteReason(info);
            list.Add(info);
        }
    }

    static void WriteNullSpriteImages(List<NullSpriteInfo> infos)
    {
        var lines = new List<string> { UIReportFiles.PrefabNullSpriteImagesHeader };
        foreach (var i in infos.GroupBy(i => i.Prefab + " " + i.Path).Select(g => g.First()).OrderByDescending(i => i.Advice).ThenBy(i => i.Prefab).ThenBy(i => i.Path))
        {
            lines.Add(string.Join(",", new[]
            {
                Csv(i.Prefab), Csv(i.Owner), Csv(i.Path), Csv(i.Node), i.Width.ToString(), i.Height.ToString(), i.Alpha.ToString("0.###"), i.RaycastTarget.ToString(), i.Maskable.ToString(), i.ImagePlus.ToString(), Csv(i.Components), Csv(i.Parent), Csv(i.ParentComponents), Csv(i.Advice), Csv(i.Reason)
            }));
        }
        File.WriteAllLines(PrefabNullSpritePath, lines, new UTF8Encoding(true));
    }

    static void WriteWhiteTextureBreaks(List<BreakInfo> infos)
    {
        var lines = new List<string> { UIReportFiles.PrefabWhiteTextureBreaksHeader };
        foreach (var group in infos.Where(HasWhiteTexture).GroupBy(i => new { i.Prefab, i.Owner, WhiteSide = WhiteSide(i), WhiteKind = WhiteKind(i), OtherKind = OtherKind(i), i.Reason, i.Advice }).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Prefab))
        {
            lines.Add(string.Join(",", new[]
            {
                Csv(group.Key.Prefab),
                Csv(group.Key.Owner),
                Csv(group.Key.WhiteSide),
                Csv(group.Key.WhiteKind),
                Csv(group.Key.OtherKind),
                Csv(group.Key.Reason),
                Csv(group.Key.Advice),
                group.Count().ToString(),
                group.Min(i => i.Index).ToString(),
                Csv(JoinAll(group.OrderBy(i => i.Index).Take(8).Select(i => $"{i.Index}:{i.PrevPath}->{i.Path}")))
            }));
        }
        File.WriteAllLines(PrefabWhiteTextureBreakPath, lines, new UTF8Encoding(true));
    }

    static void WriteTextureSwitchPairs(List<BreakInfo> infos)
    {
        var lines = new List<string> { UIReportFiles.PrefabTextureSwitchPairsHeader };
        foreach (var group in infos.GroupBy(i => new { i.Prefab, i.Owner, i.Advice, i.Reason, i.PrevTexture, i.Texture }).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Prefab))
        {
            lines.Add(string.Join(",", new[]
            {
                Csv(group.Key.Prefab),
                Csv(group.Key.Owner),
                Csv(group.Key.Advice),
                Csv(group.Key.Reason),
                Csv(group.Key.PrevTexture),
                Csv(group.Key.Texture),
                group.Count().ToString(),
                group.Min(i => i.Index).ToString(),
                Csv(JoinAll(group.OrderBy(i => i.Index).Take(8).Select(i => $"{i.Index}:{i.PrevPath}->{i.Path}")))
            }));
        }
        File.WriteAllLines(PrefabTextureSwitchPairsPath, lines, new UTF8Encoding(true));
    }

    static void WriteOptimizationTargets(List<TargetInfo> targets)
    {
        var lines = new List<string> { UIReportFiles.PrefabOptimizationTargetsHeader };
        foreach (var t in targets.OrderByDescending(t => t.Score).ThenBy(t => t.Prefab))
        {
            lines.Add(string.Join(",", new[]
            {
                Csv(t.Prefab), Csv(t.Owner), t.Score.ToString(), Csv(t.MainIssue), Csv(t.NextStep), t.ImageBatchGroups.ToString(), t.EstimatedBatchGroups.ToString(), t.BreakCount.ToString(), t.CrossAtlasBreaks.ToString(), t.LooseTextureBreaks.ToString(), t.TextBreaks.ToString(), t.MaterialBreaks.ToString(), t.CanvasBreaks.ToString(), t.AtlasCount.ToString(), t.UITextureCount.ToString(), t.LargeTextureCount.ToString()
            }));
        }
        File.WriteAllLines(PrefabOptimizationTargetsPath, lines, new UTF8Encoding(true));
    }

    static void WriteBatchBreakSummary(List<BreakInfo> infos)
    {
        var lines = new List<string> { UIReportFiles.PrefabBatchBreakSummaryHeader };
        foreach (var group in infos.GroupBy(i => i.Prefab).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
        {
            lines.Add(string.Join(",", new[]
            {
                Csv(group.Key),
                Csv(group.First().Owner),
                group.Count().ToString(),
                group.Count(i => i.Reason.Contains("Texture")).ToString(),
                group.Count(IsTextBreak).ToString(),
                group.Count(i => i.Reason.Contains("Material") && !IsTextBreak(i)).ToString(),
                group.Count(i => i.Reason.Contains("Canvas")).ToString(),
                group.Count(i => i.Advice == "跨图集相邻，审计层级或图片归属").ToString(),
                group.Count(i => i.Advice == "散图或大图相邻，确认是否必须保留UITexture").ToString(),
                Csv(TopKeys(group.Select(i => i.Advice))),
                Csv(TopKeys(group.Select(i => i.Texture))),
                Csv(TopKeys(group.Select(i => i.PrevTexture + " => " + i.Texture))),
                Csv(JoinAll(group.Take(6).Select(i => $"{i.Index}:{i.PrevPath}->{i.Path}:{i.Advice}")))
            }));
        }
        File.WriteAllLines(PrefabBatchBreakSummaryPath, lines, new UTF8Encoding(true));
    }

    static void WriteLooseTextureCandidates(Dictionary<string, LooseTextureInfo> loose, Dictionary<string, List<string>> textMap)
    {
        var lines = new List<string> { UIReportFiles.LooseTextureCandidatesHeader };
        foreach (var pair in loose.OrderByDescending(p => p.Value.UseCount).ThenBy(p => p.Key))
        {
            var path = pair.Key;
            var info = pair.Value;
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var texts = textMap.TryGetValue(name.ToLowerInvariant(), out var t) ? t : new List<string>();
            var owners = info.Prefabs.Select(Owner).Distinct().OrderBy(x => x).ToList();
            var width = tex == null ? 0 : tex.width;
            var height = tex == null ? 0 : tex.height;
            var size = SizeClass(path, width, height);
            var target = TargetAtlasForLoose(path);
            var advice = LooseTextureAdvice(size, texts.Count, info.Prefabs.Count, target);
            lines.Add(string.Join(",", new[]
            {
                Csv(path),
                Csv(name),
                Csv(AssetDatabase.AssetPathToGUID(path)),
                width.ToString(),
                height.ToString(),
                Csv(size),
                info.UseCount.ToString(),
                info.Prefabs.Count.ToString(),
                Csv(JoinAll(info.Prefabs.OrderBy(p => p))),
                Csv(JoinAll(info.Nodes.OrderBy(p => p))),
                Csv(Join(texts)),
                owners.Count.ToString(),
                Csv(JoinAll(owners)),
                Csv(advice.Item1),
                Csv(advice.Item2),
                Csv(target)
            }));
        }
        File.WriteAllLines(LooseTextureCandidatesPath, lines, new UTF8Encoding(true));
    }

    static List<BatchItem> GetBatchItems(string prefab, string owner, GameObject root, Dictionary<string, string> atlasMap)
    {
        var list = new List<BatchItem>();
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.gameObject.activeInHierarchy || !graphic.enabled || graphic.color.a <= 0.001f)
                continue;
            var item = new BatchItem
            {
                Path = NodePath(root.transform, graphic.transform),
                Type = "Graphic",
                Source = graphic.GetType().Name,
                Canvas = CanvasKey(root.transform, graphic.transform),
                Material = MaterialKey(graphic),
                ImagePlus = HasComponent(graphic.gameObject, UIControlRole.DynamicImage)
            };
            FillTexture(item, graphic, atlasMap);
            item.BatchKey = item.Canvas + "|" + item.Material + "|" + item.Texture;
            list.Add(item);
        }
        return list;
    }

    static void FillTexture(BatchItem item, Graphic graphic, Dictionary<string, string> atlasMap)
    {
        if (graphic is Image image)
        {
            item.Type = "Image";
            item.NullSprite = image.sprite == null;
            item.ImageAsset = image.sprite == null ? "" : AssetDatabase.GetAssetPath(image.sprite);
            if (item.ImageAsset.Length > 0 && atlasMap.TryGetValue(item.ImageAsset, out var atlas))
            {
                item.Atlas = atlas;
                item.Texture = "Atlas:" + atlas;
                item.Kind = "AtlasImage";
                return;
            }
            item.Texture = item.ImageAsset.Length > 0 ? "Texture:" + item.ImageAsset : "Builtin:White";
            item.Kind = item.ImageAsset.Length > 0 ? TextureKind(item.Texture) : "NullSpriteImage";
            return;
        }
        if (graphic is RawImage raw)
        {
            item.Type = "RawImage";
            var path = raw.texture == null ? "" : AssetDatabase.GetAssetPath(raw.texture);
            item.ImageAsset = path;
            item.Texture = path.Length > 0 ? "Texture:" + path : raw.texture == null ? "NoTexture" : "RuntimeTexture:" + raw.texture.name;
            item.Kind = TextureKind(item.Texture);
            return;
        }
        if (graphic is TMP_Text tmp)
        {
            item.ImageAsset = tmp.font == null ? "" : AssetDatabase.GetAssetPath(tmp.font);
            item.Material = TextMaterialKey(tmp);
            item.Texture = TextTextureKey(tmp);
            item.Kind = "TextGraphic";
            return;
        }
        var tex = graphic.mainTexture;
        var texPath = tex == null ? "" : AssetDatabase.GetAssetPath(tex);
        item.Texture = texPath.Length > 0 ? "Texture:" + texPath : tex == null ? "NoTexture" : "RuntimeTexture:" + tex.name;
        item.Kind = item.Source.Contains("Text") ? "TextGraphic" : TextureKind(item.Texture);
    }
}
}
