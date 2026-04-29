using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Xipin.UIAITools
{
    public static class UIReportCsv
    {
        public static List<Dictionary<string, string>> ReadRows(string logRoot, string fileName)
        {
            return ReadRows(UIReportFiles.GetPath(logRoot, fileName));
        }

        public static List<Dictionary<string, string>> ReadRows(string path)
        {
            if (!File.Exists(path))
                throw new Exception($"Missing UI AI Tools CSV: {path}");
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0 || string.IsNullOrEmpty(lines[0].TrimStart('\ufeff')))
                throw new Exception($"Empty UI AI Tools CSV header: {path}");
            var headers = SplitLine(lines[0], path, 1).Select(h => h.TrimStart('\ufeff')).ToArray();
            ValidateHeaders(headers, path);
            var rows = new List<Dictionary<string, string>>();
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (line.Length == 0)
                    continue;
                var values = SplitLine(line, path, lineIndex + 1);
                if (values.Length != headers.Length)
                    throw new Exception($"Invalid UI AI Tools CSV column count: {path}:{lineIndex + 1} expected {headers.Length}, got {values.Length}");
                var row = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length; i++)
                    row[headers[i]] = values[i];
                rows.Add(row);
            }
            return rows;
        }

        static void ValidateHeaders(string[] headers, string path)
        {
            var seen = new HashSet<string>();
            foreach (var header in headers)
            {
                if (string.IsNullOrEmpty(header))
                    throw new Exception($"Invalid UI AI Tools CSV empty header: {path}:1");
                if (!seen.Add(header))
                    throw new Exception($"Duplicate UI AI Tools CSV header: {path}:1 {header}");
            }
        }

        static string[] SplitLine(string line, string path, int lineNumber)
        {
            var values = new List<string>();
            var value = new StringBuilder();
            var quoted = false;
            var quotedValue = false;
            var afterQuote = false;
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                        afterQuote = true;
                    }
                    else
                    {
                        value.Append(c);
                    }
                    continue;
                }
                if (afterQuote)
                {
                    if (c == ',')
                    {
                        values.Add(value.ToString());
                        value.Length = 0;
                        quotedValue = false;
                        afterQuote = false;
                        continue;
                    }
                    throw new Exception($"Invalid UI AI Tools CSV quote: {path}:{lineNumber}");
                }
                if (c == ',')
                {
                    values.Add(value.ToString());
                    value.Length = 0;
                    quotedValue = false;
                }
                else if (c == '"')
                {
                    if (value.Length > 0 || quotedValue)
                        throw new Exception($"Invalid UI AI Tools CSV quote: {path}:{lineNumber}");
                    quoted = true;
                    quotedValue = true;
                }
                else
                {
                    value.Append(c);
                }
            }
            if (quoted)
                throw new Exception($"Invalid UI AI Tools CSV quote: {path}:{lineNumber}");
            values.Add(value.ToString());
            return values.ToArray();
        }
    }
}
