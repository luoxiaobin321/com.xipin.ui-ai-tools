using System;
using System.Collections.Generic;

namespace Xipin.UIAITools
{
    [Serializable]
    public class UICreationBrief
    {
        public string featureName;
        public string uiType;
        public string targetFolder;
        public string stylePrompt;
        public List<string> referenceImagePaths = new List<string>();
        public List<string> requiredInteractions = new List<string>();
        public List<string> dataBindings = new List<string>();
        public List<string> constraints = new List<string>();
        public bool requiresConfirmation = true;
    }

    [Serializable]
    public class UILayoutDraft
    {
        public UILayoutRoot root = new UILayoutRoot();
        public List<UILayoutNode> nodes = new List<UILayoutNode>();
        public List<UICreationAssetNeed> assets = new List<UICreationAssetNeed>();
        public List<string> interactions = new List<string>();
        public List<string> risks = new List<string>();
        public bool requiresConfirmation = true;
    }

    [Serializable]
    public class UILayoutRoot
    {
        public string name;
        public string uiType;
        public string targetFolder;
        public string referenceResolution;
        public string safeAreaPolicy;
    }

    [Serializable]
    public class UILayoutNode
    {
        public string nodeId;
        public string parentId;
        public string name;
        public string componentRole;
        public string componentId;
        public string anchor;
        public string position;
        public string size;
        public string state;
        public string text;
        public string dataBinding;
        public string assetPath;
    }

    [Serializable]
    public class UICreationAssetNeed
    {
        public string needId;
        public string kind;
        public string path;
        public string source;
        public string status;
        public string reason;
    }
}
