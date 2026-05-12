using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xipin.UIAITools
{
    [InitializeOnLoad]
    public static class UISkinRuntimePrefabOverrideService
    {
        const string BackupRoot = "Library/UIAITools/SkinRuntimePrefabOverride";
        const string StatePath = BackupRoot + "/state.json";

        public static string LastMessage { get; set; }
        public static bool IsActive => File.Exists(StatePath);

        static UISkinRuntimePrefabOverrideService()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += RestoreStaleOverride;
        }

        public static void ApplyAndEnterPlay(string manifestPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new Exception("Real runtime skin preview must start from Edit Mode.");
            if (IsActive)
                throw new Exception("A real runtime skin preview override is already active. Restore it first.");

            var manifest = UISkinContractService.LoadManifest(NormalizeManifestPath(manifestPath));
            var sourcePrefabPath = NormalizePrefabPath(manifest.sourcePrefabPath, "sourcePrefabPath");
            var skinPrefabPath = NormalizePrefabPath(manifest.generated.outputPrefabPath, "generated.outputPrefabPath");
            if (!File.Exists(sourcePrefabPath))
                throw new Exception("Source prefab not found: " + sourcePrefabPath);
            if (!File.Exists(skinPrefabPath))
                throw new Exception("Skin prefab not found: " + skinPrefabPath);

            Directory.CreateDirectory(BackupRoot);
            var backupPath = BackupRoot + "/" + AssetDatabase.AssetPathToGUID(sourcePrefabPath) + ".prefab";
            AssetDatabase.SaveAssets();
            File.Copy(sourcePrefabPath, backupPath, true);

            try
            {
                File.Copy(skinPrefabPath, sourcePrefabPath, true);
                AssetDatabase.ImportAsset(sourcePrefabPath, ImportAssetOptions.ForceUpdate);
                WriteState(new RuntimePrefabOverrideState
                {
                    manifestPath = manifestPath,
                    sourcePrefabPath = sourcePrefabPath,
                    skinPrefabPath = skinPrefabPath,
                    backupPrefabPath = backupPath
                });
                LastMessage = "真实运行时替换已应用：" + sourcePrefabPath + " <- " + skinPrefabPath;
                EditorApplication.EnterPlaymode();
            }
            catch
            {
                File.Copy(backupPath, sourcePrefabPath, true);
                AssetDatabase.ImportAsset(sourcePrefabPath, ImportAssetOptions.ForceUpdate);
                DeleteState();
                throw;
            }
        }

        public static void Restore()
        {
            var state = LoadState();
            if (state == null)
            {
                LastMessage = "没有需要恢复的真实运行时替换。";
                return;
            }

            if (!File.Exists(state.backupPrefabPath))
                throw new Exception("Runtime preview backup prefab not found: " + state.backupPrefabPath);

            File.Copy(state.backupPrefabPath, state.sourcePrefabPath, true);
            AssetDatabase.ImportAsset(state.sourcePrefabPath, ImportAssetOptions.ForceUpdate);
            File.Delete(state.backupPrefabPath);
            DeleteState();
            LastMessage = "源 prefab 已恢复：" + state.sourcePrefabPath;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (IsActive && (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode))
                Restore();
        }

        static void RestoreStaleOverride()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && IsActive)
                Restore();
        }

        static string NormalizeManifestPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Select a skin.json before opening real runtime skin preview.");
            var normalized = NormalizePath(path);
            if (!normalized.EndsWith("/skin.json", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Real runtime skin preview requires a skin.json asset path.");
            if (!File.Exists(normalized))
                throw new Exception("Skin manifest not found: " + normalized);
            return normalized;
        }

        static string NormalizePrefabPath(string path, string field)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Skin manifest " + field + " is empty.");
            var normalized = NormalizePath(path);
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) || !normalized.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Skin manifest " + field + " must be an Assets prefab path: " + path);
            return normalized;
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Trim();
        }

        static RuntimePrefabOverrideState LoadState()
        {
            if (!File.Exists(StatePath))
                return null;
            return JsonUtility.FromJson<RuntimePrefabOverrideState>(File.ReadAllText(StatePath));
        }

        static void WriteState(RuntimePrefabOverrideState state)
        {
            Directory.CreateDirectory(BackupRoot);
            File.WriteAllText(StatePath, JsonUtility.ToJson(state, true));
        }

        static void DeleteState()
        {
            if (File.Exists(StatePath))
                File.Delete(StatePath);
        }

        [Serializable]
        sealed class RuntimePrefabOverrideState
        {
            public string manifestPath;
            public string sourcePrefabPath;
            public string skinPrefabPath;
            public string backupPrefabPath;
        }
    }
}
