# 06 — Decision Log: Phase 1 Scope Refinements

> **Date**: 2026-04-15  
> **Context**: Decisions made after reviewing the Phase 1 sprint roadmap (05_SprintRoadmap.md). Changes reflected in that document.

---

## Decision 1 — VT Registration Confirmed as Phase 2

**Input**: BC7 texture compression + disk cache is confirmed as Phase 1. VT registration stays deferred.

**Decision**: Phase 2 (VT registration) begins **only after Phase 1 is confirmed production-stable**. No VT registration work will be done during Phase 1, even if ahead of schedule.

**Rationale**: VT registration requires format-exact BC7 tile data (padded, z-ordered via `AtlassingUtils.PreProcessData`) and carries higher integration risk. Phase 1 validates the BC7 compression approach first. The disk cache format produced in Phase 1 is intentionally designed to be compatible with Phase 2 VT tiling — raw BC7 bytes from `Texture2D.Compress()` can feed directly into `AtlassingUtils.PreProcessData` without re-compression.

**Impact on 05_SprintRoadmap.md**:
- "Out of Scope" section updated to explicitly state "Phase 2 begins after Phase 1 is production-stable"
- Added note that T2 (BC7 helper) stores raw bytes in a Phase-2-compatible format

---

## Decision 2 — Font Atlas Optimization Deferred

**Input**: Question raised: After calling `makeNoLongerReadable = true` on the font texture, can we still write pixels?

**Answer**: **No.** `Texture2D.Apply(false, makeNoLongerReadable: true)` releases the CPU-side pixel buffer. Any subsequent call to `SetPixels()` or `GetPixels()` throws a `UnityException`. Since `FontAtlas.RenderGlyph()` calls `SetPixels()` every time a new glyph is rendered (on demand, not predictable), making the atlas non-readable would cause a hard exception on the first unseen character encountered after the atlas is "frozen."

**Workaround considered**: Trap the failure and trigger a full destructive reset. This is technically valid but causes a jarring visual reset for users (all font text disappears and re-renders). The risk/reward is poor for a memory optimization.

**Decision**: Font atlas memory optimization is **deferred** from Phase 1. T9 (Font atlas `makeNoLongerReadable`) is removed from the sprint. The font system is left unchanged.

**Future path**: A proper font memory optimization would require a two-buffer architecture (writable ARGB32 staging + compressed read-only for GPU) with a mechanism to invalidate the compressed copy when new glyphs arrive. This is a non-trivial redesign that warrants its own focused sprint.

**Impact on 05_SprintRoadmap.md**:
- T9 (Font atlas) replaced with a deferral notice
- Dependency graph: T9 node removed, `T9 --> T10` edge removed
- Gantt: T9 row removed, "UI & Font" section renamed to "UI"
- Track C (font) removed from parallel tracks
- Task count: 10 → 9
- Expected outcomes: font row removed from memory table

---

## Decision 3 — BC7 Compression Uses Unity API (Not Game Pipeline)

**Input**: User noted that the game has a process that generates textures compressed to VT requirements. We should investigate and use it to avoid incompatibilities.

**Research finding (original — INCORRECT)**: The initial investigation concluded that `PipelinePlugin.dll` was an offline build tool not shipped with the game, and proposed using Unity's `Texture2D.Compress()` as the runtime BC7 path.

**⚠ CORRECTION (2026-04-15, superseded by `07_GameBC7ImportPipelineResearch.md`)**: A direct filesystem search of the Steam installation confirmed that **`PipelinePlugin.dll` IS shipped with the game runtime** at `Cities2_Data/Plugins/x86_64/`. Both `Colossal.AssetPipeline.dll` and `Colossal.AssetPipeline.Native.dll` are present in `Cities2_Data/Managed/`. Unity loads all DLLs in `Plugins/x86_64/` at startup, making them available to mod code.

Corrected availability table:

| Component | Available at Runtime? | Notes |
|---|---|---|
| `NativeTextures.BlockCompress` | **Yes** | P/Invoke into `PipelinePlugin.dll` — confirmed present in `Plugins/x86_64/` |
| `TextureImporter.Texture.CompressBC()` | **Yes** | In `Colossal.AssetPipeline.dll` (Managed/) |
| `AtlassingUtils.PreProcessData` | **Yes** | Pure managed/Burst; tiles pre-existing BC7 bytes into VT layout |
| `VTTextureAsset.PreProcessData` | **Yes** | Delegates to above; needs BC7 bytes as input |
| Unity `Texture2D.Compress(highQuality)` | Yes (but not used) | GPU/driver-based; produces vendor-defined quality — **not** the game's path |

**Corrected Decision**: Phase 1 uses **`TextureImporter.Texture.CompressBC(effort: 3)`** — the same CPU-side BC7 encoder (`PipelinePlugin.dll`) that the game's own asset editor uses. This guarantees format-identical output at the same compression quality. `Texture2D.Compress()` is no longer the proposed path.

Phase 2 compatibility is unchanged: raw BC7 bytes produced by `CompressBC()` can feed directly into `AtlassingUtils.PreProcessData` without re-compression.

**Impact on 05_SprintRoadmap.md**:
- T2 implementation notes corrected: `PipelinePlugin.dll` is runtime-available; `TextureImporter.Texture.CompressBC(effort: 3)` is the BC7 path
- T2 DoD updated: `CompressToBC7()` uses game's own encoder, not `Texture2D.Compress`
- See `07_GameBC7ImportPipelineResearch.md` for full pipeline trace and usage example

---

## Decision 4 — 512×512 Minimum Size Constraint Is Not Applicable

**Input**: There are no atlases in this mod that can be smaller than 512×512. The VT minimum tile size concern is invalid.

**Context**: The `03_AtlasVTActionPlan.md` listed "Minimum 512×512" as a critical VT constraint under R9 (VT registration), with a note that "small atlases (< 512) must be padded up." This derived from `TextureStreamingSystem.ReserveTextureRect` requiring `width ≥ tileSize (512)`.

**Decision**: This constraint is **confirmed non-applicable** to WE atlases. Atlas size 18 (the minimum) maps to 512×512, exactly meeting the VT minimum. No padding logic is needed.

**Impact**: This concern was already in `03_AtlasVTActionPlan.md` (Phase 2 territory) and did not appear in Phase 1 tasks (`05_SprintRoadmap.md`). No change required to `05_SprintRoadmap.md`. The note in `03_AtlasVTActionPlan.md` under R9 "Critical Constraints" (`| **Minimum 512×512** per VT registration | Small atlases (< 512) must be padded up |`) can be struck through or noted as non-applicable when Phase 2 work begins.
