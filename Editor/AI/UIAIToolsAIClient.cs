using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Xipin.UIAITools
{
    public static class UIAIToolsAIClient
    {
        public static UIAIToolsAIResponse RequestText(string prompt, List<UIAIToolsAIImage> images)
        {
            var settings = UIAIToolsAISettingsService.Load();
            if (string.IsNullOrEmpty(settings.ApiKey))
                throw new Exception("UIAITools AI API Key is not configured.");

            var requestBody = BuildRequestBody(settings.Model, prompt, images);
            var request = (HttpWebRequest)WebRequest.Create(settings.ResponsesUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "Bearer " + settings.ApiKey;
            request.Timeout = 120000;
            request.ReadWriteTimeout = 120000;
            var bytes = Encoding.UTF8.GetBytes(requestBody);
            using (var stream = request.GetRequestStream())
                stream.Write(bytes, 0, bytes.Length);

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var raw = reader.ReadToEnd();
                    return new UIAIToolsAIResponse(ExtractOutputText(raw), raw);
                }
            }
            catch (WebException exception)
            {
                var message = exception.Message;
                if (exception.Response != null)
                {
                    using (var stream = exception.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                        message = reader.ReadToEnd();
                }
                throw new Exception("UIAITools AI request failed: " + message);
            }
        }

        static string BuildRequestBody(string model, string prompt, List<UIAIToolsAIImage> images)
        {
            var builder = new StringBuilder();
            builder.Append("{\"model\":\"").Append(Json(model)).Append("\",\"input\":[{\"role\":\"user\",\"content\":[");
            builder.Append("{\"type\":\"input_text\",\"text\":\"").Append(Json(prompt)).Append("\"}");
            foreach (var image in images)
            {
                builder.Append(",{\"type\":\"input_image\",\"image_url\":\"")
                    .Append(Json(image.DataUrl))
                    .Append("\"}");
            }
            builder.Append("]}]}");
            return builder.ToString();
        }

        static string ExtractOutputText(string raw)
        {
            var marker = "\"type\":\"output_text\"";
            var markerIndex = raw.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                markerIndex = raw.IndexOf("\"output_text\"", StringComparison.Ordinal);
            if (markerIndex < 0)
                throw new Exception("UIAITools AI response did not contain output_text.");
            var textIndex = raw.IndexOf("\"text\"", markerIndex, StringComparison.Ordinal);
            if (textIndex < 0)
                throw new Exception("UIAITools AI response did not contain output text.");
            var colon = raw.IndexOf(':', textIndex);
            if (colon < 0)
                throw new Exception("UIAITools AI response output text is malformed.");
            var start = colon + 1;
            while (start < raw.Length && char.IsWhiteSpace(raw[start]))
                start++;
            if (start >= raw.Length || raw[start] != '"')
                throw new Exception("UIAITools AI response output text is malformed.");
            return ReadJsonString(raw, start);
        }

        static string ReadJsonString(string json, int quoteIndex)
        {
            var builder = new StringBuilder();
            for (var i = quoteIndex + 1; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '"')
                    return builder.ToString();
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }
                if (++i >= json.Length)
                    throw new Exception("UIAITools AI response string escape is malformed.");
                var escaped = json[i];
                if (escaped == '"' || escaped == '\\' || escaped == '/')
                    builder.Append(escaped);
                else if (escaped == 'b')
                    builder.Append('\b');
                else if (escaped == 'f')
                    builder.Append('\f');
                else if (escaped == 'n')
                    builder.Append('\n');
                else if (escaped == 'r')
                    builder.Append('\r');
                else if (escaped == 't')
                    builder.Append('\t');
                else if (escaped == 'u')
                {
                    if (i + 4 >= json.Length)
                        throw new Exception("UIAITools AI response unicode escape is malformed.");
                    builder.Append((char)Convert.ToInt32(json.Substring(i + 1, 4), 16));
                    i += 4;
                }
            }
            throw new Exception("UIAITools AI response output text is not closed.");
        }

        static string Json(string value)
        {
            if (value == null)
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }
    }

    public sealed class UIAIToolsAIImage
    {
        public readonly string Path;
        public readonly string DataUrl;

        public UIAIToolsAIImage(string path, string dataUrl)
        {
            Path = path;
            DataUrl = dataUrl;
        }
    }

    public sealed class UIAIToolsAIResponse
    {
        public readonly string Text;
        public readonly string RawResponse;

        public UIAIToolsAIResponse(string text, string rawResponse)
        {
            Text = text;
            RawResponse = rawResponse;
        }
    }
}
