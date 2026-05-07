using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xipin.UIAITools
{
    [CreateAssetMenu(menuName = "Xipin/UI AI Tools/Profile")]
    public class UIAIToolsProfile : ScriptableObject
    {
        public string workspaceRoot = "Assets/UIAITools";
        public string prefabRoot = "Assets/Bundle/Prefab";
        public string artUIRoot = "Assets/Art/UI";
        public string uiAtlasRoot = "Assets/Bundle/UIAtlas";
        public string uiTextureRoot = "Assets/Bundle/UITexture";
        public string mapTextureRoot = "Assets/Bundle/MapTexture";
        public string logRoot = "UIAIToolsReports";
        public string yooAssetAddressRule = "AddressByFileName";
        public string projectStylePrompt = "Use the shared project art style, target platform, and slicing constraints.";
        public List<string> textSearchRoots = new List<string> { "Assets/Scripts", "Assets/Bundle/Config", "Assets/Bundle/Setting" };
        public bool excludeMapFromUITriage = true;

        public static void ValidateContract()
        {
            var profile = CreateInstance<UIAIToolsProfile>();
            Expect("workspaceRoot", profile.workspaceRoot, "Assets/UIAITools");
            Expect("prefabRoot", profile.prefabRoot, "Assets/Bundle/Prefab");
            Expect("artUIRoot", profile.artUIRoot, "Assets/Art/UI");
            Expect("uiAtlasRoot", profile.uiAtlasRoot, "Assets/Bundle/UIAtlas");
            Expect("uiTextureRoot", profile.uiTextureRoot, "Assets/Bundle/UITexture");
            Expect("mapTextureRoot", profile.mapTextureRoot, "Assets/Bundle/MapTexture");
            Expect("logRoot", profile.logRoot, "UIAIToolsReports");
            Expect("yooAssetAddressRule", profile.yooAssetAddressRule, "AddressByFileName");
            Expect("projectStylePrompt", profile.projectStylePrompt, "Use the shared project art style, target platform, and slicing constraints.");
            ExpectTextRoot(profile, "Assets/Scripts");
            ExpectTextRoot(profile, "Assets/Bundle/Config");
            ExpectTextRoot(profile, "Assets/Bundle/Setting");
            if (!profile.excludeMapFromUITriage)
                throw new Exception("UI AI Tools profile contract failed: excludeMapFromUITriage");
            Debug.Log("UI AI Tools profile contract validation passed.");
        }

        static void Expect(string field, string actual, string expected)
        {
            if (actual != expected)
                throw new Exception($"UI AI Tools profile contract failed: {field}");
        }

        static void ExpectTextRoot(UIAIToolsProfile profile, string expected)
        {
            if (!profile.textSearchRoots.Contains(expected))
                throw new Exception("UI AI Tools profile contract failed: textSearchRoots " + expected);
        }
    }
}
