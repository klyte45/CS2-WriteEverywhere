# Coverage Gap Review — Sprint 009 Closure

> **Sprint:** Sprint 009 (Sprint 8 of testing cycle)  
> **Date:** 2026-04-04  
> **Reviewer:** Claude-Sonnet-4-6  
> **Baseline:** 939 passing, 29 ignored → **989 passing, 35 ignored** (+50 passing, +6 ignored)

---

## 1. B-Tier Files — Reviewed Against Test Output

| File | Est. Tests (Matrix) | Test File Exists? | Status | Notes |
|---|---|---|---|---|
| `WETextDataMaterial.cs` | 40–60 | ✅ `WETextDataMaterialTests.cs` | ✅ Covered | Sprint 3–6 |
| `WETextDataTransform.cs` | 35–50 | ✅ `WETextDataTransformTests.cs` | ✅ Covered | Sprint 4–5 |
| `WETextDataValueColor.cs` | ~7 | ✅ `WETextDataValueColorTests.cs` | ✅ Covered | Sprint 8: +formula injection |
| `WETextDataValueFloat.cs` | ~8 | ✅ `WETextDataValueFloatTests.cs` | ✅ Covered | Sprint 7–8 |
| `WETextDataValueFloat3.cs` | ~8 | ✅ `WETextDataValueFloat3Tests.cs` | ✅ Covered | Sprint 8: +formula injection |
| `WETextDataValueInt.cs` | ~8 | ✅ `WETextDataValueIntTests.cs` | ✅ Covered | Sprint 7–8 |
| `WETextDataValueString.cs` | ~9 | ✅ `WETextDataValueStringTests.cs` | ✅ Covered | Sprint 7–8 |
| `WETextDataMesh.cs` | 10–15 | ✅ `WETextDataMeshTests.cs` | ✅ Covered | Earlier sprints |
| `WETextDataVariable.cs` | 3–5 | ✅ `WETextDataVariableTests.cs` | ✅ Covered | Earlier sprints |
| `WENumberFormattingFn.cs` | 15–20 | ✅ `WENumberFormattingFnTests.cs` | ✅ Covered | Sprint 6 |
| `WEFormulaeEvalCore.cs` | 20–30 | ✅ `WEFormulaeEvalCore*Tests.cs` | ✅ Covered | Sprint 7–8 |
| `WEFormulaeHelper.cs` | 20–30 | ✅ `WEFormulaeHelperTests.cs` | ✅ Covered | Sprint 8: 23 tests |
| `IO/WESelflessTextDataTree.cs` | 10–15 | ✅ `WESelflessTextDataTreeTests.cs` | ✅ Covered | Earlier sprints |
| `IO/WETextDataXmlTree.cs` | 15–20 | ✅ `WETextDataXmlTreeTests.cs` | ✅ Covered | Earlier sprints |
| `IO/WETextDataXml.cs` | 20–30 | ✅ `WETextDataXmlTests.cs` | ⚠️ Partial | `ToXml(EntityManager)` path blocked |
| `IO/WEImageInfoXml.cs` | 5–8 | ✅ `WEImageInfoXmlTests.cs` | ✅ Covered | Earlier sprints |
| `IO/WEComponentTypeDesc.cs` | 5–8 | ✅ `WEComponentTypeDescTests.cs` | ⚠️ Partial | `Unity.Entities.dll` metadata dependency for IBufferElementData check |
| `Font/System/Font.cs` | 10–15 | ❌ None | ❌ **Gap** | Needs TTF byte fixture; deprioritized (font system is high-effort) |
| `Font/System/FontAtlas.cs` | 20–30 | ❌ None | ❌ **Gap** | Skyline binner testable but needs dedicated font-atlas sprint |
| `Font/System/FontGlyph.cs` | 8–12 | ❌ None | ❌ **Gap** | `GetKerning` blocked by NativeHashMap; pure accessors testable in isolation |
| `Font/Sprites/MaxRectsBinPack.cs` | 20–30 | ❌ None | ❌ **Gap** | Needs `UnityEngine.Rect` metadata ref; pure algorithm but blocked |
| `IO/ObjFileHandler.cs` | 10–15 | ❌ None | ❌ **Gap** | Parsing loop pure; needs `UnityEngine.Vector3/Vector2` metadata |

