**Start time:** 2026-04-01 01:41 -0300
# [0014] Sub-organize Controllers/ Folder

**Developed by:** Agent-Claude-Opus-4.6 (agent@example.com)
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/04_OverallModStructure/02_ImprovementOpportunities.md — Improvement 4

## User Story

> Acting as **a mod developer navigating the Write Everywhere codebase**, I want **the Controllers/ folder organized into sub-folders by responsibility (Base, Data, Library)**, so that I **the 15 controller files are grouped by their change frequency and purpose rather than listed flat**.

---

## Background

Controllers/ currently contains 15 files with four distinct responsibilities: Base (1–2 files): WEBindableSystemBase, WETextDataBaseController; Data (4 files): WETextDataMainController, WETextDataMaterialController, WETextDataMeshController, WETextDataTransformController; Library (3 files): WEFontManagementController, WECustomMeshLibraryController, WETextureAtlasController; Other (6 files): WEWorldPickerController, WELayoutController, WEFormulaeController, WEModulesSystem, FileController, DebugController.

At 15 files, the folder is navigable but will become difficult as the project grows. Sub-organizing by responsibility reduces cognitive load when looking for a specific controller.

This is a folder reorganization only. No logic changes are required.

---

## Definition of Ready (DoR)

- [ ] All 15 files in Controllers/ are inventoried and their responsibilities are confirmed
- [ ] Namespace declarations in all 15 files are checked — confirm the namespace does not encode the folder path
- [ ] The .csproj glob pattern is confirmed so no explicit file entries need updating
- [ ] No external code (BelzontCommons or other mods) imports types by path-dependent namespace

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Controllers/Base/ contains WEBindableSystemBase.cs and WETextDataBaseController.cs
- [ ] Controllers/Data/ contains WETextDataMainController.cs, WETextDataMaterialController.cs, WETextDataMeshController.cs, WETextDataTransformController.cs
- [ ] Controllers/Library/ contains WEFontManagementController.cs, WECustomMeshLibraryController.cs, WETextureAtlasController.cs
- [ ] Remaining 6 files remain at Controllers/ root level
- [ ] No namespace declarations are changed
- [ ] Project compiles without errors
- [ ] Git history preserves file moves (use git mv)

---

## Implementation Notes

1. Use git mv to preserve history for all 9 files being moved into subfolders

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Namespace encodes path | Low | Check all 15 files before moving |
| Folder reorganization makes git blame harder | Very low | git mv preserves history; git log --follow works |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0011]
