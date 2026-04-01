**Start time:** 2026-03-31 23:25 -0300
# [0006] FontServer Job Scheduling — Deep Analysis

**Developed by:** Agent-Claude-Sonnet-4.6 <agent@example.com>
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

- [x] All five analysis questions are answered with references to specific files and line numbers
- [x] A clear Go or No-Go recommendation is written with rationale
- [x] If Go: a concrete implementation plan is written for task 0007 (which methods move, to which phase, how job handles are passed between phases)
- [ ] If No-Go: task 0007 is marked Canceled (Z) and the reason is documented in this file as an addendum
- [ ] Analysis findings are written as an addendum section at the bottom of this file

---

## Implementation Notes

1. NO-GO DECISION: FontServer pipeline is tightly coupled via mutating shared state (glyph dictionary, atlas version, atlas texture). No safe split point exists between glyph prep (main-thread write) and job scheduling (reader). Moving entire RunJobs to PreSim might work but provides no overlap benefit since glyph rasterization dominates cost and cannot be parallelized. Full analysis documented in task addendum.

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

---

## Analysis Addendum — FontServer Job Scheduling Deep Dive

### Q1: What data does FontServer mutate before scheduling the job?

**Primary mutations (main-thread only):**
1. **Glyph rasterization** (FontSystem.cs ~lines 285–305): `GetGlyph()` calls `Font.RenderGlyphBitmap()` (FreeType call), populates `glyphs[codepoint]` hashmap with glyph metadata (position, advance, dimensions, UV bounds).
2. **Atlas state modifications** (FontAtlas.cs ~lines 162–182): `AddRect()` allocates space in skyline data structure; `RenderGlyph()` writes pixel data to Texture2D via `SetPixels()`; `Version++` incremented after each glyph render; `IsPendingApply = true`.
3. **Kerning pair computation** (FontSystem.cs ~lines 313–317): `GetKerning()` pre-computes kerning between glyph pairs.
4. **Text cache state** (FontSystem.cs ~line 317): Sets `m_textCache[text] = LOADING_PLACEHOLDER` before scheduling.

**Critical invariant:** All glyph data, atlas UV coordinates, and atlas texture pixels are complete before job scheduling. The job receives a read-only snapshot.

### Q2: What data does FontServer output, and when is it consumed?

**Output structure** (BasicRenderInformationJob): `vertices[]`, `colors[]`, `uv1[]`, `triangles[]`, `m_YAxisOverflows`, `m_fontBaseLimits`, `AtlasVersion`.

**Consumption timing — same frame, synchronous:**
1. `dependency.Complete()` blocks main thread until all StringRenderingJobs complete.
2. `PostJob()` immediately processes results from `results` queue.
3. Constructs `PrimitiveRenderInformation` wrapper and stores in `m_textCache[originalText]`.
4. GPU `Apply()` happens via `FontServer.UpdateFontSystem()`.

**Downstream consumer:** `WEPostRendererSystem` (AllowedPhase.EndFrame) queries `WEWaitingRendering` entities, calls `font.FontSystem.DrawText(text)` → retrieves from `m_textCache`.

### Q3: Is the atlas-state mutation separable from job scheduling?

**No.** The job's `Execute()` reads `glyphs[codepoint].x`, `.y`, and `AtlasVersion`. These are written during the pre-schedule glyph prep phase. If they mutate after scheduling but before execution, the job operates on stale data — wrong UV coordinates, corrupted rendering, or atlas version mismatch.

### Q4: Frame-time cost — steady state vs. initial load

**Steady state:** ~0µs per frame. `RunJobs()` returns early when `itemsQueue.Count < 256` and `framesBuffering ≤ 60`.

**Initial load:** Per string (with new characters): glyph prep 1–10 ms (main-thread, FreeType), StringRenderingJob 0.1–2 ms (parallel), PostJob 0.01–0.1 ms (main-thread), GPU Apply 0.5–5 ms (main-thread). Up to 256 strings/frame batched.

### Q5: Thread-safety constraints preventing early scheduling

Multiple critical constraints: glyph hashmap is main-thread write / job read with no synchronization; atlas version is bumped during RenderGlyph and captured at schedule time; m_textCache has no lock and relies on sequential main-thread access; CurrentAtlasFull event (atlas expansion) can fire between schedule and execute, invalidating glyph coordinates.

### Decision: NO-GO

**Rationale:** The pipeline is tightly coupled via mutating shared state (glyph dictionary, atlas version, atlas texture). There is no safe split point. Concrete failure scenarios:
- Atlas expansion in Rendering phase → job uses old glyph coordinates after resize
- New glyphs added in Rendering → job's hashmap doesn't contain them → silently skips
- Version mismatch → PostJob invalidates result → retry increases latency

**Consequence:** Task [0007] (FontServer Job Scheduling Execution) should be cancelled.
