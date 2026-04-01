# [0016] Explicit System Dependency Documentation

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/04_OverallModStructure/02_ImprovementOpportunities.md — Improvement 6

## User Story

> Acting as **a new contributor or AI agent reading the Write Everywhere codebase**, I want **a centralized comment block in WriteEverywhereCS2Mod.DoOnCreateWorld() that documents the full system dependency graph**, so that I **I can understand system ordering without having to trace GetSystem<T>() calls and [UpdateAfter] attributes across all files**.

---

## Background

The system dependency graph for Write Everywhere is currently implicit — it can only be discovered by reading all GetSystem<T>() calls, [UpdateAfter/Before] attributes, and updateSystem.UpdateAt<T>() registrations. For a codebase with 19+ registered systems across 7 ECS phases, this is a significant reading burden.

Adding a structured comment block to the one place where all systems are registered (WriteEverywhereCS2Mod.DoOnCreateWorld()) provides a complete dependency overview at a glance.

This is a documentation-only change. Zero behavioral impact.

---

## Definition of Ready (DoR)

- [ ] WriteEverywhereCS2Mod.cs DoOnCreateWorld() is read in full
- [ ] All GetSystem<T>() dependency edges are traced across all system files
- [ ] The final dependency graph is confirmed to be consistent with the data flow diagram in RefsLibrary/20260330_CodeStructureAnalysis/01_ModSystems_vs_GameSystems/01_UpdatePhaseMapping.md

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] A comment block is added at the top of DoOnCreateWorld() (or immediately before the registration calls) that lists: Each ECS phase used by the mod; Under each phase: the systems registered in that phase, in execution order; For each system: its downstream data consumers (one-line annotation)
- [ ] The comment is accurate as of the time of writing (matches current system registrations)
- [ ] If tasks 0003 or 0004 are completed before this task, the comment reflects the updated phase assignments
- [ ] No code changes — documentation only
- [ ] Project compiles without errors (comments do not affect compilation)

---

## Implementation Notes

1. Add structured ASCII comment block at top of DoOnCreateWorld() documenting all systems, their phases, and their data outputs/consumers

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Comment becomes stale as systems change | Low | Note in the comment itself: 'update when adding/moving/removing systems' |
| ASCII box drawing characters cause encoding issues | Very low | Use UTF-8 file encoding (standard for C# files) |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0003]
- [0004]
- [0005]
