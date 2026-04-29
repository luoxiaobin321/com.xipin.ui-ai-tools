using System;
using System.IO;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UICreationBriefTemplateService
    {
        public static string Generate(UIAIToolsProfile profile, UICreationBrief brief)
        {
            UIComponentCandidateIndexService.Validate(profile);
            ValidateBrief(brief);
            brief.requiresConfirmation = true;
            var path = UIReportFiles.GetPath(profile.logRoot, $"UICreationBriefTemplate_{SafeName(brief.featureName)}.json");
            Directory.CreateDirectory(profile.logRoot);
            File.WriteAllText(path, ToJsonWithRootArrays(brief, "referenceImagePaths", "requiredInteractions", "dataBindings", "constraints"));
            LoadBrief(path);
            Debug.Log($"UI creation brief template generated: {path}");
            return path;
        }

        public static UICreationBrief LoadBrief(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new Exception("Missing UI creation brief JSON: " + path);
            var json = File.ReadAllText(path);
            ValidateBriefJson(json);
            var brief = FromJson<UICreationBrief>(json, "brief");
            ValidateBrief(brief);
            brief.requiresConfirmation = true;
            return brief;
        }

        public static void ValidateBrief(UICreationBrief brief)
        {
            if (brief == null)
                throw new Exception("Invalid UI creation brief: missing brief root");
            RequireValue(brief.featureName, "featureName");
            RequireValue(brief.uiType, "uiType");
            RequireAssetsFolder(brief.targetFolder, "targetFolder");
            if (brief.referenceImagePaths == null || brief.requiredInteractions == null || brief.dataBindings == null || brief.constraints == null)
                throw new Exception("Invalid UI creation brief: list fields are required");
        }

        internal static void RequireValue(string value, string field)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception($"Invalid UI creation brief: {field} is required");
        }

        internal static void RequireAssetsFolder(string value, string field)
        {
            RequireValue(value, field);
            if (value.Contains("\\") || !value.StartsWith("Assets/", StringComparison.Ordinal))
                throw new Exception($"Invalid UI creation brief: {field} must be an Assets/ path");
            if (value.Contains("/../") || value.EndsWith("/..", StringComparison.Ordinal))
                throw new Exception($"Invalid UI creation brief: {field} cannot contain .. path segments");
        }

        static void ValidateBriefJson(string json)
        {
            var rootStart = JsonObjectStart(json, "brief");
            var rootEnd = JsonObjectEnd(json, rootStart, "brief");
            RequireNoTrailingContent(json, rootEnd, "brief");
            RequireJsonStringField(json, rootStart, rootEnd, "featureName", "featureName", "brief");
            RequireJsonStringField(json, rootStart, rootEnd, "uiType", "uiType", "brief");
            RequireJsonStringField(json, rootStart, rootEnd, "targetFolder", "targetFolder", "brief");
            RequireJsonStringField(json, rootStart, rootEnd, "stylePrompt", "stylePrompt", "brief");
            RequireJsonOptionalBooleanField(json, rootStart, rootEnd, "requiresConfirmation", "requiresConfirmation", "brief");
            RequireJsonArrayField(json, rootStart, rootEnd, "referenceImagePaths", "referenceImagePaths", "brief");
            RequireJsonArrayField(json, rootStart, rootEnd, "requiredInteractions", "requiredInteractions", "brief");
            RequireJsonArrayField(json, rootStart, rootEnd, "dataBindings", "dataBindings", "brief");
            RequireJsonArrayField(json, rootStart, rootEnd, "constraints", "constraints", "brief");
            ValidateRootJsonStringArrayItems(json, "brief", "referenceImagePaths");
            ValidateRootJsonStringArrayItems(json, "brief", "requiredInteractions");
            ValidateRootJsonStringArrayItems(json, "brief", "dataBindings");
            ValidateRootJsonStringArrayItems(json, "brief", "constraints");
            ValidateJsonObjectMembers(json, rootStart, rootEnd, "Invalid UI creation brief JSON", "brief");
        }

        internal static void ValidateLayoutDraftJson(string json)
        {
            var rootStart = JsonObjectStart(json, "layout draft");
            var rootEnd = JsonObjectEnd(json, rootStart, "layout draft");
            RequireNoTrailingContent(json, rootEnd, "layout draft");
            var layoutRootStart = RequireJsonObjectField(json, rootStart, rootEnd, "root", "layout draft");
            var layoutRootEnd = JsonObjectEnd(json, layoutRootStart, "layout draft root");
            RequireJsonStringField(json, layoutRootStart, layoutRootEnd, "name", "root.name", "layout draft");
            RequireJsonStringField(json, layoutRootStart, layoutRootEnd, "uiType", "root.uiType", "layout draft");
            RequireJsonStringField(json, layoutRootStart, layoutRootEnd, "targetFolder", "root.targetFolder", "layout draft");
            RequireJsonStringField(json, layoutRootStart, layoutRootEnd, "referenceResolution", "root.referenceResolution", "layout draft");
            RequireJsonStringField(json, layoutRootStart, layoutRootEnd, "safeAreaPolicy", "root.safeAreaPolicy", "layout draft");
            RequireJsonArrayField(json, rootStart, rootEnd, "nodes", "nodes", "layout draft");
            RequireJsonArrayField(json, rootStart, rootEnd, "assets", "assets", "layout draft");
            RequireJsonArrayField(json, rootStart, rootEnd, "interactions", "interactions", "layout draft");
            RequireJsonArrayField(json, rootStart, rootEnd, "risks", "risks", "layout draft");
            RequireJsonBooleanField(json, rootStart, rootEnd, "requiresConfirmation", "requiresConfirmation", "layout draft");
            ValidateRootJsonStringArrayItems(json, "layout draft", "interactions");
            ValidateRootJsonStringArrayItems(json, "layout draft", "risks");
            ValidateRootJsonObjectArrayItems(json, "layout draft", "nodes", new[]
            {
                "nodeId",
                "name",
                "componentRole",
                "componentId",
                "anchor",
                "position",
                "size"
            }, new[] { "parentId", "state", "text", "dataBinding", "assetPath" });
            ValidateRootJsonObjectArrayItems(json, "layout draft", "assets", new[]
            {
                "needId",
                "kind",
                "path",
                "source",
                "status",
                "reason"
            }, new string[0]);
            ValidateJsonObjectMembers(json, layoutRootStart, layoutRootEnd, "Invalid UI creation layout draft JSON", "root");
            ValidateJsonObjectMembers(json, rootStart, rootEnd, "Invalid UI creation layout draft JSON", "layout draft");
        }

        internal static T FromJson<T>(string json, string label)
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException exception)
            {
                throw new Exception($"Invalid UI creation {label} JSON: {exception.Message}");
            }
        }

        internal static string ToJsonWithRootArrays(object value, params string[] fields)
        {
            var json = JsonUtility.ToJson(value, true);
            foreach (var field in fields)
                json = WithRootArray(json, field);
            return json;
        }

        internal static void ValidateRootJson(string json, string label, string[] fields, string[] arrayFields)
        {
            var rootStart = JsonObjectStart(json, label);
            var rootEnd = JsonObjectEnd(json, rootStart, label);
            RequireNoTrailingContent(json, rootEnd, label);
            foreach (var field in fields)
                RequireJsonStringField(json, rootStart, rootEnd, field, field, label);
            foreach (var field in arrayFields)
                RequireJsonArrayField(json, rootStart, rootEnd, field, field, label);
            ValidateJsonObjectMembers(json, rootStart, rootEnd, $"Invalid UI creation {label} JSON", label);
        }

        internal static void ValidateRootJsonArrayItems(string json, string label, string arrayField, string[] stringFields)
        {
            ValidateRootJsonObjectArrayItems(json, label, arrayField, stringFields, new string[0]);
        }

        internal static void ValidateRootJsonStringArrayItems(string json, string label, string arrayField)
        {
            var rootStart = JsonObjectStart(json, label);
            var rootEnd = JsonObjectEnd(json, rootStart, label);
            var arrayStart = RequireJsonArrayField(json, rootStart, rootEnd, arrayField, arrayField, label);
            var arrayEnd = JsonArrayEnd(json, arrayStart, arrayField);
            var expectItem = true;
            var hasItem = false;
            for (var i = arrayStart + 1; i < arrayEnd; i++)
            {
                if (char.IsWhiteSpace(json[i]))
                    continue;
                if (!expectItem)
                {
                    if (json[i] != ',')
                        throw new Exception($"Invalid UI creation {label} JSON: {arrayField} items must be separated by commas");
                    expectItem = true;
                    continue;
                }
                if (json[i] == ',')
                    throw new Exception($"Invalid UI creation {label} JSON: {arrayField} item is missing");
                if (json[i] != '"')
                    throw new Exception($"Invalid UI creation {label} JSON: {arrayField} item must be a string");
                SkipJsonString(json, ref i);
                expectItem = false;
                hasItem = true;
            }
            if (expectItem && hasItem)
                throw new Exception($"Invalid UI creation {label} JSON: {arrayField} item is missing");
        }

        internal static void ValidateRootJsonObjectArrayItems(string json, string label, string arrayField, string[] requiredStringFields, string[] optionalStringFields)
        {
            ValidateRootJsonObjectArrayItems(json, label, arrayField, requiredStringFields, optionalStringFields, new string[0]);
        }

        internal static void ValidateRootJsonObjectArrayItems(string json, string label, string arrayField, string[] requiredStringFields, string[] optionalStringFields, string[] requiredIntegerFields)
        {
            var rootStart = JsonObjectStart(json, label);
            var rootEnd = JsonObjectEnd(json, rootStart, label);
            var arrayStart = RequireJsonArrayField(json, rootStart, rootEnd, arrayField, arrayField, label);
            var arrayEnd = JsonArrayEnd(json, arrayStart, arrayField);
            var expectItem = true;
            var hasItem = false;
            for (var i = arrayStart + 1; i < arrayEnd; i++)
            {
                if (char.IsWhiteSpace(json[i]))
                    continue;
                if (!expectItem)
                {
                    if (json[i] != ',')
                        throw new Exception($"Invalid UI creation {label} JSON: {arrayField} items must be separated by commas");
                    expectItem = true;
                    continue;
                }
                if (json[i] == ',')
                    throw new Exception($"Invalid UI creation {label} JSON: {arrayField} item is missing");
                if (json[i] != '{')
                    throw new Exception($"Invalid UI creation {label} JSON: {arrayField} item must be an object");
                var itemEnd = JsonObjectEnd(json, i, arrayField + " item");
                foreach (var field in requiredStringFields)
                    RequireJsonStringField(json, i, itemEnd, field, arrayField + "." + field, label);
                foreach (var field in optionalStringFields)
                    RequireJsonOptionalStringField(json, i, itemEnd, field, arrayField + "." + field, label);
                foreach (var field in requiredIntegerFields)
                    RequireJsonIntegerField(json, i, itemEnd, field, arrayField + "." + field, label);
                ValidateJsonObjectMembers(json, i, itemEnd, $"Invalid UI creation {label} JSON", arrayField + " item");
                i = itemEnd;
                expectItem = false;
                hasItem = true;
            }
            if (expectItem && hasItem)
                throw new Exception($"Invalid UI creation {label} JSON: {arrayField} item is missing");
        }

        internal static void ValidateJsonObjectMembers(string json, int objectStart, int objectEnd, string errorPrefix, string label)
        {
            var expectField = true;
            var hasField = false;
            for (var i = objectStart + 1; i < objectEnd;)
            {
                while (i < objectEnd && char.IsWhiteSpace(json[i]))
                    i++;
                if (i >= objectEnd)
                    break;
                if (!expectField)
                {
                    if (json[i] != ',')
                        throw new Exception($"{errorPrefix}: {label} fields must be separated by commas");
                    i++;
                    expectField = true;
                    continue;
                }
                if (json[i] == ',')
                    throw new Exception($"{errorPrefix}: {label} field is missing");
                if (json[i] != '"')
                    throw new Exception($"{errorPrefix}: {label} field name must be a string");
                SkipJsonString(json, ref i);
                var colon = i + 1;
                while (colon < objectEnd && char.IsWhiteSpace(json[colon]))
                    colon++;
                if (colon >= objectEnd || json[colon] != ':')
                    throw new Exception($"{errorPrefix}: {label} field separator is missing");
                i = JsonValueEnd(json, colon + 1, objectEnd, errorPrefix, label);
                expectField = false;
                hasField = true;
            }
            if (expectField && hasField)
                throw new Exception($"{errorPrefix}: {label} field is missing");
        }

        static int JsonValueEnd(string json, int start, int objectEnd, string errorPrefix, string label)
        {
            while (start < objectEnd && char.IsWhiteSpace(json[start]))
                start++;
            if (start >= objectEnd)
                throw new Exception($"{errorPrefix}: {label} field value is missing");
            if (json[start] == '"')
            {
                var end = start;
                SkipJsonString(json, ref end);
                return end + 1;
            }
            if (json[start] == '{')
                return JsonObjectEnd(json, start, label) + 1;
            if (json[start] == '[')
                return JsonArrayEnd(json, start, label) + 1;
            var i = start;
            while (i < objectEnd && !char.IsWhiteSpace(json[i]) && json[i] != ',')
                i++;
            return i;
        }

        static int JsonObjectStart(string json, string label)
        {
            var start = 0;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            if (start >= json.Length || json[start] != '{')
                throw new Exception($"Invalid UI creation {label} JSON: missing {label} root");
            return start;
        }

        static int JsonObjectEnd(string json, int start, string label)
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
            throw new Exception($"Invalid UI creation {label} JSON: {label} root is not closed");
        }

        static int JsonArrayEnd(string json, int start, string label)
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
            throw new Exception($"Invalid UI creation {label} JSON: {label} array is not closed");
        }

        static string WithRootArray(string json, string field)
        {
            var rootStart = JsonObjectStart(json, "serialized object");
            var rootEnd = JsonObjectEnd(json, rootStart, "serialized object");
            if (JsonFieldValueStart(json, rootStart, rootEnd, field) >= 0)
                return json;
            var insertAt = rootEnd;
            while (insertAt > rootStart && char.IsWhiteSpace(json[insertAt - 1]))
                insertAt--;
            var separator = HasObjectContent(json, rootStart, insertAt) ? "," : "";
            return json.Insert(insertAt, $"{separator}\n    \"{field}\": []");
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

        static void RequireNoTrailingContent(string json, int rootEnd, string label)
        {
            for (int i = rootEnd + 1; i < json.Length; i++)
            {
                if (!char.IsWhiteSpace(json[i]))
                    throw new Exception($"Invalid UI creation {label} JSON: trailing content after {label} root");
            }
        }

        static void RequireJsonStringField(string json, int objectStart, int objectEnd, string field, string label, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} is required");
            if (!JsonStringAt(json, valueStart))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} must be a string");
        }

        static void RequireJsonOptionalStringField(string json, int objectStart, int objectEnd, string field, string label, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart >= 0 && !JsonStringAt(json, valueStart))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} must be a string");
        }

        static void RequireJsonBooleanField(string json, int objectStart, int objectEnd, string field, string label, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} is required");
            if (!JsonStartsWith(json, valueStart, "true") && !JsonStartsWith(json, valueStart, "false"))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} must be a boolean");
        }

        static void RequireJsonOptionalBooleanField(string json, int objectStart, int objectEnd, string field, string label, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart >= 0 && !JsonStartsWith(json, valueStart, "true") && !JsonStartsWith(json, valueStart, "false"))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} must be a boolean");
        }

        static void RequireJsonIntegerField(string json, int objectStart, int objectEnd, string field, string label, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} is required");
            if (!JsonIntegerAt(json, valueStart))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} must be an integer");
        }

        static int RequireJsonObjectField(string json, int objectStart, int objectEnd, string field, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {field} is required");
            if (!JsonObjectAt(json, valueStart, field))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {field} must be an object");
            return valueStart;
        }

        static int RequireJsonArrayField(string json, int objectStart, int objectEnd, string field, string label, string rootLabel)
        {
            var valueStart = JsonFieldValueStart(json, objectStart, objectEnd, field);
            if (valueStart < 0)
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} is required");
            if (!JsonArrayAt(json, valueStart, field))
                throw new Exception($"Invalid UI creation {rootLabel} JSON: {label} must be an array");
            return valueStart;
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
                                throw new Exception($"Invalid UI creation JSON: {field} is duplicated");
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

        static bool JsonStartsWith(string json, int start, string value)
        {
            if (start + value.Length > json.Length || string.Compare(json, start, value, 0, value.Length, StringComparison.Ordinal) != 0)
                return false;
            var next = start + value.Length;
            return JsonValueEndsAt(json, next);
        }

        static bool JsonIntegerAt(string json, int start)
        {
            var i = start;
            if (i < json.Length && json[i] == '-')
                i++;
            var digitStart = i;
            while (i < json.Length && IsJsonDigit(json[i]))
                i++;
            if (i > digitStart + 1 && json[digitStart] == '0')
                return false;
            return i > digitStart && JsonValueEndsAt(json, i);
        }

        static bool JsonStringAt(string json, int start)
        {
            if (start >= json.Length || json[start] != '"')
                return false;
            var end = start;
            SkipJsonString(json, ref end);
            return end < json.Length && JsonValueEndsAt(json, end + 1);
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
                    throw new Exception("Invalid UI creation JSON: string contains control character");
                if (json[i] != '\\')
                    continue;
                escaped = true;
                if (++i >= json.Length)
                    throw new Exception("Invalid UI creation JSON: string escape is invalid");
                if (json[i] == 'u')
                {
                    for (var n = 1; n <= 4; n++)
                    {
                        if (i + n >= json.Length || !IsJsonHex(json[i + n]))
                            throw new Exception("Invalid UI creation JSON: unicode escape is invalid");
                    }
                    i += 4;
                }
                else if ("\"\\/bfnrt".IndexOf(json[i]) < 0)
                {
                    throw new Exception("Invalid UI creation JSON: string escape is invalid");
                }
            }
            if (i >= json.Length)
                throw new Exception("Invalid UI creation JSON: string is not closed");
        }

        static bool IsJsonHex(char value)
        {
            return value >= '0' && value <= '9' || value >= 'a' && value <= 'f' || value >= 'A' && value <= 'F';
        }

        static bool IsJsonDigit(char value)
        {
            return value >= '0' && value <= '9';
        }

        static string SafeName(string value)
        {
            var name = value;
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
