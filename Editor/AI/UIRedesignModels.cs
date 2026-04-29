using System;
using System.Collections.Generic;

namespace Xipin.UIAITools
{
    [Serializable]
    public class UIRedesignRequest
    {
        public string sourcePrefabPath;
        public string sourcePreviewPath;
        public string stylePrompt;
        public string inputImageFolder;
        public string reuseCandidateReportPath;
        public string outputFolder;
        public List<string> referenceImagePaths = new List<string>();
    }

    [Serializable]
    public class UIRedesignDraft
    {
        public string draftPreviewPath;
        public string generatedImageFolder;
        public UIReplacementPlan replacementPlan = new UIReplacementPlan();
        public bool requiresConfirmation = true;
        public List<string> risks = new List<string>();
    }

    [Serializable]
    public class UIReplacementPlan
    {
        public List<UIReplacementItem> items = new List<UIReplacementItem>();
    }

    [Serializable]
    public class UIReplacementItem
    {
        public string oldAssetPath;
        public string newAssetPath;
        public string targetAtlasPath;
        public bool preserveGuid;
        public bool requiresConfirmation = true;
        public string reason;
    }
}
