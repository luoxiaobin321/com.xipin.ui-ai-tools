using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static List<string> FindTextures()
    {
        return AssetDatabase.FindAssets("t:Texture", new[] { Profile.artUIRoot, Profile.uiAtlasRoot, Profile.uiTextureRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsImage)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    static Dictionary<string, string> BuildAtlasMap()
    {
        var map = new Dictionary<string, string>();
        foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { Profile.uiAtlasRoot }))
        {
            var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            foreach (var obj in SpriteAtlasExtensions.GetPackables(atlas))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    foreach (var texGuid in AssetDatabase.FindAssets("t:Texture", new[] { path }))
                    {
                        var texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                        if (IsImage(texPath))
                            map[texPath] = atlasPath;
                    }
                }
                else if (IsImage(path))
                {
                    map[path] = atlasPath;
                }
            }
        }
        return map;
    }

    static Dictionary<string, List<string>> BuildPrefabMap()
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Profile.prefabRoot }))
        {
            var prefab = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var dep in AssetDatabase.GetDependencies(prefab, true))
            {
                if (!IsUiImage(dep))
                    continue;
                if (!map.TryGetValue(dep, out var list))
                    map[dep] = list = new List<string>();
                list.Add(prefab);
            }
        }
        return map;
    }

    static Dictionary<string, List<string>> BuildTextMap()
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var file in TextFiles())
        {
            var path = file.Replace('\\', '/');
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > 2 * 1024 * 1024)
                continue;
            var text = Encoding.UTF8.GetString(bytes).ToLowerInvariant();
            var keys = Regex.Matches(text, @"[a-z0-9_\-\u4e00-\u9fa5]{3,}").Cast<Match>().Select(m => m.Value).Distinct();
            foreach (var key in keys)
            {
                if (!map.TryGetValue(key, out var list))
                    map[key] = list = new List<string>();
                list.Add(path);
            }
        }
        return map;
    }

    static IEnumerable<string> TextFiles()
    {
        foreach (var root in Profile.textSearchRoots)
        {
            if (!Directory.Exists(root))
                continue;
            foreach (var file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".cs" || ext == ".bytes" || ext == ".json" || ext == ".txt" || ext == ".asset")
                    yield return file;
            }
        }
    }
}
}
