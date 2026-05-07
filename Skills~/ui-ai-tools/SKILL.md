---
name: ui-ai-tools
description: Unity Editor workflow skill for Xipin UI AI Tools, a UPM package named com.xipin.ui-ai-tools. Use when Codex works on UI image triage, atlas auditing, reuse-image search, automatic UI creation contracts, skin prefab contracts, UIAIToolsProfile, UIControlCatalog, project adapter menus, or workbench integration.
---

# UI AI Tools

Use this skill for `com.xipin.ui-ai-tools`. Keep context small: read the package `AGENTS.md`, then load only the module needed for the current task.

## Locate

Prefer the current workspace package:

```text
Packages/com.xipin.ui-ai-tools
```

From another Unity project, find it with:

```powershell
rg --files -g package.json | rg "com\.xipin\.ui-ai-tools[\\/]package\.json"
```

## Read By Task

Usage or integration:

| Task | Module |
| --- | --- |
| paths, profile, roles | `Documentation~/modules/configuration.md` |
| scan reports and DrawCall audit | `Documentation~/modules/scanning.md` |
| cropped image reuse search | `Documentation~/modules/reuse-search.md` |
| automatic UI creation contract | `Documentation~/modules/ui-creation.md` |
| host UI prefab generator contract | `Documentation~/modules/ui-creation-host-generator.md` |
| host menu, batch, workbench adapter | `Documentation~/modules/project-adapter.md` |

Package changes:

| Task | Module |
| --- | --- |
| package boundary | `Development~/modules/package-boundary.md` |
| scan internals | `Development~/modules/scanning-internals.md` |
| report names, fields, Markdown contracts | `Development~/modules/report-contracts.md` |
| automatic UI creation boundary | `Development~/modules/ui-creation-contracts.md` |

## Rules

- Keep package code reusable. Do not reference `GameApp`, `MotionFramework`, `com.xipin.lframework`, YooAsset, or host assemblies.
- Put project paths in `UIAIToolsProfile`, roles in `UIControlCatalog`, and menus/batch wrappers/final prefab generation in the host project.
- Use package APIs from namespace `Xipin.UIAITools`.
- Do not move assets, overwrite prefabs, edit SpriteAtlas pack lists, or modify YooAsset settings unless the user explicitly asks for execution.
- Proposed migrations must list source path, target path, reason, risk, and verification; approved Unity asset moves should preserve GUIDs.

## Current Priority

The product still has four lines: automatic organization, reuse search, automatic UI creation, and new skinning. Current priority is the skinning line for “target concept + provided PNG crops”.

Skin work uses:

```text
Assets/UIAITools/Skinning/<UIName>
UIAIToolsReports/Skinning/<UIName>
```

For a new or non-UIVipcard target, start with `GenerateSkinHostAdapterChecklistBatch` and use `host-adapter-checklist.md` to define host crop ids, dynamic nodes, binding moves, and `_v2.prefab` generator mapping. The checklist should include a directly reusable `Re-run Checklist` command.
Existing `host-adapter-checklist.md` reports are revalidated for section order and traceability fields by the skin contract, so update generation, validation, and real reports together.

For provided-crops skinning, prefer:

```text
GenerateProvidedSkinAssetCropSpecBatch
ValidateProvidedSkinAssetCropsBatch
GenerateProvidedSkinReviewPackageBatch
```

`-uiInputImageFolder` must be an `Assets` folder. The crop spec entry may run before that folder exists so it can produce the art handoff spec first. The spec, validation, review-package, and debug step entries should all block a PNG file path or a folder outside `Assets`; validation, registration, prefab draft, and review-package entries should write the crop spec and a blocked crop check when the folder is missing, is a PNG file, or is outside `Assets`. The crop spec should include a directly reusable `Validate Command`, and the crop check report should include a directly reusable `Re-run Check` command. The detected-layout, asset-crops and skin-layout summaries should include `Manifest` plus their source JSON paths, and validation should reload the generated JSON bodies too. Runtime cleanup and post-cleanup reports should include `Manifest` plus directly reusable `Re-run Check`, `Re-run Checklist`, or `Re-run Readiness` commands. The binding report should include a directly reusable `Re-run Binding` command, and provided-crops binding/visual subreports should include a directly reusable `Review Package Command`. Existing provided-crops, visual, binding, and apply subreports are revalidated for section order, rerun commands, and source fields. Input-path failures should tell the user to fix `-uiInputImageFolder` first. `GenerateProvidedSkinReviewPackageBatch` should write a successful `review-package.md` with `Package Gate: Passed` and a directly reusable `Re-run` command, and should still write `Gate：Blocked` `review-package.md` for input, crop gate, or package gate failures, including prefab generation/cleanup gates, binding gates, and target/preview image gates.

