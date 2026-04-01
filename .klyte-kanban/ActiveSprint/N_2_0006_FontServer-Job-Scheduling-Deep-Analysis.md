# [0006] FontServer Job Scheduling — Deep Analysis

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md — ID 3

Analysis questions: Q1: What data does FontServer mutate before scheduling the job? Q2: What data does FontServer output, and when is it consumed? Q3: Is the atlas-state mutation (glyph rasterization) separable from job scheduling? Q4: What is the actual frame-time cost of FontServer in steady state vs. initial load? Q5: Are there any thread-safety constraints that prevent early scheduling?

## User Story

> Acting as **a mod developer evaluating performance improvements**, I want **a thorough analysis of whether FontServer's StringRenderingJob scheduling can be split into a kick phase (PreSimulation) and a read phase (Rendering)**, so that I **I have a clear implementation plan or a documented reason not to proceed before any code changes are attempted**.

---

## Background

FontServer currently runs entirely within the Rendering phase. Its main work is: 1) For each loaded font, calling FontSystem.RunJobs() which schedules a StringRenderingJob (IJobParallelForBatch), 2) Immediately completing the job (Dependency.Complete()) on the main thread, 3) Processing results (building PrimitiveRenderInformation).

The hypothesis is that the job scheduling (step 1) could be moved to an earlier phase (e.g., PreSimulation) so the worker threads process strings while the main thread runs simulation systems, then results are collected in Rendering. This would overlap font work with simulation work.

However, this is marked as Medium risk because: FontServer uses Texture2D.Apply() for GPU upload (must be on main thread), Atlas-state mutations (glyph rasterization via stbtt) happen before job scheduling, It is unclear whether the pre-job work (atlas mutations) can also be moved earlier.

This analysis task must produce a clear Go / No-Go decision before execution task 0007 is started.

---

## Definition of Ready (DoR)

- [ ] FontServer.cs, FontSystem.cs, and FontAtlas.cs are available for reading
- [ ] The complete RunJobs() and OnUpdate() call chain is traced for one font
- [ ] The dependency graph between FontServer outputs and WEPostRendererSystem inputs is understood
- [ ] CS2 PreSimulation phase execution order relative to Rendering is confirmed

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] All five analysis questions are answered with references to specific files and line numbers
- [ ] A clear Go or No-Go recommendation is written with rationale
- [ ] If Go: a concrete implementation plan is written for task 0007 (which methods move, to which phase, how job handles are passed between phases)
- [ ] If No-Go: task 0007 is marked Canceled (Z) and the reason is documented in this file as an addendum
- [ ] Analysis findings are written as an addendum section at the bottom of this file

---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for



### Is parent of

- [0007]
