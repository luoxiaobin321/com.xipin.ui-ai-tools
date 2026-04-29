using System;
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
            UIReportMarkdown.RequireExactSectionOrder("host apply result contract self-test", new[]
            {
                "# UI 替换宿主执行结果",
                "## 总览",
                "## 状态分布",
                "## 失败项",
                "## 执行项",
                "## 执行后复验"
            }, "## 总览", "## 状态分布", "## 失败项", "## 执行项", "## 执行后复验");
            UIReportMarkdown.RequireExactSectionOrder("UI creation host generate result contract self-test", new[]
            {
                "# UI 生成宿主结果",
                "## 目标",
                "## 状态分布",
                "## 下一步"
            }, "## 目标", "## 状态分布", "## 下一步");
            RequireFailure("missing", new[] { "## A" }, "markdown contract self-test is missing section: ## B");
            RequireFailure("unexpected", new[] { "## A", "## C" }, "markdown contract self-test has unexpected section at line 2: ## C");
            RequireFailure("out-of-order", new[] { "## B", "## A" }, "markdown contract self-test section is out of order at line 1: expected ## A, found ## B");
            Debug.Log("UI report markdown section contract validation passed.");
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
