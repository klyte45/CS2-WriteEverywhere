# [0008] Deferred Font Job Completion for Multi-Font Setups

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/03_FontProcessing/02_ImprovementAnalysis.md — Area 2

## User Story

> Acting as **a player or server operator running Write Everywhere with multiple custom fonts loaded simultaneously**, I want **the font rendering jobs for all fonts to be scheduled at once and completed together in a single barrier**, so that I **the main thread is not stalled N times sequentially — once per loaded font**.

---

## Background

FontServer.OnUpdate() iterates over all loaded FontSystem instances and calls RunJobs() for each. RunJobs() internally: 1) Schedules StringRenderingJob, 2) Immediately calls Dependency.Complete() — blocks the main thread, 3) Processes results.

With N fonts loaded, the main thread stalls N times, each stall waiting for one font's jobs to finish before the next font even begins scheduling its jobs. This prevents any parallelism between fonts.

The improvement is: 1) First loop: call a "schedule-only" variant of RunJobs() for all fonts, collecting all JobHandles, 2) Combine all handles: JobHandle.CombineDependencies(...), 3) Call Complete() once on the combined handle, 4) Second loop: process results for all fonts.

This allows worker threads to process all fonts' StringRenderingJobs in parallel while the main thread finishes its scheduling pass.

---

## Definition of Ready (DoR)

- [ ] FontServer.cs OnUpdate() and FontSystem.RunJobs() are read and the exact blocking pattern is confirmed
- [ ] The result-processing code (post-Complete() work per font) is identified and confirmed to be main-thread-only
- [ ] Confirmed that StringRenderingJob instances for different fonts do not share write targets (no data hazard between fonts running in parallel)
- [ ] A local test environment with 3+ fonts loaded is available (or can be constructed with test XML)

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] FontServer.OnUpdate() (or FontSystem) is refactored to two passes: schedule-all, then complete-all
- [ ] All font JobHandles are collected and combined before any Complete() call
- [ ] Post-completion result processing runs after the single combined Complete() barrier
- [ ] With a single font loaded (common case), behaviour is functionally identical to before (one schedule + one complete)
- [ ] With 3 fonts loaded, the total main-thread stall time is reduced (profiler: one combined stall instead of three sequential stalls)
- [ ] No visual regression in any loaded font's rendered text
- [ ] The mod compiles and loads without errors in CS2 v1.5.6

---

## Implementation Notes

1. In FontSystem.cs, add a new method ScheduleJobs() → JobHandle that does the scheduling work only (no Complete() call). The existing RunJobs() can delegate to ScheduleJobs() + Complete() for backward compat, or be replaced
2. In FontServer.OnUpdate(): Pass 1: schedule all (collect JobHandles into NativeArray); Single barrier: JobHandle.CombineDependencies(handles).Complete(); Pass 2: collect results for all fonts
3. Ensure atlas-state mutations (glyph rasterization, AddRect) still happen on the main thread before the schedule pass (unchanged)

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| StringRenderingJob for different fonts shares atlas pixel buffer (write hazard) | Low | Each font has its own FontAtlas; jobs write to different textures |
| NativeArray<JobHandle> allocation overhead | Very low | Temp allocator; freed immediately after combine |
| Single-font benefit is zero | Low | Acceptable; the pattern is still cleaner and benefits multi-font setups |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0006]
- [0007]
