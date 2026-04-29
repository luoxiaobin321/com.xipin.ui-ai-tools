using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static string[] GetPlan(string path, Texture tex, List<string> prefabRefs, string advice, string hash, Dictionary<string, List<string>> hashDups)
    {
        var feature = Feature(path);
        var size = tex == null ? new[] { "0", "0" } : new[] { tex.width.ToString(), tex.height.ToString() };
        if (advice == "人工确认")
            return Plan("KeepReview", "Medium", path, "Keep in UITexture until usage is proven", feature, size, prefabRefs.Count, advice, "No prefab/text evidence in L0; may be dynamic or unused", prefabRefs);
        if (advice != "检查迁入功能图集")
            return null;

        var same = hashDups.TryGetValue(hash, out var dup) ? dup.Where(p => p != path).ToList() : new List<string>();
        if (same.Count > 0)
            return Plan("ReviewDuplicateSameHash", "Medium", path, Join(same), feature, size, prefabRefs.Count, advice, "Same content exists elsewhere; merge only after GUID refs are checked", prefabRefs);
        if (prefabRefs.Count > 1)
            return Plan("ReviewCommonOrSharedAtlas", "Medium", path, "ACommon or a shared feature atlas after manual usage check", feature, size, prefabRefs.Count, advice, "Referenced by multiple prefabs; moving to one feature atlas may create cross-atlas coupling", prefabRefs);
        if (prefabRefs.Count == 1 && !SameFeature(feature, prefabRefs[0]))
            return Plan("ReviewFeatureMismatch", "Medium", path, TargetAtlas(path, feature), feature, size, prefabRefs.Count, advice, "Source folder and prefab owner differ; confirm ownership before moving", prefabRefs);

        var target = TargetAtlas(path, feature);
        return Plan(File.Exists(target) ? "MoveToExistingAtlasCandidate" : "CreateFeatureAtlasCandidate", File.Exists(target) ? "Low" : "Medium", path, target, feature, size, prefabRefs.Count, advice, File.Exists(target) ? "Single prefab ref, no text refs, existing atlas target" : "Single prefab ref, no text refs, but target atlas does not exist yet", prefabRefs);
    }

    static string[] Plan(string action, string risk, string source, string target, string feature, string[] size, int prefabCount, string advice, string note, List<string> prefabRefs)
    {
        return new[] { action, risk, source, target, feature, size[0], size[1], prefabCount.ToString(), advice, note, Join(prefabRefs) };
    }
}
}
