# [0007] FontServer Job Scheduling — Execution

**Developed by:** 
**Cancellation time:** 2026-03-31 23:28 -0300

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md — ID 3

## User Story

> Acting as **a player running Write Everywhere in a busy city with many new text strings loading per frame**, I want **FontServer's StringRenderingJob to be scheduled in an early phase (e.g. PreSimulation) and results collected in Rendering**, so that I **font mesh generation overlaps with game simulation and reduces frame-time budget consumed in the Rendering phase**.

---

## Background

Based on the findings of task 0006, FontServer.RunJobs() schedules a StringRenderingJob (IJobParallelForBatch) and immediately blocks on Dependency.Complete(). Splitting this into: Kick phase (early, e.g. PreSimulation): schedule StringRenderingJob, store JobHandle; Collect phase (Rendering): call Complete(), process results — would allow the Unity job scheduler to run font mesh generation on worker threads while the main thread advances through simulation phases.

The specific implementation plan is defined by the output of task 0006.

BLOCKED on task [0006] (FontServerJobSchedulingAnalysis). This task must not be started until task 0006 is complete and a Go decision has been recorded in that file. If task 0006 concludes a No-Go, this task shall be marked Canceled (Z).

---

## Definition of Ready (DoR)

- [ ] Task 0006 (FontServerJobSchedulingAnalysis) is complete with a Go decision recorded
- [ ] The implementation plan from task 0006 is read and understood
- [ ] A local CS2 modded installation is available for testing
- [ ] A test scenario exists with 3+ fonts loaded and 50+ new strings per frame (to stress the multi-font path)

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] StringRenderingJob is scheduled in the phase identified by task 0006 analysis (not in Rendering)
- [ ] JobHandle.Complete() (or equivalent multi-handle completion) is called in the Rendering phase, after all fonts have scheduled their jobs
- [ ] PrimitiveRenderInformation construction still occurs on the main thread after completion
- [ ] In steady state (all strings cached, no new jobs scheduled), there is no regression in frame time
- [ ] With 3 fonts and 50 new strings each, the Rendering phase main-thread time is measurably reduced (profiler verification)
- [ ] Texture2D.Apply() still runs on the main thread (unchanged — this is not part of the split)
- [ ] Atlas state mutations (glyph rasterization) are handled per the plan from task 0006
- [ ] The mod compiles and loads without errors in CS2 v1.5.6
- [ ] No visual regressions in rendered text

---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Early job scheduling reads data not yet valid in PreSimulation | Unknown | Resolved by task 0006 analysis |
| Main-thread atlas mutation cannot be moved earlier | Medium | Resolved by task 0006 Q3 |
| In practice the gain is <1ms in steady state | Medium | Acceptable; improvement documented. If gain is negligible in all real scenarios, consider cancelling after profiling |

---

## Related Tasks

### Depends on

- [0006]

### Is dependent for



### Is child of

- [0006]
