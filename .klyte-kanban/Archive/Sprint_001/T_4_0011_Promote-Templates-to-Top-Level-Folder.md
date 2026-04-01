**End time:** 2026-04-01 00:34 -0300
**Start time:** 2026-03-31 23:59 -0300
# [0011] Promote Templates/ to Top-Level Folder

**Developed by:** Agent-Claude-Opus-4.6 <agent@example.com>
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/04_OverallModStructure/02_ImprovementOpportunities.md — Improvement 1

## User Story

> Acting as **a mod developer navigating the Write Everywhere codebase**, I want **the Templates/ subsystem to live at the project root level (alongside Systems/, Controllers/, etc.) rather than nested inside Systems/**, so that I **it is immediately visible as a self-contained module and Systems/ becomes a focused folder for runtime ECS systems only**.

---

## Background

Systems/Templates/ contains 12 files (7 partial WETemplateManager files + 5 supporting systems). It behaves more like a self-contained module than a subfolder of Systems. Promoting it to Templates/ at the project root makes the high-level structure clearer and separates template-management concerns from rendering/culling/node concerns.

This is a folder move only. No logic changes are required.

---

## Definition of Ready (DoR)

- [ ] All files currently under Systems/Templates/ are inventoried
- [ ] All using / namespace declarations in the files are checked — confirm they do not encode the folder path
- [ ] The .csproj file is checked to confirm it uses glob includes (**/*.cs) rather than explicit file references
- [ ] No external mod (BelzontCommons or other) references the folder path directly

---

## Acceptance Criteria / Definition of Done (DoD)

- [x] All 12 files are moved from BelzontWE/Systems/Templates/ to BelzontWE/Templates/
- [x] The Systems/Templates/ folder is deleted (empty after move)
- [x] All namespace declarations in moved files are unchanged (no rename required)
- [x] The project compiles without errors
- [x] No using aliases referencing the old path exist in other files
- [x] Git history preserves file moves (use git mv instead of delete+create)

---

## Implementation Notes

1. Use git mv for each file to preserve history: git mv BelzontWE/Systems/Templates/WETemplateManager.cs BelzontWE/Templates/WETemplateManager.cs (repeat for all 12 files)
2. If the .csproj uses explicit <Compile Include="..."> entries, update them. If it uses <Compile Include="**\*.cs" />, no .csproj change is needed
3. Moved all 12 files from Systems/Templates/ to Templates/ using git mv. All namespaces remain BelzontWE (unchanged). .csproj uses SDK-style implicit globs so no project file changes needed. Build succeeds. Old Systems/Templates/ folder is now empty (git auto-removes empty dirs).

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Namespace/using path encoded in filenames or attributes | Low | Verify before moving |
| .csproj explicit file references break build | Low | Check glob vs explicit before moving |

---

## Related Tasks

### Depends on



### Is dependent for


