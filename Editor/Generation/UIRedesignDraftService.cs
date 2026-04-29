using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIRedesignDraftService
    {
        public static UIRedesignDraft CreateDraft(UIRedesignRequest request, IUIAIGenerationProvider provider)
        {
            var draft = provider.CreateDraft(request);
            ValidateDraft(draft);
            ForceConfirmation(draft);
            return draft;
        }

        public static UIRedesignDraft CreateDraftAndShow(UIRedesignRequest request, IUIAIGenerationProvider provider)
        {
            var draft = CreateDraft(request, provider);
            UIRedesignDraftWindow.ShowDraft(draft);
            return draft;
        }

        public static string SaveDraft(UIAIToolsProfile profile, UIRedesignRequest request, UIRedesignDraft draft)
        {
            ValidateSavedDraft(profile, request, draft);
            return WriteDraft(profile, draft, DraftPath(profile, request, ""));
        }

        internal static string SaveDraftSnapshot(UIAIToolsProfile profile, UIRedesignRequest request, UIRedesignDraft draft, string sourceDraftJson)
        {
            ValidateSavedDraft(profile, request, draft);
            var path = DraftPath(profile, request, "_Snapshot");
            for (var i = 2; SamePath(path, sourceDraftJson); i++)
                path = DraftPath(profile, request, "_Snapshot" + i);
            return WriteDraft(profile, draft, path);
        }

        public static UIRedesignDraft LoadDraft(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new Exception("Missing UI redesign draft JSON: " + path);
            var json = File.ReadAllText(path);
            var rootStart = JsonObjectStart(json);
            var rootEnd = JsonObjectEnd(json, rootStart, "draft root");
            RequireNoTrailingContent(json, rootEnd);
            RequireJsonStringField(json, rootStart, rootEnd, "draftPreviewPath", "draftPreviewPath");
            RequireJsonStringField(json, rootStart, rootEnd, "generatedImageFolder", "generatedImageFolder");
            RequireJsonOptionalBooleanField(json, rootStart, rootEnd, "requiresConfirmation", "requiresConfirmation");
            var replacementPlanStart = RequireJsonObjectField(json, rootStart, rootEnd, "replacementPlan");
            var replacementPlanEnd = JsonObjectEnd(json, replacementPlanStart, "replacementPlan");
            ValidateReplacementItems(json, RequireJsonArrayField(json, replacementPlanStart, replacementPlanEnd, "items", "replacementPlan.items"));
            ValidateStringArrayItems(json, RequireJsonArrayField(json, rootStart, rootEnd, "risks", "risks"), "risks");
            UICreationBriefTemplateService.ValidateJsonObjectMembers(json, replacementPlanStart, replacementPlanEnd, "Invalid UI redesign draft JSON", "replacementPlan");
            UICreationBriefTemplateService.ValidateJsonObjectMembers(json, rootStart, rootEnd, "Invalid UI redesign draft JSON", "draft root");
            var draft = JsonUtility.FromJson<UIRedesignDraft>(json);
            ValidateDraft(draft);
            ForceConfirmation(draft);
            return draft;
        }

        public static UIRedesignDraft LoadDraftAndShow(string path)
        {
            var draft = LoadDraft(path);
            UIRedesignDraftWindow.ShowDraft(draft);
            return draft;
        }

        internal static void ValidateDraft(UIRedesignDraft draft)
        {
            if (draft == null)
                throw new Exception("Invalid UI redesign draft JSON: missing draft root");
            if (draft.replacementPlan == null || draft.replacementPlan.items == null)
                throw new Exception("Invalid UI redesign draft JSON: missing replacementPlan.items");
            if (draft.risks == null)
                throw new Exception("Invalid UI redesign draft JSON: risks is required");
            RequireDraftUnityPath(draft.draftPreviewPath, "draftPreviewPath");
            RequirePng(draft.draftPreviewPath, "draftPreviewPath");
            RequireDraftUnityPath(draft.generatedImageFolder, "generatedImageFolder");

            var oldAssets = new HashSet<string>();
            var generatedImageFolder = Root(draft.generatedImageFolder);
            for (int i = 0; i < draft.replacementPlan.items.Count; i++)
            {
                ValidateItem(draft.replacementPlan.items[i], i, generatedImageFolder);
                RequireUniqueOldAsset(draft.replacementPlan.items[i].oldAssetPath, oldAssets, i);
            }
        }

        static void ValidateItem(UIReplacementItem item, int index, string generatedImageFolder)
        {
            if (item == null)
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}] is empty");
            RequireUnityPath(item.oldAssetPath, index, "oldAssetPath");
            RequireUnityPath(item.newAssetPath, index, "newAssetPath");
            RequireUnityPath(item.targetAtlasPath, index, "targetAtlasPath");
            RequirePng(item.newAssetPath, index, "newAssetPath");
            if (!Root(item.newAssetPath).StartsWith(generatedImageFolder + "/", StringComparison.Ordinal))
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}].newAssetPath must be under generatedImageFolder");
            if (!item.targetAtlasPath.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}].targetAtlasPath must be a .spriteatlasv2 asset");
        }

        static void RequireDraftUnityPath(string value, string field)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception($"Invalid UI redesign draft JSON: {field} is required");
            if (value.Contains("\\") || !value.StartsWith("Assets/", StringComparison.Ordinal))
                throw new Exception($"Invalid UI redesign draft JSON: {field} must be an Assets/ path");
            RequireNoTraversal(value, field);
        }

        static void RequireUnityPath(string value, int index, string field)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}].{field} is required");
            if (value.Contains("\\") || !value.StartsWith("Assets/", StringComparison.Ordinal))
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}].{field} must be an Assets/ path");
            RequireNoTraversal(value, $"replacementPlan.items[{index}].{field}");
        }

        static void RequirePng(string value, string field)
        {
            if (!value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Invalid UI redesign draft JSON: {field} must be a .png asset");
        }

        static void RequirePng(string value, int index, string field)
        {
            if (!value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}].{field} must be a .png asset");
        }

        static void RequireUniqueOldAsset(string oldAssetPath, HashSet<string> oldAssets, int index)
        {
            if (!oldAssets.Add(oldAssetPath))
                throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}].oldAssetPath duplicates {oldAssetPath}");
        }

        static void RequireNoTraversal(string value, string field)
        {
            if (value.Contains("/../") || value.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"Invalid UI redesign draft JSON: {field} cannot contain .. path segments");
        }

        static int JsonObjectStart(string json)
        {
            var start = 0;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            if (start >= json.Length || json[start] != '{')
                throw new Exception("Invalid UI redesign draft JSON: missing draft root");
            return start;
        }

        static void RequireJsonStringField(string json, int objectStart, int objectEnd, string field, string label)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI redesign draft JSON: {label} is required");
            if (!JsonStringAt(json, valueStart))
                throw new Exception($"Invalid UI redesign draft JSON: {label} must be a string");
        }

        static int RequireJsonObjectField(string json, int objectStart, int objectEnd, string field)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI redesign draft JSON: {field} is required");
            if (!JsonObjectAt(json, valueStart, field))
                throw new Exception($"Invalid UI redesign draft JSON: {field} must be an object");
            return valueStart;
        }

        static int RequireJsonArrayField(string json, int objectStart, int objectEnd, string field, string label)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI redesign draft JSON: {label} is required");
            if (!JsonArrayAt(json, valueStart, field))
                throw new Exception($"Invalid UI redesign draft JSON: {label} must be an array");
            return valueStart;
        }

        static void ValidateReplacementItems(string json, int itemsStart)
        {
            var itemsEnd = JsonArrayEnd(json, itemsStart, "replacementPlan.items");
            var index = 0;
            var expectItem = true;
            var hasItem = false;
            for (var i = itemsStart + 1; i < itemsEnd; i++)
            {
                if (char.IsWhiteSpace(json[i]))
                    continue;
                if (!expectItem)
                {
                    if (json[i] != ',')
                        throw new Exception("Invalid UI redesign draft JSON: replacementPlan.items items must be separated by commas");
                    expectItem = true;
                    continue;
                }
                if (json[i] == ',')
                    throw new Exception("Invalid UI redesign draft JSON: replacementPlan.items item is missing");
                if (json[i] != '{')
                    throw new Exception($"Invalid UI redesign draft JSON: replacementPlan.items[{index}] must be an object");
                var itemEnd = JsonObjectEnd(json, i, $"replacementPlan.items[{index}]");
                RequireJsonStringField(json, i, itemEnd, "oldAssetPath", $"replacementPlan.items[{index}].oldAssetPath");
                RequireJsonStringField(json, i, itemEnd, "newAssetPath", $"replacementPlan.items[{index}].newAssetPath");
                RequireJsonStringField(json, i, itemEnd, "targetAtlasPath", $"replacementPlan.items[{index}].targetAtlasPath");
                RequireJsonOptionalBooleanField(json, i, itemEnd, "preserveGuid", $"replacementPlan.items[{index}].preserveGuid");
                RequireJsonOptionalBooleanField(json, i, itemEnd, "requiresConfirmation", $"replacementPlan.items[{index}].requiresConfirmation");
                RequireJsonOptionalStringField(json, i, itemEnd, "reason", $"replacementPlan.items[{index}].reason");
                UICreationBriefTemplateService.ValidateJsonObjectMembers(json, i, itemEnd, "Invalid UI redesign draft JSON", $"replacementPlan.items[{index}]");
                i = itemEnd;
                index++;
                expectItem = false;
                hasItem = true;
            }
            if (expectItem && hasItem)
                throw new Exception("Invalid UI redesign draft JSON: replacementPlan.items item is missing");
        }

        static void RequireJsonOptionalStringField(string json, int objectStart, int objectEnd, string field, string label)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart >= 0 && !JsonStringAt(json, valueStart))
                throw new Exception($"Invalid UI redesign draft JSON: {label} must be a string");
        }

        static void RequireJsonOptionalBooleanField(string json, int objectStart, int objectEnd, string field, string label)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart >= 0 && !JsonStartsWith(json, valueStart, "true") && !JsonStartsWith(json, valueStart, "false"))
                throw new Exception($"Invalid UI redesign draft JSON: {label} must be a boolean");
        }

        static void ValidateStringArrayItems(string json, int arrayStart, string field)
        {
            var arrayEnd = JsonArrayEnd(json, arrayStart, field);
            var expectItem = true;
            var hasItem = false;
            for (var i = arrayStart + 1; i < arrayEnd; i++)
            {
                if (char.IsWhiteSpace(json[i]))
                    continue;
                if (!expectItem)
                {
                    if (json[i] != ',')
                        throw new Exception($"Invalid UI redesign draft JSON: {field} items must be separated by commas");
                    expectItem = true;
                    continue;
                }
                if (json[i] == ',')
                    throw new Exception($"Invalid UI redesign draft JSON: {field} item is missing");
                if (json[i] != '"')
                    throw new Exception($"Invalid UI redesign draft JSON: {field} item must be a string");
                SkipJsonString(json, ref i);
                expectItem = false;
                hasItem = true;
            }
            if (expectItem && hasItem)
                throw new Exception($"Invalid UI redesign draft JSON: {field} item is missing");
        }

        static bool JsonStartsWith(string json, int start, string value)
        {
            if (start + value.Length > json.Length || string.Compare(json, start, value, 0, value.Length, StringComparison.Ordinal) != 0)
                return false;
            var next = start + value.Length;
            while (next < json.Length && char.IsWhiteSpace(json[next]))
                next++;
            return next == json.Length || json[next] == ',' || json[next] == '}';
        }

        static bool JsonStringAt(string json, int start)
        {
            if (start >= json.Length || json[start] != '"')
                return false;
            var end = start;
            SkipJsonString(json, ref end);
            if (end >= json.Length)
                return false;
            var next = end + 1;
            while (next < json.Length && char.IsWhiteSpace(json[next]))
                next++;
            return next == json.Length || json[next] == ',' || json[next] == '}';
        }

        static bool JsonObjectAt(string json, int start, string field)
        {
            if (start >= json.Length || json[start] != '{')
                return false;
            return JsonValueEndsAt(json, JsonObjectEnd(json, start, field) + 1);
        }

        static bool JsonArrayAt(string json, int start, string field)
        {
            if (start >= json.Length || json[start] != '[')
                return false;
            return JsonValueEndsAt(json, JsonArrayEnd(json, start, field) + 1);
        }

        static bool JsonValueEndsAt(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            return index == json.Length || json[index] == ',' || json[index] == '}';
        }

        static int JsonFieldValueStart(string json, int objectStart, int objectEnd, string field)
        {
            var depth = 0;
            var result = -1;
            for (int i = objectStart + 1; i < objectEnd; i++)
            {
                if (json[i] == '"')
                {
                    if (depth == 0)
                    {
                        if (MatchesJsonField(json, field, ref i))
                        {
                            if (result >= 0)
                                throw new Exception($"Invalid UI redesign draft JSON: {field} is duplicated");
                            var valueStart = i + 1;
                            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                                valueStart++;
                            result = valueStart;
                        }
                    }
                    else
                    {
                        SkipJsonString(json, ref i);
                    }
                }
                else if (json[i] == '{' || json[i] == '[')
                    depth++;
                else if (json[i] == '}' || json[i] == ']')
                    depth--;
            }
            return result;
        }

        static int JsonObjectEnd(string json, int start, string field)
        {
            var depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '"')
                    SkipJsonString(json, ref i);
                else if (json[i] == '{')
                    depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            throw new Exception($"Invalid UI redesign draft JSON: {field} object is not closed");
        }

        static int JsonArrayEnd(string json, int start, string field)
        {
            var depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '"')
                    SkipJsonString(json, ref i);
                else if (json[i] == '[')
                    depth++;
                else if (json[i] == ']')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            throw new Exception($"Invalid UI redesign draft JSON: {field} array is not closed");
        }

        static void RequireNoTrailingContent(string json, int rootEnd)
        {
            for (int i = rootEnd + 1; i < json.Length; i++)
            {
                if (!char.IsWhiteSpace(json[i]))
                    throw new Exception("Invalid UI redesign draft JSON: trailing content after draft root");
            }
        }

        static bool MatchesJsonField(string json, string field, ref int i)
        {
            if (json[i] != '"')
                return false;
            var start = i + 1;
            var escaped = false;
            SkipJsonString(json, ref i, ref escaped);
            if (escaped || i - start != field.Length || string.Compare(json, start, field, 0, field.Length, StringComparison.Ordinal) != 0)
                return false;
            var colon = i + 1;
            while (colon < json.Length && char.IsWhiteSpace(json[colon]))
                colon++;
            if (colon >= json.Length || json[colon] != ':')
                return false;
            i = colon;
            return true;
        }

        static void SkipJsonString(string json, ref int i)
        {
            var escaped = false;
            SkipJsonString(json, ref i, ref escaped);
        }

        static void SkipJsonString(string json, ref int i, ref bool escaped)
        {
            while (++i < json.Length && json[i] != '"')
            {
                if (json[i] < ' ')
                    throw new Exception("Invalid UI redesign draft JSON: string contains control character");
                if (json[i] != '\\')
                    continue;
                escaped = true;
                if (++i >= json.Length)
                    throw new Exception("Invalid UI redesign draft JSON: string escape is invalid");
                if (json[i] == 'u')
                {
                    for (var n = 1; n <= 4; n++)
                    {
                        if (i + n >= json.Length || !IsJsonHex(json[i + n]))
                            throw new Exception("Invalid UI redesign draft JSON: unicode escape is invalid");
                    }
                    i += 4;
                }
                else if ("\"\\/bfnrt".IndexOf(json[i]) < 0)
                {
                    throw new Exception("Invalid UI redesign draft JSON: string escape is invalid");
                }
            }
            if (i >= json.Length)
                throw new Exception("Invalid UI redesign draft JSON: string is not closed");
        }

        static bool IsJsonHex(char value)
        {
            return value >= '0' && value <= '9' || value >= 'a' && value <= 'f' || value >= 'A' && value <= 'F';
        }

        static string Root(string path)
        {
            return path.TrimEnd('/');
        }

        static string SafeName(string path)
        {
            var name = path.Replace("Assets/Bundle/Prefab/", "").Replace(".prefab", "").Replace('/', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static void ValidateSavedDraft(UIAIToolsProfile profile, UIRedesignRequest request, UIRedesignDraft draft)
        {
            UIRedesignRequestValidation.ValidateSourcePrefab(profile, request.sourcePrefabPath, "draft");
            ValidateDraft(draft);
            ForceConfirmation(draft);
        }

        static string DraftPath(UIAIToolsProfile profile, UIRedesignRequest request, string suffix)
        {
            return UIReportFiles.GetPath(profile.logRoot, $"UIRedesignDraft_{SafeName(request.sourcePrefabPath)}{suffix}.json");
        }

        static string WriteDraft(UIAIToolsProfile profile, UIRedesignDraft draft, string path)
        {
            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllText(path, ToJsonWithRequiredArrays(draft));
            LoadDraft(path);
            Debug.Log($"UI redesign draft saved: {path}, {draft.replacementPlan.items.Count} items.");
            return path;
        }

        internal static string ToJsonWithRequiredArrays(UIRedesignDraft draft)
        {
            return WithReplacementPlanItems(UICreationBriefTemplateService.ToJsonWithRootArrays(draft, "risks"));
        }

        static string WithReplacementPlanItems(string json)
        {
            var rootStart = JsonObjectStart(json);
            var rootEnd = JsonObjectEnd(json, rootStart, "draft root");
            var replacementPlanStart = RequireJsonObjectField(json, rootStart, rootEnd, "replacementPlan");
            var replacementPlanEnd = JsonObjectEnd(json, replacementPlanStart, "replacementPlan");
            if (JsonFieldValueStart(json, replacementPlanStart, replacementPlanEnd, "items") >= 0)
                return json;
            var insertAt = replacementPlanEnd;
            while (insertAt > replacementPlanStart && char.IsWhiteSpace(json[insertAt - 1]))
                insertAt--;
            var separator = HasObjectContent(json, replacementPlanStart, insertAt) ? "," : "";
            return json.Insert(insertAt, $"{separator}\n        \"items\": []");
        }

        static bool HasObjectContent(string json, int objectStart, int objectEnd)
        {
            for (var i = objectStart + 1; i < objectEnd; i++)
            {
                if (!char.IsWhiteSpace(json[i]))
                    return true;
            }
            return false;
        }

        static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }

        static void ForceConfirmation(UIRedesignDraft draft)
        {
            draft.requiresConfirmation = true;
            foreach (var item in draft.replacementPlan.items)
                item.requiresConfirmation = true;
        }
    }
}