---

## 2. C-Tier Files — Reviewed Against Test Output

| File | Est. Tests (Matrix) | Test File Exists? | Status | Notes |
|---|---|---|---|---|
| `WETextDataMain.cs` | 3–5 | ⚠️ Partial | ⚠️ Partial | `SetNewParent()` ECS-coupled; dirty flag testable but skipped as low-value |
| `WECalendarFn.cs` | 10–15 | ✅ `WECalendarFnTests.cs` | ✅ Covered | Sprint 6 |
| `WERoadFn.cs` | ~5 | ❌ None | ❌ **Gap** | **Investigated in Sprint 8:** all methods call `World.DefaultGameObjectInjectionWorld.EntityManager` directly — **F-tier in practice** despite matrix rating C. `WENodeElementCache` struct fields could be tested independently but offer low ROI. |
| `WEXmlExtensions.cs` | 10–15 | ❌ None | ❌ **Gap** | XML graph serialization pure portion (non-EntityManager paths) testable; deferred to next cycle |

---

## 3. Gap Summary

### Confirmed Gaps (B-tier, testable with seam work)

| File | Blocker | Required Action |
|---|---|---|
| `Font/System/Font.cs` | Needs TTF fixture file | Create `Fixtures/TestFont.ttf` fixture; dedicate font-reader sprint |
| `Font/System/FontAtlas.cs` | Texture2D rendering methods blocked | Extract skyline-binner logic to pure helper class |
| `Font/System/FontGlyph.cs` | NativeHashMap in `GetKerning` | Test pure accessor fields only; [Ignore] NativeHashMap tests |
| `Font/Sprites/MaxRectsBinPack.cs` | `UnityEngine.Rect` struct | Add `UnityEngine.dll` metadata reference to BelzontWE.Tests.csproj |
| `IO/ObjFileHandler.cs` | `UnityEngine.Vector3/Vector2` structs | Same: add UnityEngine.dll meta ref |
| `WEXmlExtensions.cs` | None — pure portion exists | Extract and test non-EntityManager XML methods directly |

### Gaps Accepted as Out-of-Scope (F-tier reclassification)

| File | Reason |
|---|---|
| `WERoadFn.cs` | Originally C-tier but all methods use `World.DefaultGameObjectInjectionWorld.EntityManager` directly — **reclassified to F-tier in practice** |

---

## 4. Coverage Achievement vs DoD Targets

| DoD Requirement | Result |
|---|---|
| ≥50 new tests added in Sprint 8 | ✅ **+50 passing tests** (939 → 989) |
| ≥720 total passing | ✅ **989 passing** |
| All Sprint 8 epics complete | ✅ FE-05, FE-06, FE-07, SR-07, FE-EXT-01 all T |
| WEFormulaeHelper discovery tests pass | ✅ 23 tests, all passing |
| Coverage gap review documented | ✅ This document |

---

## 5. Follow-Up Cycle Plan (Next Testing Sprint)

Recommended next sprint targets by priority:

### Tier-1: Font Reader (high ROI — pure algorithms)
1. `Font/FileReader/FakePtr.cs` — S-tier, 20–30 tests, pure pointer abstraction
2. `Font/FileReader/Buf.cs` — S-tier, 25–40 tests, pure buffer cursor
3. `Font/System/Font.cs` — B-tier, 10–15 tests, needs TTF fixture
4. `Font/System/FontAtlas.cs` — B-tier, 15–20 tests (binner-only), needs seam extraction

### Tier-2: XML I/O Round-trips
5. `IO/WETextDataXml.cs` — B-tier, 20–30 tests, pure ToXML/FromXML paths
6. `WEXmlExtensions.cs` — C-tier, 8–12 tests, non-EntityManager portion

### Tier-3: Missing UnityEngine-metadata-blocked
7. `Font/Sprites/MaxRectsBinPack.cs` — add `UnityEngine.dll` meta ref, then 20–30 tests
8. `IO/ObjFileHandler.cs` — same meta ref, 10–15 tests

**Prerequisite action:** Add `UnityEngine.dll` as metadata-only reference to `BelzontWE.Tests.csproj` to unblock MaxRectsBinPack and ObjFileHandler in a single step.

---

*Generated as Sprint 009 CLOSE-01 DoD artifact*
