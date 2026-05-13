using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Xipin.UIAITools
{
    public static class UIAIToolsAISettingsService
    {
        const string ApiKeyPrefsKey = "UIAITools.AI.ApiKey";
        const string ResponsesUrlPrefsKey = "UIAITools.AI.ResponsesUrl";
        const string ModelPrefsKey = "UIAITools.AI.Model";
        const string LegacySkinningApiKeyPrefsKey = "UIAITools.Skinning.OpenAI.ApiKey";
        const string LegacySkinningResponsesUrlPrefsKey = "UIAITools.Skinning.OpenAI.ResponsesUrl";
        const string LegacySkinningModelPrefsKey = "UIAITools.Skinning.OpenAI.Model";
        const string DefaultResponsesUrl = "https://api.openai.com/v1/responses";
        const string DefaultModel = "gpt-5.5";

        public static UIAIToolsAISettings Load()
        {
            var apiKey = FirstValue(
                EditorPrefs.GetString(ApiKeyPrefsKey, ""),
                EditorPrefs.GetString(LegacySkinningApiKeyPrefsKey, ""),
                Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            var responsesUrl = FirstValue(
                EditorPrefs.GetString(ResponsesUrlPrefsKey, ""),
                EditorPrefs.GetString(LegacySkinningResponsesUrlPrefsKey, ""),
                Environment.GetEnvironmentVariable("UIAI_OPENAI_RESPONSES_URL"),
                Environment.GetEnvironmentVariable("UIAI_OPENAI_BASE_URL"),
                CodexResponsesUrl(),
                DefaultResponsesUrl);

            var model = FirstValue(
                EditorPrefs.GetString(ModelPrefsKey, ""),
                EditorPrefs.GetString(LegacySkinningModelPrefsKey, ""),
                Environment.GetEnvironmentVariable("UIAI_OPENAI_MODEL"),
                CodexModel(),
                DefaultModel);

            return new UIAIToolsAISettings(apiKey, NormalizeResponsesUrl(responsesUrl), model);
        }

        public static UIAIToolsAISettings LoadCodexDefaults()
        {
            var current = Load();
            return new UIAIToolsAISettings(
                current.ApiKey,
                FirstValue(CodexResponsesUrl(), current.ResponsesUrl),
                FirstValue(CodexModel(), current.Model));
        }

        public static void Save(string apiKey, string responsesUrl, string model)
        {
            var normalizedApiKey = apiKey ?? "";
            var normalizedResponsesUrl = NormalizeResponsesUrl(FirstValue(responsesUrl, DefaultResponsesUrl));
            var normalizedModel = FirstValue(model, DefaultModel);
            EditorPrefs.SetString(ApiKeyPrefsKey, normalizedApiKey);
            EditorPrefs.SetString(ResponsesUrlPrefsKey, normalizedResponsesUrl);
            EditorPrefs.SetString(ModelPrefsKey, normalizedModel);
            EditorPrefs.SetString(LegacySkinningApiKeyPrefsKey, normalizedApiKey);
            EditorPrefs.SetString(LegacySkinningResponsesUrlPrefsKey, normalizedResponsesUrl);
            EditorPrefs.SetString(LegacySkinningModelPrefsKey, normalizedModel);
        }

        public static string NormalizeResponsesUrl(string value)
        {
            var url = (value ?? "").Trim().TrimEnd('/');
            if (url.Length == 0)
                return DefaultResponsesUrl;
            if (url.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
                return url;
            if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                return url + "/responses";
            return url + "/v1/responses";
        }

        static string CodexResponsesUrl()
        {
            var configPath = Path.Combine(CodexHome(), "config.toml");
            if (!File.Exists(configPath))
                return "";
            var lines = File.ReadAllLines(configPath);
            var provider = ConfigValue(lines, "model_provider");
            var section = string.IsNullOrEmpty(provider) ? "" : "[model_providers." + provider + "]";
            var inProvider = string.IsNullOrEmpty(section);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                    inProvider = line == section;
                if (!inProvider || !line.StartsWith("base_url", StringComparison.Ordinal))
                    continue;
                var baseUrl = ConfigLineValue(line);
                return string.IsNullOrEmpty(baseUrl) ? "" : NormalizeResponsesUrl(baseUrl);
            }
            return "";
        }

        static string CodexModel()
        {
            var configPath = Path.Combine(CodexHome(), "config.toml");
            return File.Exists(configPath) ? ConfigValue(File.ReadAllLines(configPath), "model") : "";
        }

        static string CodexHome()
        {
            var value = Environment.GetEnvironmentVariable("CODEX_HOME");
            return string.IsNullOrEmpty(value) ? @"E:\Codex\.codex" : value;
        }

        static string ConfigValue(string[] lines, string key)
        {
            foreach (var line in lines.Select(item => item.Trim()))
                if (line.StartsWith(key, StringComparison.Ordinal))
                    return ConfigLineValue(line);
            return "";
        }

        static string ConfigLineValue(string line)
        {
            var index = line.IndexOf('=');
            return index < 0 ? "" : line.Substring(index + 1).Trim().Trim('"');
        }

        static string FirstValue(params string[] values)
        {
            foreach (var value in values)
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            return "";
        }
    }

    public sealed class UIAIToolsAISettings
    {
        public readonly string ApiKey;
        public readonly string ResponsesUrl;
        public readonly string Model;

        public UIAIToolsAISettings(string apiKey, string responsesUrl, string model)
        {
            ApiKey = apiKey ?? "";
            ResponsesUrl = responsesUrl ?? "";
            Model = model ?? "";
        }
    }
}
