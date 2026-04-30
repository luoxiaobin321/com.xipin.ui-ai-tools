using System;
using System.Collections.Generic;
using System.Linq;

namespace Xipin.UIAITools
{
    internal static class UIRedesignRequestValidation
    {
        public static void ValidateSourcePrefab(UIAIToolsProfile profile, string sourcePrefabPath, string context)
        {
            if (string.IsNullOrEmpty(sourcePrefabPath))
                throw new Exception($"Missing source prefab path for UI redesign {context}.");
            if (sourcePrefabPath.Contains("\\") || !sourcePrefabPath.StartsWith("Assets/", StringComparison.Ordinal) || !sourcePrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new Exception("UI redesign sourcePrefabPath must be an Assets/ prefab path.");
            if (sourcePrefabPath.Contains("/../") || sourcePrefabPath.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception("UI redesign sourcePrefabPath cannot contain .. path segments.");
            var targets = UIScanReportRows.ReadPrefabOptimizationTargets(profile);
            if (!targets.Any(r => r["Prefab"] == sourcePrefabPath))
                throw new Exception("UI prefab is not present in scan reports: " + sourcePrefabPath);
        }

        public static void ValidateOutputFolder(string outputFolder)
        {
            ValidateOptionalAssetsFolder(outputFolder, "outputFolder");
        }

        public static void ValidateSourcePreviewPath(string sourcePreviewPath)
        {
            if (!string.IsNullOrEmpty(sourcePreviewPath))
                ValidateImageFilePath(sourcePreviewPath, "sourcePreviewPath");
        }

        public static void ValidateInputImageFolder(string inputImageFolder)
        {
            ValidateOptionalAssetsFolder(inputImageFolder, "inputImageFolder");
        }

        public static void ValidateReferenceImagePaths(List<string> referenceImagePaths)
        {
            if (referenceImagePaths == null)
                throw new Exception("UI redesign referenceImagePaths is required.");
            foreach (var path in referenceImagePaths)
                ValidateImagePath(path, "referenceImagePaths");
        }

        static void ValidateImagePath(string path, string field)
        {
            if (string.IsNullOrEmpty(path) || path.Contains("\\") || !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new Exception($"UI redesign {field} must be an Assets/ image path.");
            if (path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"UI redesign {field} cannot contain .. path segments.");
            if (!IsImagePath(path))
                throw new Exception($"UI redesign {field} must be an image path.");
        }

        static void ValidateImageFilePath(string path, string field)
        {
            if (path.Contains("\\"))
                throw new Exception($"UI redesign {field} must be an image path.");
            if (path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"UI redesign {field} cannot contain .. path segments.");
            if (!IsImagePath(path))
                throw new Exception($"UI redesign {field} must be an image path.");
        }

        static bool IsImagePath(string path)
        {
            return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".psb", StringComparison.OrdinalIgnoreCase);
        }

        static void ValidateOptionalAssetsFolder(string path, string field)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (path.Contains("\\") || !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new Exception($"UI redesign {field} must be an Assets/ path.");
            if (path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"UI redesign {field} cannot contain .. path segments.");
        }
    }
}
