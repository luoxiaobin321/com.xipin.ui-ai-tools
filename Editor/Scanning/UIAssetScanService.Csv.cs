using System.Collections.Generic;
using System.Linq;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    static string Csv(string value)
    {
        value = value ?? "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    static string Join(List<string> list)
    {
        return string.Join(";", list.Take(6)) + (list.Count > 6 ? ";..." : "");
    }

    static string JoinAll(IEnumerable<string> list)
    {
        return string.Join(";", list);
    }
}
}
