# Actual Coverage Report — Sprint 009

> **Sprint:** Sprint 009 (Sprint 8 of testing cycle)  
> **Date:** 2026-04-04  
> **Tool:** Coverlet 6.0.2 via `dotnet test --collect:"XPlat Code Coverage"` (Cobertura)  
> **Overall:** 13.8% line coverage (2,523 of 18,204 coverable lines)  
> ⚠️ Low overall % is expected — the majority of lines are in F-tier ECS/GPU/game-engine code not coverable without the game runtime.

---

## Key: Legend

| Symbol | Meaning |
|---|---|
| ✅ Exceeded | Actual ≥ estimated |
| ⚠️ Fell short | Actual < estimated |
| ➡️ As expected | Approx. matches estimate |

---

## Files with Coverage Data (actual vs. estimated)

### Components — `Components/WETextData/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `WETextDataVariable.cs` | 50% | **100%** | ✅ Exceeded | All round-trip tests pass |
| `WETextDataMain.cs` | 30% | **88%** | ✅ Exceeded | More lines covered than expected |
| `WETextDataValueFloat.cs` | 70% | **83%** | ✅ Exceeded | Formula injection tests boosted coverage |
| `WETextDataValueFloat3.cs` | 70% | **82%** | ✅ Exceeded | Formula injection tests added Sprint 8 |
| `WETextDataValueString.cs` | 75% | **82%** | ✅ Exceeded | Formula injection tests |
| `WETextDataValueInt.cs` | 70% | **81%** | ✅ Exceeded | Formula injection tests |
| `WETextDataValueColor.cs` | 70% | **81%** | ✅ Exceeded | Formula injection tests added Sprint 8 |
| `WETextDataTransform.cs` | 95% | **64%** | ⚠️ Fell short | Matrix overestimated; many array-instancing paths use EntityManager internally |
| `WETextDataMaterial.cs` | 90% | **18%** | ⚠️ Fell short | Large file (416 lines); most clamped setters have complex EntityManager back-propagation; only pure getters covered |
| `WETextDataMesh.cs` | 55% | **22%** | ⚠️ Fell short | `IBasicRenderInformation` GCHandle paths not exercised |

---

### BuiltinFn — `BuiltinFn/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `WEBuildingFn.cs` | 85% | **100%** | ✅ Exceeded | Binding seam tests complete |
| `WEColorsFn.cs` | 0% | **100%** | ✅ Exceeded (line coverage) | Coverlet reports 100% — but these are ECS delegate registration lines; actual business logic not exercised (methods need game runtime). Lines 1–16 all covered by class-loader. |
| `WEBuiltinAttributes.cs` | 100% | **100%** | ➡️ As expected | |
| `WENumberFormattingFn.cs` | 80% | **89%** | ✅ Exceeded | Locale seam + pure math covered |
| `WECalendarFn.cs` | 30% | **80%** | ✅ Exceeded | Calendar mock tests covered more than estimated |
| `WEParameterFn.cs` | 100% | **53%** | ⚠️ Fell short | Half of parameter path methods not directly invoked by tests; pure S-tier but tests not exhaustive |
| `WEVehicleFn.cs` | 80% | **34%** | ⚠️ Fell short | Plate split and serial math covered; but larger portion of delegate chains not exercised |
| `WERouteFn.cs` | 75% | **27%** | ⚠️ Fell short | WERouteFnTests test only happy paths; error/null branches not covered |

---

### Utils — `Utils/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `WEFormulaeHelper.cs` | 70% | **92%** | ✅ Exceeded | Sprint 8: cache tests, typed variants, IsByRefLikeSafe |
| `WEFormulaeEvalCore.cs` | 60% | **69%** | ✅ Exceeded | Sprint 7–8 integration + dispatch tests |
| `WEFormulaeControllerHelper.cs` | NEW (0% baseline) | **68%** | ✅ New file, sprint 8 extracted | Pure helper; IsTypeIndexable/members paths covered |
| `WELayoutUtility.cs` | 0% | **17%** | ✅ Exceeded (partially) | Some IECSReader-injected paths exercised |
| `WEXmlExtensions.cs` | 30% | **40%** | ✅ Exceeded | More XML serialization paths covered than expected |

---

