using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIPackageDependencyBoundaryService
    {
        static readonly string[] ForbiddenNames = { "YooAsset", "GameApp", "MotionFramework", "Xipin.LFramework", "com.xipin.lframework" };

        public static void ValidatePackage()
        {
            ValidateRoot(Path.GetFullPath("Packages/com.xipin.ui-ai-tools"));
            Debug.Log("UI AI Tools package dependency boundary validation passed.");
        }

        public static void ValidateContract()
        {
            var root = Path.Combine(Path.GetTempPath(), "UIAIToolsPackageDependencyBoundaryContract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var yooAsset = "Yoo" + "Asset";
                var gameApp = "Game" + "App";
                var motionFramework = "Motion" + "Framework";
                var xipinLFramework = "Xipin." + "LFramework";
                var packageLFramework = "com.xipin." + "lframework";
                Write(root, "Good.cs", "class Good { const string Note = \"不修改图集或 YooAsset 配置。\"; }");
                Write(root, "CommentOnly.cs", "// " + yooAsset + ".Editor belongs to the host executor\nclass CommentOnly {}");
                Write(root, "Good.asmdef", "{\"name\":\"Good\",\"references\":[\"Unity.TextMeshPro\"]}");
                ValidateRoot(root);
                ExpectFailure(root, "Using" + yooAsset + ".cs", "using " + yooAsset + ".Editor;\nclass Bad {}", yooAsset);
                ExpectFailure(root, "Using" + gameApp + ".cs", "using " + gameApp + ";\nclass Bad {}", gameApp);
                ExpectFailure(root, "Qualified" + motionFramework + ".cs", "class Bad { object Value = " + motionFramework + ".AssetManager.Instance; }", motionFramework);
                ExpectFailure(root, "Bad.asmdef", "{\"name\":\"Bad\",\"references\":[\"" + yooAsset + ".Editor\"]}", yooAsset);
                ExpectFailure(root, "Using" + xipinLFramework + ".cs", "using " + xipinLFramework + ";\nclass Bad {}", xipinLFramework);
                ExpectFailure(root, "Package.asmdef", "{\"name\":\"Bad\",\"references\":[\"" + packageLFramework + "\"]}", packageLFramework);
            }
            finally
            {
                Directory.Delete(root, true);
            }
            Debug.Log("UI AI Tools package dependency boundary contract validation passed.");
        }

        static void ValidateRoot(string root)
        {
            foreach (var path in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
                         .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)))
                ValidateFile(path);
        }

        static void ValidateFile(string path)
        {
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (Path.GetExtension(path).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
                    ValidateAsmdefLine(path, i + 1, line);
                else
                    ValidateCsLine(path, i + 1, line);
            }
        }

        static void ValidateCsLine(string path, int lineNumber, string line)
        {
            var commentStart = line.IndexOf("//", StringComparison.Ordinal);
            if (commentStart >= 0)
                line = line.Substring(0, commentStart).Trim();
            foreach (var name in ForbiddenNames)
            {
                if (line.StartsWith("using " + name, StringComparison.Ordinal) || line.Contains(name + "."))
                    throw new Exception($"Forbidden UI AI Tools package dependency {name}: {path}:{lineNumber}");
            }
        }

        static void ValidateAsmdefLine(string path, int lineNumber, string line)
        {
            foreach (var name in ForbiddenNames)
            {
                if (line.Contains("\"" + name, StringComparison.Ordinal))
                    throw new Exception($"Forbidden UI AI Tools package dependency {name}: {path}:{lineNumber}");
            }
        }

        static void ExpectFailure(string root, string relativePath, string content, string expected)
        {
            var path = Write(root, relativePath, content);
            try
            {
                ValidateFile(path);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains(expected))
                {
                    File.Delete(path);
                    return;
                }
                throw new Exception($"Unexpected UI AI Tools dependency boundary contract failure for {relativePath}: {exception.Message}");
            }
            throw new Exception("UI AI Tools dependency boundary contract sample did not fail: " + relativePath);
        }

        static string Write(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath);
            File.WriteAllText(path, content);
            return path;
        }
    }
}
