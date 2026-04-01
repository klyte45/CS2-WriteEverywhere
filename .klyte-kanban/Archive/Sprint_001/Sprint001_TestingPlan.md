# Sprint 001 — Manual Testing Plan

> Since the codebase has no automated tests, all verification must be done manually in CS2 v1.5.6.

## Pre-requisites

- Cities Skylines 2 v1.5.6 installed
- BelzontWE mod compiled from `master` branch (build `65534.2026.401.xxxx`)
- At least one city save with WE templates applied to buildings, vehicles, and road props
- At least one custom font loaded (not the default)
- Existing save that uses formulae-driven text (e.g. bus route numbers, building addresses)

---

## Test Areas

### 1. Font Atlas — Copy-on-Expand (Task 0001)

**What changed:** Font atlas now uses copy-on-expand instead of destructive reset when growing.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 1.1 | Basic font rendering | Load a city with WE text on multiple buildings | All text displays correctly with no missing glyphs |
| 1.2 | Atlas expansion trigger | Use a font with many unique glyphs (CJK or emoji); ensure atlas needs to expand | Text remains visible during expansion; no flash/reset of prior glyphs |
| 1.3 | Multiple fonts | Load 3+ different fonts simultaneously | All fonts render correctly without interference |
| 1.4 | Atlas size limit | Push a font past 8192x8192 (extreme case) | Atlas resets correctly; no crash; text re-renders |

---

### 2. Formula Pre-compilation (Task 0002)

**What changed:** Formulae are now pre-compiled when templates are loaded, not on first evaluation.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 2.1 | Template load performance | Load a city with 50+ formulae-driven texts | No visible stutter on first frame; texts appear immediately |
| 2.2 | Formula evaluation | Check that dynamic texts update (e.g. time, route numbers) | Values change correctly per frame/interval |
| 2.3 | Invalid formula | Create a template with a broken formula string | Formula health log shows status code 1-252; fallback value displayed |

---

### 3. System Phase Corrections (Tasks 0003, 0004)

**What changed:** WETemplateQuerySystem moved to UIUpdate; WETemplateDisposalSystem moved to Cleanup.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 3.1 | Template queries | Use WE editor to query/search templates | Queries respond correctly; no stale data |
| 3.2 | Component disposal | Delete WE text entities or remove templates from buildings | Entities are cleaned up; no orphaned components visible in debug |
| 3.3 | Disposal interval | Verify disposal runs at ~256-frame intervals | Check logs in debug mode: disposal batches are not happening every frame |

---

### 4. Font Job Scheduling (Tasks 0008, 0010)

**What changed:** Two-pass font job scheduling; shared per-font kerning table.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 4.1 | Multi-font cities | Load city with 3+ fonts and many text entities | No font corruption; all fonts render with correct kerning |
| 4.2 | Kerning accuracy | Place text with letter pairs known to have kerning (e.g. "AV", "To") | Letter spacing matches expected kerning for the loaded font |
| 4.3 | Job performance | Monitor frame time during heavy text rendering | No significant frame drops compared to pre-sprint baseline |

---

### 5. Unified Formula Health Reporting (Task 0009)

**What changed:** All 5 WETextDataValue types now use WEFormulaeEvalCore with consistent error handling.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 5.1 | Float formula | Set a formula that returns float (e.g. night light effect) | Value updates correctly; NaN shown on error |
| 5.2 | Int formula | Set a formula that returns int (e.g. module check) | Value updates correctly; MinValue on error |
| 5.3 | String formula | Set a formula that returns string (e.g. entity name) | Text displays correctly; truncated at 500 chars |
| 5.4 | Color formula | Set a formula that returns Color | Color applied correctly; magenta on error |
| 5.5 | Float3 formula | Set a formula that returns float3 (e.g. sidewalk offset) | Position applied correctly |
| 5.6 | Runtime error | Create a formula that throws at runtime | Status=255 logged once (not every frame); fallback value shown |

---

### 6. Template/Controller File Reorganization (Tasks 0011, 0014)

**What changed:** Templates moved from Systems/Templates/ to Templates/; Controllers split into Base/, Data/, Library/ subdirectories.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 6.1 | Template CRUD | Create, edit, copy, delete templates via WE editor | All operations work normally |
| 6.2 | Template save/load | Save a city with WE templates; reload | All templates preserved correctly |
| 6.3 | Prefab templates | Verify prefab-associated templates load from disk | Templates appear on placed buildings/props |

---

### 7. BuiltinFn Attributes (Task 0013)

**What changed:** Added [WEBuiltinFunction] and [WEFormula] attributes to all 14 BuiltinFn classes.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 7.1 | Formula discovery | Use formulae that call built-in functions (e.g. GetEntityName, GetVehiclePlate) | Functions are still discovered and callable via reflection |
| 7.2 | All categories | Test at least one function from each category: Attached, Building, Calendar, City, Colors, Effects, Module, NumberFormatting, Parameter, Renter, Road, Route, Utilities, Vehicle | Each returns expected values |

---

### 8. Centralized Constants (Task 0015)

**What changed:** Magic constants extracted to WEConstants.cs.

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 8.1 | Variable parsing | Create a template with variables (key=value pairs) | Variables parsed correctly using separator chars |
| 8.2 | Template serialization | Save and reload a city with template replacement data | All atlas/font/subtemplate/mesh replacements preserved |
| 8.3 | Frame-staggered updates | Observe update rate in debug mode | Updates happen every 32 frames (RENDERER_FRAME_CHECK_MASK=0x1f) |

---

### 9. System Dependency Documentation (Task 0016)

**What changed:** Comment block added to DoOnCreateWorld().

| # | Test Case | Steps | Expected Result |
|---|-----------|-------|-----------------|
| 9.1 | Compilation | Build succeeds | No compilation issues (verified by build) |

---

## Regression Checklist

| Area | Verification |
|------|-------------|
| Mod loads without errors | Check CS2 mod manager shows WE as loaded |
| WE editor opens and closes | Keyboard shortcut, panel visibility |
| Text renders on buildings | Place a building with a WE template |
| Text renders on vehicles | Observe bus/tram with route numbers |
| Text renders on road props | Check road name signs |
| Image/sprite rendering | Templates with image type work |
| Save/load cycle | Save and reload preserves all WE data |
| Performance | No obvious frame rate degradation vs. baseline |

---

## Notes

- Task 0005 (Move WEPostRendererSystem to Rendering phase) was blocked (Z) due to `BelzontBasicSystem.AllowedPhase` not supporting the Rendering phase — this is a known limitation.
- Task 0007 (FontServer Job Scheduling) was cancelled (C) after deep NO-GO analysis (Task 0006).
- The pre-existing `CS0162` warning in `WEWorldPickerTooltip.cs(39)` is not related to sprint changes.
