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
    static UIAIToolsProfile profile;
    static UIControlCatalog catalog;
    public static UIAIToolsProfile Profile
    {
        get
        {
            if (profile == null)
                profile = ScriptableObject.CreateInstance<UIAIToolsProfile>();
            return profile;
        }
        set { profile = value; }
    }

    public static UIControlCatalog Catalog
    {
        get
        {
            if (catalog == null)
                catalog = ScriptableObject.CreateInstance<UIControlCatalog>();
            return catalog;
        }
        set { catalog = value; }
    }

    static string ReportPath => LogPath(UIReportFiles.AssetTriageReport);
    static string PlanPath => LogPath(UIReportFiles.AssetTriagePlan);
    static string PrefabStatsPath => LogPath(UIReportFiles.PrefabAtlasStats);
    static string ACommonUsagePath => LogPath(UIReportFiles.ACommonUsage);
    static string UITextureSizePath => LogPath(UIReportFiles.TextureSizeReport);
    static string DuplicatePath => LogPath(UIReportFiles.DuplicateImageReport);
    static string ReuseIndexPath => LogPath(UIReportFiles.ReuseIndex);
    static string PrefabImageDetailsPath => LogPath(UIReportFiles.PrefabImageDetails);
    static string PrefabAtlasBreakdownPath => LogPath(UIReportFiles.PrefabAtlasBreakdown);
    static string PrefabDrawCallRiskPath => LogPath(UIReportFiles.PrefabDrawCallRisk);
    static string PrefabBatchSequencePath => LogPath(UIReportFiles.PrefabBatchSequence);
    static string PrefabBatchBreakPath => LogPath(UIReportFiles.PrefabBatchBreaks);
    static string PrefabBatchBreakSummaryPath => LogPath(UIReportFiles.PrefabBatchBreakSummary);
    static string PrefabOptimizationTargetsPath => LogPath(UIReportFiles.PrefabOptimizationTargets);
    static string PrefabTextureSwitchPairsPath => LogPath(UIReportFiles.PrefabTextureSwitchPairs);
    static string PrefabWhiteTextureBreakPath => LogPath(UIReportFiles.PrefabWhiteTextureBreaks);
    static string PrefabNullSpritePath => LogPath(UIReportFiles.PrefabNullSpriteImages);
    static string LooseTextureCandidatesPath => LogPath(UIReportFiles.LooseTextureCandidates);
    static string ReuseSearchPath => LogPath(UIReportFiles.ReuseSearchResults);

    public static void Run(UIAIToolsProfile scanProfile)
    {
        Profile = scanProfile;
        Run();
    }

    public static void Run(UIAIToolsProfile scanProfile, UIControlCatalog controlCatalog)
    {
        Profile = scanProfile;
        Catalog = controlCatalog;
        Run();
    }

    public static void Run()
    {
        Directory.CreateDirectory(Profile.logRoot);
        var assets = FindTextures();
        var atlasMap = BuildAtlasMap();
        var prefabMap = BuildPrefabMap();
        var textMap = BuildTextMap();
        var names = assets.GroupBy(Path.GetFileNameWithoutExtension).Where(g => g.Count() > 1).ToDictionary(g => g.Key, g => g.Count());
        var hashes = assets.ToDictionary(p => p, Sha1);
        var hashDups = assets.GroupBy(p => hashes[p]).Where(g => g.Count() > 1).ToDictionary(g => g.Key, g => g.ToList());
        var lines = new List<string> { UIReportFiles.AssetTriageReportHeader };
        var plans = new List<string> { UIReportFiles.AssetTriagePlanHeader };

        for (int i = 0; i < assets.Count; i++)
        {
            var path = assets[i];
            EditorUtility.DisplayProgressBar("UI图片归类扫描", path, (float)i / assets.Count);
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var name = Path.GetFileNameWithoutExtension(path);
            var prefabRefs = prefabMap.TryGetValue(path, out var prefabs) ? prefabs : new List<string>();
            var textRefs = textMap.TryGetValue(name.ToLowerInvariant(), out var texts) ? texts : new List<string>();
            atlasMap.TryGetValue(path, out var atlas);
            names.TryGetValue(name, out var dup);
            var advice = GetAdvice(path, name, tex, atlas, prefabRefs.Count, textRefs.Count);
            var plan = GetPlan(path, tex, prefabRefs, advice.Item1, hashes[path], hashDups);
            lines.Add(string.Join(",", new[]
            {
                Csv(path),
                Csv(name),
                Csv(AssetDatabase.AssetPathToGUID(path)),
                tex == null ? "0" : tex.width.ToString(),
                tex == null ? "0" : tex.height.ToString(),
                new FileInfo(path).Length.ToString(),
                tex == null ? "0" : UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex).ToString(),
                Csv(importer == null ? "" : importer.textureType + "/" + importer.spriteImportMode),
                Csv(atlas),
                Csv(Join(prefabRefs)),
                Csv(Join(textRefs)),
                dup.ToString(),
                Csv(hashes[path]),
                Csv(advice.Item1),
                Csv(advice.Item2)
            }));
            if (plan != null)
                plans.Add(string.Join(",", plan.Select(Csv)));
        }

        File.WriteAllLines(ReportPath, lines, new UTF8Encoding(true));
        File.WriteAllLines(PlanPath, plans, new UTF8Encoding(true));
        WritePrefabStats(atlasMap);
        WriteACommonUsage(assets, atlasMap, prefabMap, textMap, hashes);
        WriteUITextureSizeReport(assets, prefabMap, textMap, hashes);
        WriteDuplicateReport(assets, atlasMap, prefabMap, textMap, hashes);
        WriteReuseIndex(assets, atlasMap, prefabMap, textMap, hashes);
        WritePrefabImageDetails(atlasMap, textMap, hashes);
        WritePrefabAtlasBreakdown(atlasMap);
        WritePrefabDrawCallRisk(atlasMap, textMap);
        EditorUtility.ClearProgressBar();
        UIReportValidationService.Validate(Profile);
        Debug.Log($"UI图片归类扫描完成：{ReportPath}，{PlanPath}，{PrefabStatsPath}，{ACommonUsagePath}，{UITextureSizePath}，{DuplicatePath}，{ReuseIndexPath}，{PrefabImageDetailsPath}，{PrefabAtlasBreakdownPath}，{PrefabDrawCallRiskPath}，{PrefabBatchSequencePath}，{PrefabBatchBreakPath}，{PrefabBatchBreakSummaryPath}，{PrefabOptimizationTargetsPath}，{PrefabTextureSwitchPairsPath}，{PrefabWhiteTextureBreakPath}，{PrefabNullSpritePath}，{LooseTextureCandidatesPath}");
        if (!Application.isBatchMode)
            EditorUtility.RevealInFinder(Path.GetFullPath(ReportPath));
    }

}
}
