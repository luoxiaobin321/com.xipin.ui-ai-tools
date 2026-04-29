using System.Collections.Generic;
using System.IO;

namespace Xipin.UIAITools
{
    public static class UIReportFiles
    {
        public const string AssetTriageReport = "UIAssetTriageReport.csv";
        public const string AssetTriagePlan = "UIAssetTriagePlan.csv";
        public const string PrefabAtlasStats = "UIPrefabAtlasStats.csv";
        public const string ACommonUsage = "UIACommonUsage.csv";
        public const string TextureSizeReport = "UITextureSizeReport.csv";
        public const string DuplicateImageReport = "UIDuplicateImageReport.csv";
        public const string ReuseIndex = "UIReuseIndex.csv";
        public const string PrefabImageDetails = "UIPrefabImageDetails.csv";
        public const string PrefabAtlasBreakdown = "UIPrefabAtlasBreakdown.csv";
        public const string PrefabDrawCallRisk = "UIPrefabDrawCallRisk.csv";
        public const string PrefabBatchSequence = "UIPrefabBatchSequence.csv";
        public const string PrefabBatchBreaks = "UIPrefabBatchBreaks.csv";
        public const string PrefabBatchBreakSummary = "UIPrefabBatchBreakSummary.csv";
        public const string PrefabOptimizationTargets = "UIPrefabOptimizationTargets.csv";
        public const string PrefabTextureSwitchPairs = "UIPrefabTextureSwitchPairs.csv";
        public const string PrefabWhiteTextureBreaks = "UIPrefabWhiteTextureBreaks.csv";
        public const string PrefabNullSpriteImages = "UIPrefabNullSpriteImages.csv";
        public const string LooseTextureCandidates = "UILooseTextureCandidates.csv";
        public const string ReuseSearchResults = "UIReuseSearchResults.csv";
        public const string Summary = "UIAIToolsSummary.md";
        public const string PanelFocus = "UIAIToolsPanelFocus.md";
        public const string ReplacementPlanDryRun = "UIReplacementPlanDryRun.csv";
        public const string ReplacementPlanDryRunSummary = "UIReplacementPlanDryRunSummary.md";
        public const string ReplacementExecutionPlan = "UIReplacementExecutionPlan.csv";
        public const string ReplacementExecutionPlanSummary = "UIReplacementExecutionPlanSummary.md";
        public const string ReplacementPendingInputs = "UIReplacementPendingInputs.csv";
        public const string ReplacementPendingInputsSummary = "UIReplacementPendingInputsSummary.md";
        public const string ReplacementPendingInputReadiness = "UIReplacementPendingInputReadiness.csv";
        public const string ReplacementPendingInputReadinessSummary = "UIReplacementPendingInputReadinessSummary.md";
        public const string ReplacementExternalInputPackage = "UIReplacementExternalInputPackage.json";
        public const string ReplacementExternalInputPackageSummary = "UIReplacementExternalInputPackage.md";
        public const string ReplacementExternalGenerationTasks = "UIReplacementExternalGenerationTasks.md";
        public const string ReplacementExternalPromptPack = "UIReplacementExternalPromptPack.md";
        public const string ReplacementExternalPromptItemList = "UIReplacementExternalPromptItems.md";
        public const string ReplacementExternalReferenceCopyList = "UIReplacementExternalReferenceCopyList.md";
        public const string ReplacementHostApplyChecklist = "UIReplacementHostApplyChecklist.md";
        public const string ComponentCandidateIndex = "UIComponentCandidateIndex.csv";
        public const string ComponentCandidateIndexSummary = "UIComponentCandidateIndexSummary.md";
        public const string ComponentCandidateReview = "UIComponentCandidateReview.csv";
        public const string CreationLayoutDryRun = "UICreationLayoutDryRun.csv";
        public const string CreationLayoutDryRunSummary = "UICreationLayoutDryRunSummary.md";
        public const string CreationHostGenerateChecklist = "UICreationHostGenerateChecklist.md";

