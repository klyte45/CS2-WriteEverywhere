# [0004] Move WETemplateDisposalSystem to Cleanup Phase

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md — ID 2

## User Story

> Acting as **a mod developer maintaining the Write Everywhere codebase**, I want **WETemplateDisposalSystem registered in the Cleanup phase instead of the Rendering phase**, so that I **structural ECS entity deletions happen at the idiomatic end-of-frame barrier and cannot invalidate entity queries mid-frame in other Rendering-phase systems**.

---

## Background

WETemplateDisposalSystem runs every 256 frames to destroy orphaned WE entities and components. Entity disposal is a structural ECS operation. The game's own cleanup/disposal systems run in the Cleanup phase (end of frame). Placing this system in the Rendering phase means structural changes (entity destruction via EntityCommandBuffer) may execute at the Rendering phase barrier — potentially invalidating entity queries in other Rendering-phase systems.

Moving to Cleanup is more idiomatic and aligns with the game's own pattern, while ensuring the Rendering → PreCulling data flow is not interrupted by structural mutations.

---

## Definition of Ready (DoR)

- [ ] WriteEverywhereCS2Mod.DoOnCreateWorld() registration call for WETemplateDisposalSystem is located
- [ ] WETemplateDisposalSystem.cs is read and the EntityCommandBuffer usage is confirmed (deferred execution via ECB, not immediate EntityManager.DestroyEntity)
- [ ] Confirmed that Cleanup phase is available and runs after Rendering and PreCulling in CS2 v1.5.6 (so the next frame's Modification1 will see the updated entity set)
- [ ] No other system has [UpdateAfter(WETemplateDisposalSystem)] in the Rendering phase that would break if disposal moves to Cleanup

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WETemplateDisposalSystem registration call in DoOnCreateWorld() uses SystemUpdatePhase.Cleanup instead of SystemUpdatePhase.Rendering
- [ ] Any [UpdateAfter] / [UpdateBefore] attributes that reference Rendering-phase systems are removed or updated to be valid in Cleanup
- [ ] Orphaned entities are still correctly destroyed (disposal still runs every 256 frames)
- [ ] No entities that should be alive are destroyed, and no destroyed entities persist as ghosts
- [ ] The mod compiles and loads without errors in CS2 v1.5.6
- [ ] Manual test: create several WE text objects, delete their parent, wait >256 frames, confirm orphaned WE components are gone

---

## Implementation Notes

1. In WriteEverywhereCS2Mod.cs, find the updateSystem.UpdateAt<WETemplateDisposalSystem>(SystemUpdatePhase.Rendering) call
2. Change the phase argument to SystemUpdatePhase.Cleanup
3. In WETemplateDisposalSystem.cs, verify the 256-frame interval check uses a stable frame counter that is not affected by phase changes
4. Remove any Rendering-phase [UpdateAfter] / [UpdateBefore] attributes on this system
5. Build and test

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Orphan detection logic depends on data only available in Rendering phase | Low | Disposal checks entity existence / component presence, not frame-timing data; these are valid in any phase |
| ECB playback timing shifts by one half-frame | Very low | ECB scheduled in Cleanup plays back at the Cleanup barrier, which still precedes the next frame's Modification1 — correctness is preserved |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0003]
- [0005]
