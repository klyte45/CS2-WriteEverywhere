# Code Structure Analysis — March 30, 2026

> **Scope**: Deep analysis of the Write Everywhere (BelzontWE) mod for Cities: Skylines II, comparing mod code structure against the game's decompiled source code (v1.5.6).
>
> **Audience**: AI agents and developers working on the mod. Language is optimized for machine parsing while remaining human-readable.

---

## Index of Contents

### 01 — Mod Systems vs Game Systems

Compares the update phase timing of WE systems against the game's own system architecture. Analyzes whether phase assignments can be improved.

| File | Description |
|------|-------------|
| [01_UpdatePhaseMapping.md](01_ModSystems_vs_GameSystems/01_UpdatePhaseMapping.md) | Maps every WE system to its update phase, shows the game's phase execution order, and diagrams the per-frame timeline. |
| [02_TimingImprovementAnalysis.md](01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md) | Data dependency analysis between systems. Evaluates each system's phase placement. Identifies `WETemplateQuerySystem` and `WETemplateDisposalSystem` as candidates for phase relocation. |
| [03_JobSchedulingPatterns.md](01_ModSystems_vs_GameSystems/03_JobSchedulingPatterns.md) | Compares WE job patterns (IJobChunk, IJobParallelFor, IJobParallelForBatch) against game patterns. Identifies synchronous FontServer completion and PreCulling double-complete as improvement areas. |

**Key findings**:
- Most phase assignments are correct and follow logical data dependencies
- Rendering phase is heavily loaded (8 WE systems); 2 can be relocated
- FontServer job completion can be batched across multiple fonts
- PreCulling system's dual-complete pattern could be simplified

---

### 02 — Formulae System

Analysis of the dynamic expression system that drives text properties at runtime via IL-compiled delegates.

| File | Description |
|------|-------------|
| [01_ArchitectureAndDataFlow.md](02_Formulaes/01_ArchitectureAndDataFlow.md) | Complete data structures (19 formula-capable fields per entity), formula string format with all operators, IL compilation pipeline, runtime evaluation flow, variable system, and update scheduling. |
| [02_BuiltinFunctionsReference.md](02_Formulaes/02_BuiltinFunctionsReference.md) | Complete reference of all ~50 built-in static functions across 14 function classes, organized by category. |
| [03_ImprovementAnalysis.md](02_Formulaes/03_ImprovementAnalysis.md) | Evaluates 6 potential improvement areas. Concludes that pre-compilation during template loading and unified formula health reporting are the viable improvements. Burst incompatibility is a correct trade-off. |

**Key findings**:
- IL compilation is the right approach given the flexibility requirements
- Pre-compiling formulas during template load eliminates first-frame stalls
- Most optimization ideas (Burst, per-field intervals, NativeHashMap vars) add complexity beyond what they save
- Staggered update scheduling is well-designed

---

### 03 — Font Processing System

Analysis of the font-to-mesh pipeline including TTF parsing, glyph rasterization, atlas management, and job scheduling.

| File | Description |
|------|-------------|
| [01_ArchitectureAndPipeline.md](03_FontProcessing/01_ArchitectureAndPipeline.md) | Complete class hierarchy, processing pipeline (4 phases: registration, text request, job pipeline, atlas upload), memory layout, atlas packing algorithm, mesh output format, and integration with rendering pipeline. |
| [02_ImprovementAnalysis.md](03_FontProcessing/02_ImprovementAnalysis.md) | Evaluates 6 improvement areas. Atlas copy-on-expand is strongly recommended. Deferred job completion is a moderate improvement. Other areas (jobified rasterization, upload frequency, kerning maps, string limits) are low-impact or high-effort. |

**Key findings**:
- Atlas expansion currently clears all cached glyphs and meshes — the single biggest performance spike source
- Atlas copy-on-expand (using `Graphics.CopyTexture`) would eliminate this
- StringRenderingJob is well-designed with IJobParallelForBatch
- Synchronous per-font job completion could be batched

---

### 04 — Overall Mod Structure

Analysis of the code organization, directory layout, architectural patterns, and maintenance-focused improvements.

| File | Description |
|------|-------------|
| [01_DirectoryOrganization.md](04_OverallModStructure/01_DirectoryOrganization.md) | Complete directory map, folder responsibility matrix, data flow diagram between layers, key architectural patterns (IBelzontBindable, partial classes, singletons, flat ECS composition), and Commons library structure. |
| [02_ImprovementOpportunities.md](04_OverallModStructure/02_ImprovementOpportunities.md) | Six improvement suggestions: promote Templates/ folder, consolidate value wrapper logic, BuiltinFn registration pattern, sub-organize Controllers/, centralize magic constants, add system dependency documentation. All are low-risk, evolutionary changes. |
| [03_RenderingPipelineEndToEnd.md](04_OverallModStructure/03_RenderingPipelineEndToEnd.md) | Complete end-to-end rendering flow from template definition to Graphics.DrawMesh, crossing all system boundaries. Includes entity component architecture and cross-system data dependency table. |

**Key findings**:
- Code organization is well-structured for ~120 files / ~25K LOC
- Feature-based folder organization and consistent naming are strong assets
- WETemplateManager partial class split is effective
- System dependency documentation in the entry point would help future maintainers

---

## Cross-Cutting Recommendations Summary

Ordered by impact-to-effort ratio:

| Priority | Change | Subject | Impact | Effort |
|----------|--------|---------|--------|--------|
| 1 | Atlas copy-on-expand | [Font Processing](03_FontProcessing/02_ImprovementAnalysis.md) | High | Medium |
| 2 | Pre-compile formulas on template load | [Formulaes](02_Formulaes/03_ImprovementAnalysis.md) | Medium | Low |
| 3 | System dependency documentation | [Mod Structure](04_OverallModStructure/02_ImprovementOpportunities.md) | Medium | Very low |
| 4 | Move WETemplateQuerySystem to UIUpdate | [Systems Timing](01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md) | Low | Low |
| 5 | Move WETemplateDisposalSystem to Cleanup | [Systems Timing](01_ModSystems_vs_GameSystems/02_TimingImprovementAnalysis.md) | Low | Low |
| 6 | Batch FontServer job completion | [Job Scheduling](01_ModSystems_vs_GameSystems/03_JobSchedulingPatterns.md) | Medium | Medium |
| 7 | Centralize magic constants | [Mod Structure](04_OverallModStructure/02_ImprovementOpportunities.md) | Low | Low |
| 8 | Unified formula health reporting | [Formulaes](02_Formulaes/03_ImprovementAnalysis.md) | Low (QoL) | Medium |

---

## Reference Information

- **Game version analyzed**: Cities: Skylines II v1.5.6 (decompiled)
- **Mod framework**: Unity ECS (Unity.Entities), HDRP, Burst (partial)
- **Key game systems referenced**: `PreCullingSystem`, `RenderingSystem`, `CameraUpdateSystem`, `GameSystemBase`, `SystemUpdatePhase`
- **Analysis date**: 2026-03-30