        public const string AssetTriageReportHeader = "Path,Name,Guid,Width,Height,Bytes,Memory,Importer,Atlas,PrefabRefs,TextRefs,NameDup,Hash,Advice,Reason";
        public const string AssetTriagePlanHeader = "Action,Risk,Source,Target,Feature,Width,Height,PrefabCount,Advice,Note,PrefabRefs";
        public const string PrefabAtlasStatsHeader = "Prefab,Owner,AtlasCount,UITextureCount,LargeTextureCount,ImageCount,Atlases,UITextures";
        public const string ACommonUsageHeader = "Path,Name,Guid,Width,Height,Atlas,PrefabCount,OwnerCount,Owners,PrefabRefs,TextRefs,Hash,Advice";
        public const string TextureSizeReportHeader = "Path,Name,Guid,Width,Height,Area,Bytes,Memory,PrefabCount,TextRefs,Hash,SizeClass,Advice";
        public const string DuplicateImageReportHeader = "Hash,Count,Width,Height,Bytes,Paths,Guids,Atlases,PrefabRefs,TextRefs,Advice";
        public const string ReuseIndexHeader = "Path,Name,Guid,Width,Height,SizeClass,Kind,Atlas,AssetOwner,PrefabCount,OwnerCount,Owners,PrefabRefs,TextRefs,Hash,SameHashCount,SameHashPaths,Advice,Reason";
        public const string PrefabImageDetailsHeader = "Prefab,PrefabOwner,Image,ImageOwner,Kind,Name,Guid,Width,Height,SizeClass,Atlas,AtlasOwner,TextRefs,Hash,Match";
        public const string PrefabAtlasBreakdownHeader = "Prefab,PrefabOwner,Atlas,AtlasOwner,ImageCount,Match,Images";
        public const string PrefabDrawCallRiskHeader = "Prefab,Owner,GraphicCount,ImageCount,RawImageCount,OtherGraphicCount,ImagePlusCount,NullSpriteImageCount,AtlasImageCount,LooseTextureImageCount,UniqueAtlasCount,UniqueLooseTextureCount,UniqueTextureCount,UniqueMaterialCount,EstimatedBatchGroups,ImageBatchGroups,TextureSwitches,ImageTextureSwitches,MaterialSwitches,NestedCanvasCount,MaskCount,RectMask2DCount,TopTextures,TopImageTextures";
        public const string PrefabBatchSequenceHeader = "Prefab,Owner,Index,Path,Type,Source,Kind,Canvas,Texture,Material,BatchKey,ImageAsset,Atlas";
        public const string PrefabBatchBreaksHeader = "Prefab,Owner,Index,PrevPath,Path,PrevType,Type,PrevKind,Kind,Reason,PrevTexture,Texture,PrevMaterial,Material,PrevCanvas,Canvas,Advice";
        public const string PrefabBatchBreakSummaryHeader = "Prefab,Owner,BreakCount,TextureBreaks,TextBreaks,MaterialBreaks,CanvasBreaks,CrossAtlasBreaks,LooseTextureBreaks,TopAdvice,TopTextures,TopTexturePairs,Samples";
        public const string PrefabOptimizationTargetsHeader = "Prefab,Owner,PriorityScore,MainIssue,NextStep,ImageBatchGroups,EstimatedBatchGroups,BreakCount,CrossAtlasBreaks,LooseTextureBreaks,TextBreaks,MaterialBreaks,CanvasBreaks,AtlasCount,UITextureCount,LargeTextureCount";
        public const string PrefabTextureSwitchPairsHeader = "Prefab,Owner,Advice,Reason,PrevTexture,Texture,Count,FirstIndex,Samples";
        public const string PrefabWhiteTextureBreaksHeader = "Prefab,Owner,WhiteSide,WhiteKind,OtherKind,Reason,Advice,Count,FirstIndex,Samples";
        public const string PrefabNullSpriteImagesHeader = "Prefab,Owner,Path,Node,Width,Height,Alpha,RaycastTarget,Maskable,ImagePlus,Components,Parent,ParentComponents,Advice,Reason";
        public const string LooseTextureCandidatesHeader = "Path,Name,Guid,Width,Height,SizeClass,UseCount,PrefabCount,Prefabs,Nodes,TextRefs,OwnerCount,Owners,Advice,Reason,Target";
        public const string ReuseSearchResultsHeader = "Query,Path,Name,Guid,Width,Height,Score,SizeClass,Kind,Atlas,AssetOwner,PrefabCount,OwnerCount,Owners,PrefabRefs,TextRefs,Advice,Reason";
        public const string ReplacementPlanDryRunHeader = "ItemIndex,Check,Severity,Status,OldAsset,NewAsset,TargetAtlas,Message,Evidence";
        public const string ReplacementExecutionPlanHeader = "ItemIndex,Action,Status,OldAsset,NewAsset,TargetAtlas,PrefabRefs,RequiresManualConfirmation,Note,Reason";
        public const string ReplacementPendingInputsHeader = "InputKind,Status,Path,ReferencePath,TargetAtlas,ItemIndices,Count,SourceAction,Note";
        public const string ReplacementPendingInputReadinessHeader = "InputKind,PendingStatus,Readiness,Path,ActualWidth,ActualHeight,ReferencePath,TargetAtlas,ItemIndices,Count,SourceAction,Note";
        public const string ComponentCandidateIndexHeader = "ComponentId,Role,Source,Type,Kind,ImageAsset,Atlas,UseCount,PrefabCount,SamplePrefabs,SampleNodes,Notes";
        public const string ComponentCandidateReviewHeader = "ComponentId,Role,ReviewTier,SuggestedDecision,UseCount,PrefabCount,ImageAsset,Atlas,SamplePrefabs,SampleNodes,ComponentPrefabPath,PreviewPath,States,UsageNotes,Reviewer,ReviewNotes";
        public const string CreationLayoutDryRunHeader = "ItemIndex,Check,Severity,Status,Message,Evidence";

