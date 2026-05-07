using System;
using System.Collections.Generic;
using System.Linq;

namespace Xipin.UIAITools
{
    static class UIReportMarkdown
    {
        public static void AddCheckSummary(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var group in rows.GroupBy(r => r["Check"])
                         .OrderByDescending(g => g.Count())
                         .ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        public static void AddSeverityCheckSummary(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var group in rows.GroupBy(r => new { Severity = r["Severity"], Check = r["Check"] })
                         .OrderBy(g => SeverityOrder(g.Key.Severity))
                         .ThenByDescending(g => g.Count())
                         .ThenBy(g => g.Key.Check))
                lines.Add($"- {group.Key.Severity} / {group.Key.Check}：{group.Count()}");
            lines.Add("");
        }

        public static void AddNotePrefixSummary(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var group in rows.GroupBy(r => NotePrefix(r["Note"]))
                         .OrderByDescending(g => g.Count())
                         .ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        public static void AddNoteRiskSummary(List<string> lines, string title, List<Dictionary<string, string>> rows)
        {
            lines.Add($"## {title}");
            if (rows.Count == 0)
            {
                lines.Add("- 无");
                lines.Add("");
                return;
            }

            foreach (var group in rows.SelectMany(r => NoteRiskLabels(r["Note"]))
                         .GroupBy(label => label)
                         .OrderByDescending(g => g.Count())
                         .ThenBy(g => g.Key))
                lines.Add($"- {group.Key}：{group.Count()}");
            lines.Add("");
        }

        public static IEnumerable<string> NoteRiskLabels(string note)
        {
            return note.Split(new[] { '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => NotePrefix(part.Trim()))
                .Where(label => label.Length > 0);
        }

        public static void RequireExactSectionOrder(string report, string[] lines, params string[] sections)
        {
            var reportSections = ReportSections(lines);
            var duplicate = reportSections.GroupBy(section => section.line).FirstOrDefault(group => sections.Contains(group.Key) && group.Count() > 1);
            if (duplicate != null)
            {
                var section = duplicate.Skip(1).First();
                throw new Exception($"{report} has duplicate section at line {section.lineNumber}: {section.line}");
            }
            var count = Math.Min(reportSections.Count, sections.Length);
            for (var i = 0; i < count; i++)
            {
                if (reportSections[i].line == sections[i])
                    continue;
                if (!sections.Contains(reportSections[i].line))
                    throw new Exception($"{report} has unexpected section at line {reportSections[i].lineNumber}: {reportSections[i].line}");
                if (!reportSections.Any(section => section.line == sections[i]))
                    throw new Exception($"{report} is missing section: {sections[i]}");
                throw new Exception($"{report} section is out of order at line {reportSections[i].lineNumber}: expected {sections[i]}, found {reportSections[i].line}");
            }
            if (reportSections.Count < sections.Length)
                throw new Exception($"{report} is missing section: {sections[reportSections.Count]}");
            if (reportSections.Count > sections.Length)
                throw new Exception($"{report} has unexpected section at line {reportSections[sections.Length].lineNumber}: {reportSections[sections.Length].line}");
        }

        static List<MarkdownSection> ReportSections(string[] lines)
        {
            var sections = new List<MarkdownSection>();
            var fenced = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    fenced = !fenced;
                    continue;
                }
                if (!fenced && line.StartsWith("## ", StringComparison.Ordinal))
                    sections.Add(new MarkdownSection { line = line, lineNumber = i + 1 });
            }
            return sections;
        }

        static string NotePrefix(string note)
        {
            var index = note.IndexOf('：');
            return index > 0 ? note.Substring(0, index) : note;
        }

        static int SeverityOrder(string severity)
        {
            switch (severity)
            {
                case "Error":
                    return 0;
                case "Warning":
                    return 1;
                case "Review":
                    return 2;
                case "Info":
                    return 3;
                default:
                    return 4;
            }
        }

        struct MarkdownSection
        {
            public string line;
            public int lineNumber;
        }
    }
}