Skin JSON under `Assets/UIAITools/Skinning` must be covered by `skin.json` or manifest.generated paths; new skin JSON files must register matching load and validation logic instead of remaining orphan work-package files.

Use `UseProvidedSkinAssetCropsBatch` and `GenerateProvidedSkinPrefabDraftBatch` only for debugging the registration and draft generation steps. `review-package.md` is the human review entry.

If real art is unavailable, do not stop. In the host project, use or add a synthetic smoke entry such as `GenerateSyntheticProvidedSkinReviewPackageBatch` to generate temporary concept/crops and exercise the same review-package chain.

## Reports

Prefer existing outputs under `UIAIToolsProfile.logRoot` before inventing analysis:

- scanning: `UIAssetTriageReport.csv`, `UIAssetTriagePlan.csv`, `UIReuseIndex.csv`, `UIPrefabOptimizationTargets.csv`
- reuse search: `UIReuseSearchResults.csv`, `UIReuseSearchSummary.md`
- automatic UI creation: `UIComponentCandidateIndex.csv`, `UIComponentCandidateReview.csv`, `UICreationLayoutDryRun.csv`, `UICreationHostGenerateChecklist.md`
- skinning: `skin.json`, `Generated/asset-crops.json`, `Generated/skin-layout.json`, `Prefabs/<SourcePrefabName>_v2.prefab`, `host-adapter-checklist.md`, `provided-crops-spec.md`, `provided-crops-check.md`, `binding-check.md`, `visual-check.md`, `review-package.md`, `apply-checklist.md`, `auto-build-notes.md`
- host maintenance: `UIVipcardSpriteBackfillReport.md`, `UIVipcardTitleTextBindingCheck.md`

For automatic UI creation reports, keep the Markdown resumable: component candidate summaries should point to the candidate/review CSVs and source batch sequence CSV, layout dry-run summaries should point to the layout draft JSON and dry-run CSV, and host generate checklists should point back to the layout draft JSON plus the dry-run/checklist rerun entries.

For host prefab generation reports, `UICreationHostGenerateResult.md` should include `Layout Draft JSON`, target prefab, result CSV, host checklist, layout dry-run CSV, component candidate review CSV, and directly reusable `Re-run Generate` / `Re-run Validate` commands.

For scan and reuse-search reports, keep the Markdown resumable too: `UIAIToolsSummary.md` and `UIAIToolsPanelFocus.md` should include source CSVs plus validate/rerun entries, and `UIReuseSearchSummary.md` should include the query image, results CSV, and directly reusable `Re-run Search` command.

The host master plan validates existing non-skin Markdown report section order for scan summaries, panel focus, reuse-search summary, component candidate summary, layout dry-run summary, host generate checklist/result, global runtime checklist, and UIVipcard host maintenance reports.

For global runtime update checklists, keep `checklist.md` resumable with the batch name, work package, `replace-map.csv`, `Images` folder, and directly reusable `Re-run Template` / `Re-run Validate` commands.

The host master plan revalidates existing real CSV headers and readability for scanning core CSVs, `UIReuseSearchResults.csv`, component candidate CSVs, layout dry-run, host generate result, skin `generate-result.csv`, and global runtime `replace-map.csv`; new real CSVs must be registered in that validation, and CSV field changes must update generation, validation, and real artifacts together.

The host master plan also reloads existing Creation brief/layout template JSON under `UIAIToolsReports/Creation`; new Creation JSON files must register matching load and validation logic.

For host-specific maintenance reports, avoid sidecar text islands. `UIVipcardSpriteBackfillReport.md` should include the target prefab, related prefabs, report path, directly reusable `Re-run Report` / `Re-run Validate` commands, empty Sprite list, and a clear note that reporting/validation do not modify prefab assets.

`UIVipcardTitleTextBindingCheck.md` should include the source prefab, report path, directly reusable `Re-run Check` command, title text nodes, parent nodes, layout values, background image state, and per-row status so host prefab structure changes cannot silently break title text binding.

## Verification

Use the cheapest check that proves the change. For skin work, start with:

```text
UIAssetTriageScanner.ValidateSkinContractBatch
```

For screenshots, remove `-nographics`. If `.csproj` files exist, focused `dotnet build Xipin.UIAITools.Editor.csproj` or `dotnet build GameApp.Editor.csproj` is acceptable.
