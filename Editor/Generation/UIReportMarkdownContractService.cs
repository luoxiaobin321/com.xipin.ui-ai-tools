using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xipin.UIAITools
{
    public static class UIReportMarkdownContractService
    {
        public static void RequireExactSectionOrder(string report, string[] lines, params string[] sections)
        {
            UIReportMarkdown.RequireExactSectionOrder(report, lines, sections);
        }

        public static void ValidateExactSectionOrder()
        {
            UIReportMarkdown.RequireExactSectionOrder("markdown contract self-test", new[] { "# Report", "", "## A", "- ok", "## B" }, "## A", "## B");
            UIReportMarkdown.RequireExactSectionOrder("markdown fenced heading self-test", new[] { "# Report", "```json", "## Ignored", "```", "## A", "## B" }, "## A", "## B");
            UIReportMarkdown.RequireExactSectionOrder("UI creation host generate result contract self-test", new[]
            {
                "# UI 生成宿主结果",
                "## 输入",
                "## 状态分布",
                "## 下一步"
            }, "## 输入", "## 状态分布", "## 下一步");
            ValidateSummaryHelpers();
            RequireFailure("missing", new[] { "## A" }, "markdown contract self-test is missing section: ## B");
            RequireFailure("unexpected", new[] { "## A", "## C" }, "markdown contract self-test has unexpected section at line 2: ## C");
            RequireFailure("duplicate", new[] { "## A", "## B", "## B" }, "markdown contract self-test has duplicate section at line 3: ## B");
            RequireFailure("out-of-order", new[] { "## B", "## A" }, "markdown contract self-test section is out of order at line 1: expected ## A, found ## B");
            Debug.Log("UI report markdown section contract validation passed.");
        }

        static void ValidateSummaryHelpers()
        {
            var lines = new List<string>();
            var rows = new List<Dictionary<string, string>>
            {
                Row("Warning", "Size", "风险：A"),
                Row("Warning", "Size", "风险：B"),
                Row("Warning", "Atlas", "风险：C"),
                Row("Review", "Other", "人工确认：A"),
                Row("Review", "Size", "人工确认：B；TextLayoutReview：需要复核文字"),
                Row("Info", "Size", "无冒号")
            };
            UIReportMarkdown.AddCheckSummary(lines, "检查分布", rows);
            UIReportMarkdown.AddSeverityCheckSummary(lines, "Severity 检查分布", rows);
            UIReportMarkdown.AddNotePrefixSummary(lines, "备注前缀分布", rows);
            UIReportMarkdown.AddNoteRiskSummary(lines, "备注风险分布", rows);
            RequireLine(lines, "## 检查分布");
            RequireLine(lines, "- Size：4");
            RequireLine(lines, "- Atlas：1");
            RequireLine(lines, "- Other：1");
            RequireLine(lines, "## Severity 检查分布");
            RequireLine(lines, "- Warning / Size：2");
            RequireLine(lines, "- Warning / Atlas：1");
            RequireLine(lines, "- Review / Other：1");
            RequireLine(lines, "- Review / Size：1");
            RequireLine(lines, "- Info / Size：1");
            RequireLine(lines, "## 备注前缀分布");
            RequireLine(lines, "- 风险：3");
            RequireLine(lines, "- 人工确认：2");
            RequireLine(lines, "- 无冒号：1");
            RequireLine(lines, "## 备注风险分布");
            RequireLine(lines, "- 风险：3");
            RequireLine(lines, "- 人工确认：2");
            RequireLine(lines, "- TextLayoutReview：1");
            RequireLine(lines, "- 无冒号：1");
        }

        static Dictionary<string, string> Row(string severity, string check, string note)
        {
            return new Dictionary<string, string>
            {
                { "Severity", severity },
                { "Check", check },
                { "Note", note }
            };
        }

        static void RequireLine(List<string> lines, string line)
        {
            if (!lines.Contains(line))
                throw new Exception("UI report markdown summary contract is missing: " + line);
        }

        static void RequireFailure(string label, string[] lines, string expectedMessage)
        {
            try
            {
                UIReportMarkdown.RequireExactSectionOrder("markdown contract self-test", lines, "## A", "## B");
            }
            catch (Exception e)
            {
                if (e.Message.Contains(expectedMessage))
                    return;
                throw new Exception($"UI report markdown section contract {label} message mismatch: {e.Message}");
            }
            throw new Exception($"UI report markdown section contract {label} did not fail.");
        }
    }
}
