using System.Collections.Generic;

namespace Xipin.UIAITools
{
public static partial class UIAssetScanService
{
    class BatchItem
    {
        public string Path;
        public string Type;
        public string Source;
        public string Kind;
        public string Canvas;
        public string Texture;
        public string Material;
        public string BatchKey;
        public string ImageAsset;
        public string Atlas;
        public bool ImagePlus;
        public bool NullSprite;
    }

    class LooseTextureInfo
    {
        public int UseCount;
        public readonly HashSet<string> Prefabs = new HashSet<string>();
        public readonly HashSet<string> Nodes = new HashSet<string>();
    }

    class BreakInfo
    {
        public string Prefab;
        public string Owner;
        public int Index;
        public string PrevPath;
        public string Path;
        public string PrevType;
        public string Type;
        public string PrevKind;
        public string Kind;
        public string Reason;
        public string PrevTexture;
        public string Texture;
        public string Advice;
    }

    class TargetInfo
    {
        public string Prefab;
        public string Owner;
        public int Score;
        public string MainIssue;
        public string NextStep;
        public int ImageBatchGroups;
        public int EstimatedBatchGroups;
        public int BreakCount;
        public int CrossAtlasBreaks;
        public int LooseTextureBreaks;
        public int TextBreaks;
        public int MaterialBreaks;
        public int CanvasBreaks;
        public int AtlasCount;
        public int UITextureCount;
        public int LargeTextureCount;
    }

    class NullSpriteInfo
    {
        public string Prefab;
        public string Owner;
        public string Path;
        public string Node;
        public int Width;
        public int Height;
        public float Alpha;
        public bool RaycastTarget;
        public bool Maskable;
        public bool ImagePlus;
        public string Components;
        public string Parent;
        public string ParentComponents;
        public string Advice;
        public string Reason;
    }
}
}
