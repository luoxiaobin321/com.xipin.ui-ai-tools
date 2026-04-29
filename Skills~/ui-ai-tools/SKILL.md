---
name: ui-ai-tools
description: Unity Editor workflow skill for Xipin UI AI Tools, a UPM package named com.xipin.ui-ai-tools. Use when Codex needs to work with this package or its UI image triage, atlas auditing, reuse-image search, static DrawCall reports, UIAIToolsProfile or UIControlCatalog configuration, project adapter menus, or AI redesign draft, dry-run, execution-plan, and replacement-plan contracts. Use for reviewing package output CSV files, changing package code, or advising a Unity project that has installed this package.
---

# UI AI Tools

Use this skill for `com.xipin.ui-ai-tools`. Treat the package docs as the source of truth and load only the module needed for the task.

## Locate The Package

Prefer the current workspace package:

- `Packages/com.xipin.ui-ai-tools`

If the task runs from another Unity project, find the package with:

```powershell
rg --files -g package.json | rg "com\.xipin\.ui-ai-tools[\\/]package\.json"
```

If this skill is loaded from the package itself, the package root is two directories above this `SKILL.md`.

## Read By Task

For usage or integration questions, read `Documentation~/README.md` first, then only the needed module:

- Configuration, paths, roles: `Documentation~/modules/configuration.md`
- Scan reports, image triage, atlas and DrawCall auditing: `Documentation~/modules/scanning.md`
- Screenshot crop to existing-image search: `Documentation~/modules/reuse-search.md`
- AI redesign draft provider, saved draft JSON, dry-run, execution plan gates, external input package, Prompt files, and reference copy lists: `Documentation~/modules/ai-redesign.md`
- Host-side confirmed replacement executor contract: `Documentation~/modules/host-apply-executor.md`
- UI creation brief, layout draft, asset needs, and dry-run contract: `Documentation~/modules/ui-creation.md`
- Host-side UI prefab draft generator contract: `Documentation~/modules/ui-creation-host-generator.md`
- Host project menu, default profile, command entry: `Documentation~/modules/project-adapter.md`

For package code changes, read `Development~/README.md` first, then only the needed module:

- Package boundary and allowed dependencies: `Development~/modules/package-boundary.md`
- `UIAssetScanService` internals: `Development~/modules/scanning-internals.md`
- CSV filenames, fields, and meanings: `Development~/modules/report-contracts.md`
- AI request, draft, dry-run, and execution contracts: `Development~/modules/ai-contracts.md`
- UI creation contract boundaries: `Development~/modules/ui-creation-contracts.md`

Always read package `AGENTS.md` before editing package files.

## Working Rules

- Keep package code reusable. Do not add references to `GameApp`, `MotionFramework`, `com.xipin.lframework`, or other host project assemblies.
- Put project-specific paths in `UIAIToolsProfile`, control role names in `UIControlCatalog`, and host menu/command wrappers in the Unity project.
- Use package APIs from namespace `Xipin.UIAITools`.
- Do not move assets, overwrite prefabs, edit SpriteAtlas pack lists, or modify YooAsset settings unless the user explicitly asks for execution.
- For proposed migrations, output a plan first and include source path, target path, reason, risk, and verification.
- When moving Unity assets after explicit approval, use Unity `AssetDatabase.MoveAsset` or another GUID-preserving Unity path. Do not copy-delete images manually.

## Report Workflow

Prefer existing CSV outputs under the configured `UIAIToolsProfile.logRoot` before inventing new analysis:

- Start with `UIAssetTriageReport.csv`, `UIAssetTriagePlan.csv`, `UIReuseIndex.csv`, and `UIPrefabOptimizationTargets.csv`.
- Use prefab and batch details only when diagnosing a specific panel or DrawCall issue.
- Use `UIComponentCandidateIndex.csv`, `UIComponentCandidateIndexSummary.md`, and `UIComponentCandidateReview.csv` when preparing automatic UI creation component libraries; regenerated review CSVs preserve manual columns by `ComponentId`.
- Use `UIComponentCandidateIndexService.Validate` as the lightweight gate after generating the component candidate index.
- Use `UICreationBriefTemplateService.Generate` for automatic UI creation input templates; it writes JSON only and must not create prefabs.
- Use `UILayoutDraftTemplateService.Generate` after a creation brief exists; it writes a JSON draft template and still must not create prefabs.
- Use `UICreationLayoutDryRunService.Run` and `ValidateNoErrors` before any host-side UI prefab generator; the package still only reports.
- Use `UICreationHostGenerateChecklistService.Generate` and `ValidateNoBlockingSteps` to hand a passed layout dry-run to a host-side generator; referenced component candidates must still be `Approved` in the current `UIComponentCandidateReview.csv`, the current dry-run target and component list must match the checklist, and the checklist must explicitly say `Gate：Passed`.
- Keep the actual UI prefab draft generator in the host project and use `Documentation~/modules/ui-creation-host-generator.md` for its input and result report contract.
- Use `UIReuseSearchResults.csv` when matching a cropped image against existing project art.
- Use `UIReplacementPlanDryRun.csv`, `UIReplacementPlanDryRunSummary.md`, `UIReplacementExecutionPlan.csv`, `UIReplacementExecutionPlanSummary.md`, `UIReplacementHostApplyChecklist.md`, and `UIRedesignPackage_*.md` before any host-side replacement flow.
- Use `UIReplacementExternalInputPackage.json/md`, `UIReplacementExternalGenerationTasks.md`, `UIReplacementExternalPromptPack.md`, `UIReplacementExternalPromptItems.md`, `UIReplacementExternalPrompt_*.md`, and `UIReplacementExternalReferenceCopyList.md` when preparing external image generation; these reports include placement directory groups, prompt item directory indexes, acceptance checks, reference source directory groups, and rerun steps.
- Use `UIReplacementPendingInputReadiness.csv` and `UIReplacementPendingInputReadinessService.ValidateNoMissing` as the gate before host-side replacement execution; Missing and Invalid both block.
- Check warning/review distributions, missing previews/assets/atlases, target-atlas image counts, and blocking gate status before advising host-side execution.
- Keep any confirmed replacement executor in the host project; the package supplies the contract and gates, not resource mutation code.
- For automatic UI creation, keep package work at brief, layout draft, asset needs, dry-run, and confirmation checklist until a host-side generator is explicitly implemented.
- When a provider is involved, keep it limited to returning `UIRedesignDraft`; save the draft JSON, then run dry-run, gates, execution plan, host checklist, and manifest. For an existing AI-edited JSON, use the package flow that snapshots the draft JSON before the same gates and records both input and snapshot paths in the manifest.
- Treat `Advice` as a first pass. Confirm risky moves with prefab refs, text/config refs, owner mismatch, image size, atlas membership, and YooAsset address rules.

## Verification

Use the cheapest checks that prove the change. If generated `.csproj` files exist, a focused build is acceptable:

```powershell
dotnet build Xipin.UIAITools.Editor.csproj
dotnet build GameApp.Editor.csproj
```

When `.csproj` files are absent, prefer Unity batchmode and build arguments from the current project path instead of hardcoding a machine path:

```powershell
$project = (Get-Location).Path
$args = @('-quit','-batchmode','-nographics','-projectPath',$project,'-executeMethod','UIAssetTriageScanner.ValidateReports','-logFile','Logs/UIReportValidation.log')
Start-Process -FilePath $env:UNITY_EXE -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
```

For redesign workflow changes, also run the package preparation batch on a known prefab sample when available.

If `UNITY_EXE` is not configured or Unity is already open, report that clearly and fall back to static checks.
