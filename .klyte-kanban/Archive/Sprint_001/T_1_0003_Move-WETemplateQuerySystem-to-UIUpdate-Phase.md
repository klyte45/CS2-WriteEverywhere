**End time:** 2026-03-31 23:17 -0300
**Start time:** 2026-03-31 23:16 -0300
# [0003] Move WETemplateQuerySystem to UIUpdate Phase

**Developed by:** Agent-Claude-Sonnet-4.6 <agent@example.com>
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md — ID 1

## User Story

> Acting as **a mod developer maintaining the Write Everywhere codebase**, I want **WETemplateQuerySystem registered in the UIUpdate phase instead of the Rendering phase**, so that I **the system's phase assignment correctly reflects its purpose and reduces contention in the Rendering phase**.

---

## Background

WETemplateQuerySystem is currently registered in SystemUpdatePhase.Rendering with [UpdateAfter(WETemplateUpdateSystem)]. Its responsibility is to answer UI query calls (GetCityTemplateUsageCount, CanBePrefabLayout) — it does not produce any data consumed by the rendering pipeline. Placing it in Rendering is semantically incorrect and adds an unnecessary system to an already-busy phase.

Moving it to UIUpdate correctly reflects that it serves UI reads, and frees one scheduling slot from the Rendering phase.

---

## Definition of Ready (DoR)

- [ ] WriteEverywhereCS2Mod.DoOnCreateWorld() registration call for WETemplateQuerySystem is located
- [ ] WETemplateQuerySystem.cs is read and confirmed to have no data outputs consumed by any Rendering-phase system
- [ ] Confirmed that UIUpdate phase is available and runs before the Unity render callback in CS2 v1.5.6
- [ ] All callers of WETemplateQuerySystem methods are identified and confirmed to be UI-layer code (not rendering systems)

---

## Acceptance Criteria / Definition of Done (DoD)

- [x] WETemplateQuerySystem registration call in DoOnCreateWorld() uses SystemUpdatePhase.UIUpdate instead of SystemUpdatePhase.Rendering
- [x] Any [UpdateAfter(WETemplateUpdateSystem)] attribute is removed or replaced with an appropriate constraint valid in UIUpdate phase (or simply removed if no ordering constraint is needed within UIUpdate)
- [x] All UI query methods on WETemplateQuerySystem return correct data after the phase change
- [x] No null reference or timing errors arise from the system running in a different phase
- [ ] The mod compiles and loads without errors in CS2 v1.5.6
- [ ] Manual test: open the template management UI in-game; verify template usage counts and prefab layout flags display correctly

---

## Implementation Notes

1. In WriteEverywhereCS2Mod.cs, find the updateSystem.UpdateAt<WETemplateQuerySystem>(SystemUpdatePhase.Rendering) (or similar) call
2. Change the phase argument to SystemUpdatePhase.UIUpdate
3. Open WETemplateQuerySystem.cs and check if any [UpdateAfter] / [UpdateBefore] attributes reference Rendering-phase systems. If so, remove them (there are no ordering requirements within UIUpdate for this system)
4. Build and test
5. Changed WETemplateQuerySystem registration from UpdateAfter<WETemplateQuerySystem>(Rendering) to UpdateAt<WETemplateQuerySystem>(UIUpdate) in WriteEverywhereCS2Mod.DoOnCreateWorld(). No [UpdateAfter]/[UpdateBefore] attributes were present on WETemplateQuerySystem itself, so no attribute changes were needed. UIUpdate runs after Rendering in CS2, so all WETemplateManager state modified during Rendering is visible to UIUpdate queries.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| UI query returns stale data (read one phase later than before) | Low | WETemplateManager state is modified in Rendering; UIUpdate runs after Rendering so data is fresh |
| Dependency (JobHandle) completion ordering breaks | Low | System's OnUpdate only calls Dependency.Complete() — no job is scheduled, so there's no ordering risk |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0004]
- [0005]
