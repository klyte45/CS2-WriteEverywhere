**Start time:** 2026-04-01 01:44 -0300
# [0015] Centralize Magic Constants

**Developed by:** Agent-Claude-Opus-4.6 (agent@example.com)
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/04_OverallModStructure/02_ImprovementOpportunities.md — Improvement 5

## User Story

> Acting as **a mod developer modifying Write Everywhere behaviour (e.g. changing atlas size limits, frame intervals, or separator characters)**, I want **all magic constants in a single WEConstants.cs file**, so that I **I can find and change any configuration value without searching across many files**.

---

## Background

Several magic values are currently scattered across the codebase: VARIABLE_ITEM_SEPARATOR and VARIABLE_KV_SEPARATOR in WEPreCullingSystem; Template replacement separators in WETemplateManager; Frame interval masks in renderers; Font atlas size limits in FontAtlas; Job batch sizes in FontServer / rendering systems.

Centralizing these into a single WEConstants.cs makes the codebase self-documenting and reduces the risk of inconsistent changes.

---

## Definition of Ready (DoR)

- [ ] A grep across the codebase for numeric literals and special Unicode characters in non-comment, non-string positions is completed
- [ ] Each literal is categorized: constant candidate vs. intentionally inline
- [ ] Confirmed that none of these values are referenced from BelzontCommons or the game itself (only internal to BelzontWE)

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BelzontWE/WEConstants.cs (or BelzontWE/Utils/WEConstants.cs) is created with: VARIABLE_ITEM_SEPARATOR, VARIABLE_KV_SEPARATOR, REPLACEMENT_ITEM_SEPARATOR, REPLACEMENT_KV_SEPARATOR, REPLACEMENT_SUB_SEPARATOR, REPLACEMENT_SUB_KV_SEPARATOR, MAX_ATLAS_SIZE, MIN_ATLAS_SIZE, FONT_JOB_BATCH_SIZE, STRING_RENDERING_BATCH, RENDERER_FRAME_CHECK_MASK, DISPOSAL_FRAME_INTERVAL
- [ ] All original inline literals are replaced with references to the named constants
- [ ] No behavior change — values are identical before and after
- [ ] Project compiles without errors
- [ ] Constants are public static readonly or public const (prefer const for compile-time values, static readonly for runtime-computed values)

---

## Implementation Notes

1. Create WEConstants.cs with all identified magic constants
2. Replace all inline literals with references to WEConstants members

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| A literal that looks like a constant is actually computed/derived | Low | Review each candidate in context before extracting |
| Renaming a separator breaks serialized save data | Medium | Separator chars are used at runtime for string parsing, not stored in saves; verify before extraction |

---

## Related Tasks

### Depends on



### Is dependent for