        public static readonly string[] CoreReports =
        {
            AssetTriageReport,
            AssetTriagePlan,
            PrefabAtlasStats,
            ACommonUsage,
            TextureSizeReport,
            DuplicateImageReport,
            ReuseIndex,
            PrefabImageDetails,
            PrefabAtlasBreakdown,
            PrefabDrawCallRisk,
            PrefabBatchSequence,
            PrefabBatchBreaks,
            PrefabBatchBreakSummary,
            PrefabOptimizationTargets,
            PrefabTextureSwitchPairs,
            PrefabWhiteTextureBreaks,
            PrefabNullSpriteImages,
            LooseTextureCandidates
        };

        public static readonly Dictionary<string, string> CoreReportHeaders = new Dictionary<string, string>
        {
            { AssetTriageReport, AssetTriageReportHeader },
            { AssetTriagePlan, AssetTriagePlanHeader },
            { PrefabAtlasStats, PrefabAtlasStatsHeader },
            { ACommonUsage, ACommonUsageHeader },
            { TextureSizeReport, TextureSizeReportHeader },
            { DuplicateImageReport, DuplicateImageReportHeader },
            { ReuseIndex, ReuseIndexHeader },
            { PrefabImageDetails, PrefabImageDetailsHeader },
            { PrefabAtlasBreakdown, PrefabAtlasBreakdownHeader },
            { PrefabDrawCallRisk, PrefabDrawCallRiskHeader },
            { PrefabBatchSequence, PrefabBatchSequenceHeader },
            { PrefabBatchBreaks, PrefabBatchBreaksHeader },
            { PrefabBatchBreakSummary, PrefabBatchBreakSummaryHeader },
            { PrefabOptimizationTargets, PrefabOptimizationTargetsHeader },
            { PrefabTextureSwitchPairs, PrefabTextureSwitchPairsHeader },
            { PrefabWhiteTextureBreaks, PrefabWhiteTextureBreaksHeader },
            { PrefabNullSpriteImages, PrefabNullSpriteImagesHeader },
            { LooseTextureCandidates, LooseTextureCandidatesHeader }
        };

        public static string GetPath(string logRoot, string fileName)
        {
            return Path.Combine(logRoot.TrimEnd('/', '\\'), fileName).Replace('\\', '/');
        }
    }
}
