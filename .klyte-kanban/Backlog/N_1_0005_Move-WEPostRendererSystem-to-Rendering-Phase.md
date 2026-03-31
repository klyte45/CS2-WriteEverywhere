# [0005] Move WEPostRendererSystem to Rendering Phase

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md — ID 4

## User Story

> Acting as **a player running Write Everywhere in a large city**, I want **WEPostRendererSystem to execute in the Rendering phase (after template systems) rather than in MainLoop of the next frame**, so that I **mesh cache updates have the minimum possible latency before being consumed by WERendererSystem**.

---

## Background

WEPostRendererSystem is currently registered with AllowedPhase.EndFrame → SystemUpdatePhase.MainLoop. This causes it to run at the beginning of the next ECS frame (MainLoop). Its output (IBasicRenderInformation mesh cache for WEWaitingRendering entities) is consumed by WERendererSystem via the Unity beginContextRendering callback, which fires after all ECS phases complete.

This introduces a full-frame latency. Moving WEPostRendererSystem to Rendering (after template update systems) reduces mesh cache latency from a full frame to within the same frame.

---

## Definition of Ready (DoR)

- [ ] WEPostRendererSystem.cs is read and the AllowedPhase.EndFrame / MainLoop registration pattern is confirmed
- [ ] Confirmed that WEPostRendererSystem has no inputs that are produced later than the Rendering phase (i.e., it does not read PreCulling-phase data)
- [ ] The WETemplateUpdateSystem registration is confirmed as a valid anchor to place [UpdateAfter] on in the Rendering phase
- [ ] Confirmed that WERendererSystem (Unity callback) fires after all ECS phases including Rendering, so the mesh cache built in Rendering is available during the callback

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WEPostRendererSystem registration no longer uses AllowedPhase.EndFrame; it is registered with SystemUpdatePhase.Rendering
- [ ] A [UpdateAfter(WETemplateUpdateSystem)] attribute is added (or kept) to ensure ordering within the Rendering phase
- [ ] WERendererSystem (Unity callback) successfully reads the mesh cache built in the same frame's Rendering phase
- [ ] No one-frame rendering lag visible in fast-moving text (dynamic counters, vehicle destination signs)
- [ ] The mod compiles and loads without errors in CS2 v1.5.6
- [ ] Manual test: observe that dynamically updating text reflects new values within the same frame they change (no 1-frame delay)

---

## Implementation Notes

1. In WriteEverywhereCS2Mod.cs, locate the WEPostRendererSystem registration. It likely uses an EndFrame/MainLoop variant
2. Replace with updateSystem.UpdateAt<WEPostRendererSystem>(SystemUpdatePhase.Rendering)
3. In WEPostRendererSystem.cs, add [UpdateAfter(typeof(WETemplateUpdateSystem))] (or the equivalent BelzontCommons registration-time ordering mechanism) to ensure it runs after template data is updated
4. Verify that no [UpdateBefore] constraint from another system conflicts with the new position
5. Build and test

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| WEPostRendererSystem reads PreCulling data that isn't available yet in Rendering | Low | Review system inputs — if it only reads WEWaitingRendering component data from template systems, no PreCulling dependency exists |
| Race condition between mesh cache write (Rendering) and DrawMesh read (Unity callback) | Very low | ECS guarantees all phase systems complete before Unity callbacks fire; no concurrent access |
| AllowedPhase.EndFrame was intentional (e.g. to defer heavy work) | Low | If heavy work is the concern, document why; but latency saving is the primary goal |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0003]
- [0004]
