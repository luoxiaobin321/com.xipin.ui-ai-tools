using System;
using System.Collections.Generic;

namespace Xipin.UIAITools
{
    [Serializable]
    public class UISkinManifest
    {
        public string sourcePrefabPath;
        public string skinName;
        public string skinFolder;
        public string conceptPath;
        public string userPromptPath;
        public string status;
        public UISkinGeneratedPaths generated = new UISkinGeneratedPaths();
    }

    [Serializable]
    public class UISkinGeneratedPaths
    {
        public string finalPromptPath;
        public string detectedLayoutPath;
        public string assetCropsPath;
        public string skinLayoutPath;
        public string outputPrefabPath;
        public string finalPreviewPath;
        public string comparisonPreviewPath;
        public string bindingReportPath;
        public string visualReportPath;
        public string applyChecklistPath;
        public string autoBuildNotesPath;
    }

    [Serializable]
    public class UISkinDetectedLayout
    {
        public string sourceImagePath;
        public int imageWidth;
        public int imageHeight;
        public List<UISkinRegion> regions = new List<UISkinRegion>();
    }

    [Serializable]
    public class UISkinRegion
    {
        public string id;
        public string type;
        public UISkinPixelRect rect = new UISkinPixelRect();
        public float confidence;
        public string note;
    }

    [Serializable]
    public class UISkinAssetCrops
    {
        public string sourceImagePath;
        public List<UISkinCrop> crops = new List<UISkinCrop>();
    }

    [Serializable]
    public class UISkinCrop
    {
        public string id;
        public string sourceRegionId;
        public UISkinPixelRect rect = new UISkinPixelRect();
        public string outputPath;
        public string usage;
        public string note;
    }

    [Serializable]
    public class UISkinLayout
    {
        public string sourcePrefabPath;
        public string outputPrefabPath;
        public string generatedVisualRoot;
        public string referenceResolution;
        public List<UISkinLayer> layers = new List<UISkinLayer>();
        public List<UISkinHideNode> hideNodes = new List<UISkinHideNode>();
        public List<UISkinMoveNode> moveNodes = new List<UISkinMoveNode>();
        public List<UISkinCreateNode> createNodes = new List<UISkinCreateNode>();
        public List<UISkinStyleTextNode> styleTextNodes = new List<UISkinStyleTextNode>();
        public List<UISkinPreserveNode> preserveNodes = new List<UISkinPreserveNode>();
        public List<UISkinValidationSlot> validationSlots = new List<UISkinValidationSlot>();
    }

    [Serializable]
    public class UISkinLayer
    {
        public string name;
        public string purpose;
    }

    [Serializable]
    public class UISkinHideNode
    {
        public string path;
        public string reason;
    }

    [Serializable]
    public class UISkinMoveNode
    {
        public string binding;
        public string targetLayer;
        public string targetRegionId;
        public UISkinRectTransform rect = new UISkinRectTransform();
        public string reason;
    }

    [Serializable]
    public class UISkinCreateNode
    {
        public string name;
        public string type;
        public string targetLayer;
        public string spritePath;
        public string targetRegionId;
        public UISkinRectTransform rect = new UISkinRectTransform();
        public bool raycastTarget;
        public string reason;
    }

    [Serializable]
    public class UISkinStyleTextNode
    {
        public string binding;
        public UISkinRectTransform rect = new UISkinRectTransform();
        public int fontSize;
        public string color;
        public string alignment;
        public string reason;
    }

    [Serializable]
    public class UISkinPreserveNode
    {
        public string binding;
        public string policy;
        public string reason;
    }

    [Serializable]
    public class UISkinValidationSlot
    {
        public string id;
        public string check;
        public string target;
        public string expected;
    }

    [Serializable]
    public class UISkinPixelRect
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }

    [Serializable]
    public class UISkinRectTransform
    {
        public string anchor;
        public float x;
        public float y;
        public float width;
        public float height;
    }
}