### Font — `Font/FileReader/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `FakePtr.cs` | 100% | **93%** | ➡️ As expected | One error-boundary path not tested |
| `Buf.cs` | 100% | **66%** | ⚠️ Fell short | Big-endian multi-byte reads not all exercised |
| `Bmp.cs` | 75% | **78–100%** | ➡️ As expected | Multiple classes in file; pixel math covered |
| `CharStringContext.cs` | 80% | **100%** | ✅ Exceeded | |
| `RectPackContext.cs` | 85% | **13–100%** | ⚠️ Complex | Multiple classes; some paths not reached |
| `Common.cs` | 85% | **19%** | ⚠️ Fell short | Large file (1208 lines); test suite covers subset of utility functions |
| `FontInfo.cs` | 90% | **24%** | ⚠️ Fell short | Large file (1593 lines); needs dedicated font sprint |

---

### Font — `Font/System/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `Font.cs` | 90% | **100%** | ✅ Exceeded | Tests exercise full API surface |
| `FontAtlas.cs` | 55% | **48%** | ➡️ As expected | Skyline binner covered; Texture2D paths blocked |
| `FontGlyph.cs` | 65% | **56%** | ➡️ As expected | Pure accessors covered; NativeHashMap blocked |
| `FontGlyphBounds.cs` | 100% | **100%** | ➡️ As expected | |
| `FontCreationException.cs` | 100% | **100%** | ➡️ As expected | |

---

### Font — `Font/Sprites/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `MaxRectsBinPack.cs` | 70% | **60%** | ⚠️ Fell short | Not blocked by UnityEngine.Rect as feared — Coverlet shows 60% line coverage already (referenced at runtime via game DLLs in test env). No dedicated test file yet. |
| `WEImageInfoXml.cs` | 100% | **100%** | ✅ Exceeded | |

---

### IO — `IO/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `WETypeMemberDesc.cs` | 100% | **100%** | ➡️ As expected | |
| `WETypeMathOperationDesc.cs` | 90% | **100%** | ✅ Exceeded | |
| `WESelflessTextDataTree.cs` | 70% | **42%** | ⚠️ Fell short | Tree traversal paths not fully exercised |
| `ObjFileHandler.cs` | 75% | **100%** | ✅ Exceeded | Parsing paths all covered |
| `WEStaticMethodDesc.cs` | 100% | **10%** | ⚠️ Fell short | `From()` blocked by Colossal.IO.AssetDatabase at runtime; only field access covered |
| `WEComponentTypeDesc.cs` | 85% | **6%** | ⚠️ Fell short | `Unity.Entities.IBufferElementData` check blocked at runtime |
| `WETextDataXmlTree.cs` | 65% | **14%** | ⚠️ Fell short | XML round-trip partially exercised |
| `WETextDataXml.cs` | 40% | **3–17% per class** | ⚠️ Fell short | Large multi-class file; EntityManager paths dominate |

---

### Systems — `Systems/`

| File | Estimated% | Actual% | Status | Notes |
|---|---|---|---|---|
| `WEStringsBank.cs` | 100% | **100%** | ✅ Exceeded | Full S-tier coverage |

---

## Overall Coverage Summary

| Category | Estimated Coverable | Actual Achieved | Notes |
|---|---|---|---|
| S-tier files | ~4,370 coverable lines | High (~80%+) | Most S-tier files show 60–100% |
| A/B-tier files | ~2,550 coverable lines | Mixed (20–92%) | Varies by how many game-coupled paths were blocked |
| C/D-tier files | ~315 coverable lines | Low (0–88%) | WECalendarFn/WETextDataMain outperformed; WERoadFn reclassified to F |
| E/F-tier files | Not targetable | ~0% (as expected) | No change, ECS runtime required |

**Key finding:** Coverlet shows 13.8% overall (2,523/18,204 lines), which aligns with the matrix estimate of ~31% coverable after full seam work — we're at roughly **44% of the coverable surface** having tests.

---

## Notable Deviations from Matrix Estimates

| File | Estimated | Actual | Delta | Reason |
|---|---|---|---|---|
| `WEColorsFn.cs` | 0% | 100% (misleading) | +100% | Coverlet counts class-registration lines as "covered"; actual business logic still untested |
| `WETextDataMaterial.cs` | 90% | 18% | -72% | Matrix over-optimistic; EntityManager back-propagation in all setters blocks most of coverage |
| `WEFormulaeHelper.cs` | 70% | 92% | +22% | Sprint 8 cache/typed tests + structural coverage of filter methods |
| `WECalendarFn.cs` | 30% | 80% | +50% | Mock-based tests covered much more than expected |
| `WEParameterFn.cs` | 100% | 53% | -47% | Tests exist but don't invoke all parameter permutations |
| `WERoadFn.cs` | 20% (C-tier) | 0% | -20% | Reclassified to F-tier in practice; all methods use ECS runtime directly |

---

*Generated as Sprint 009 CLOSE-02 DoD artifact*
