# File Testability Matrix — BelzontWE

> **Scope:** All 143 `.cs` files in `BelzontWE/` (excluding `obj/`, `bin/`).  
> **Method counting:** Signatures with `{` body (constructors, static/instance methods, properties with logic, operators). Getter/setter shims for simple auto-properties counted only if they contain non-trivial logic.  
> **Coverage %:** Proportion of methods that have meaningful logic coverable by automated tests (some methods are pure delegates, wiring, or ECS plumbing that cannot be tested without the engine).
>
> **📊 Sprint 009 Coverage Update:** Actual Coverlet line-coverage data collected 2026-04-04.  
> See [05_ActualCoverageReport_Sprint009.md](05_ActualCoverageReport_Sprint009.md) for full file-by-file actuals vs estimates.  
> Overall: **13.8% total line coverage** (2,523/18,204 lines) = ~**44% of coverable surface** tested.

## Tier Legend

| Tier | Meaning |
|---|---|
| **S** | Fully testable without mocking |
| **A** | Fully testable with mocking in some/all methods |
| **B** | Partially testable now, but can be fully testable after some refactor |
| **C** | Partially testable now and can raise coverage after some refactor |
| **D** | Not testable, but fully testable after some refactor |
| **E** | Not testable, but can be partially tested after some refactor |
| **F** | Impossible to test |
| **/** | Not Applicable (no methods to test) |

---

## Enums — `Enum/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEMemberSource.cs` | 56 | 0 | 100% | **S** | Enum + [Description] attributes; value-existence tests |
| `WEMemberType.cs` | 10 | 0 | 100% | **S** | Enum |
| `WEShader.cs` | 9 | 0 | 100% | **S** | Enum |

---

## Components — `Components/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEIsPlaceholder.cs` | 6 | 0 | 100% | **/** | Marker struct — no logic |
| `WEPlacementAlignment.cs` | 26 | 0 | 100% | **S** | Enum |
| `WEPlacementPivot.cs` | 15 | 0 | 100% | **S** | Enum |
| `WESimulationTextType.cs` | 13 | 0 | 100% | **S** | Enum |
| `WESubTextRef.cs` | 9 | 0 | 100% | **/** | Marker struct — no logic |
| `WETemplateForPrefab.cs` | 14 | 0 | 100% | **/** | Marker ECS component, no methods |
| `WETemplateUpdater.cs` | 14 | 0 | 100% | **/** | Marker ECS component, no methods |
| `WEWaitingRendering.cs` | 9 | 0 | 100% | **/** | Marker ECS component, no methods |
| `WEZPlacementPivot.cs` | 9 | 0 | 100% | **S** | Enum |
| `WETextComponentValid.cs` | 7 | 0 | 100% | **/** | Marker struct — no logic |

---

## Components — `Components/WETextData/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WETextDataDirtyFormulae.cs` | 10 | 0 | 100% | **/** | Marker ECS component |
| `WETextDataMain.cs` | 34 | 3 | 30% | **C** | `SetNewParent()` uses EntityManager; `dirty` flag logic is testable |
| `WETextDataMaterial.cs` | 416 | 42 | 90% | **A** | All clamped property setters are pure; `Color`/`EmissiveColor` need `UnityEngine.dll` metadata ref; 40–60 est. tests |
| `WETextDataMesh.cs` | 225 | 18 | 55% | **B** | `ResetBri()`, `CreateDefault()`, dirty-flag propagation testable; `IBasicRenderInformation` via `GCHandle` partially blocked |
| `WETextDataTransform.cs` | 175 | 12 | 95% | **A** | `PivotAsFloat3`, `ArrayInstancing` clamp, `SpacingByAxisOrder` all pure; needs `Unity.Mathematics` NuGet only; 35–50 est. tests |
| `WETextDataValueColor.cs` | 58 | 7 | 70% | **B** | `Formulae` round-trip via WEStringsBank testable; `Color` fallbacks need UnityEngine ref; `UpdateEffectiveValue` blocked by EntityManager |
| `WETextDataValueFloat.cs` | 64 | 8 | 70% | **B** | `Formulae` setter, `SetFormulae`, initial state testable; `UpdateEffectiveValue` blocked |
| `WETextDataValueFloat3.cs` | 60 | 8 | 70% | **B** | Same pattern as `ValueFloat` |
| `WETextDataValueInt.cs` | 57 | 8 | 70% | **B** | Same pattern |
| `WETextDataValueString.cs` | 73 | 9 | 75% | **B** | `DefaultValue`, `IsEmpty`, `SetFormulae` testable; `UpdateEffectiveValue` blocked |
| `WETextDataVariable.cs` | 12 | 1 | 50% | **B** | Simple key/value struct; `WEStringsBank` index round-trip testable |

---

## BuiltinFn — `BuiltinFn/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEBuiltinAttributes.cs` | 24 | 4 | 100% | **S** | Attribute constructors and targets — pure; 4–6 est. tests |
| `WEAttachedFn.cs` | 112 | 8 | 20% | **E** | Geometry algo inside `IJobForEach` — extractable but requires refactor; `NativeArray` blocks direct access |
| `WEBuildingFn.cs` | 29 | 6 | 85% | **A** | Binding seam already in place; null-fallback + delegate replacement fully testable today |
| `WECalendarFn.cs` | 40 | 5 | 30% | **C** | `TimeSystem` binding replaceable; date formatting partially testable; calendar logic verifiable |
| `WECityFn.cs` | 16 | 3 | 0% | **E** | All methods directly use `EntityManager`; after `IECSReader` extraction → D, after full seam → testable |
| `WEColorsFn.cs` | 16 | 3 | 0% | **E** | ECS lookups; no binding seam yet |
| `WEEffectsFn.cs` | 16 | 3 | 0% | **E** | ECS lookups |
| `WEModuleFn.cs` | 22 | 4 | 0% | **E** | Module system query; ECS blocked |
| `WENumberFormattingFn.cs` | 48 | 8 | 80% | **B** | `To4DigitsValue`, `To3DigitsValue`, `DoIntReduction` pure except for locale singleton; one static `Func<CultureInfo>` seam needed; 15–20 est. tests |
| `WEParameterFn.cs` | 47 | 10 | 100% | **S** | Pure dictionary ops; `Entity` param unused; 15–20 est. tests |
| `WERenterFn.cs` | 25 | 4 | 0% | **E** | ECS renter queries |
| `WERoadFn.cs` | 93 | 10 | 0% | **F** | ~~C-tier~~ **Reclassified Sprint 009:** All methods call `World.DefaultGameObjectInjectionWorld.EntityManager` directly — no IECSReader seam. F-tier in practice. |
| `WERouteFn.cs` | 49 | 7 | 75% | **A** | Binding seam in place; `GetWaypointStaticDestination*` path w/ vars dict testable; 10–15 est. tests |
| `WEUtitlitiesFn.cs` | 22 | 4 | 0% | **E** | `NameSystem` + ECS; no accessible seam |
| `WEVehicleFn.cs` | 72 | 10 | 80% | **A** | Binding seam in place; plate split logic, serial modular math pure; 15–20 est. tests |

---

## Font — `Font/FileReader/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `Bmp.cs` | 382 | 22 | 75% | **S** | BMP rasterizer port — pure C#; pixel output math verifiable; 15–25 est. tests |
| `Buf.cs` | 181 | 16 | 100% | **S** | Buffer cursor + big-endian reads — pure; 25–40 est. tests |
| `CharStringContext.cs` | 81 | 6 | 80% | **S** | CFF charstring state — pure; context stack and limit tests |
| `Common.cs` | 1208 | 65 | 85% | **S** | Largest file; platform/encoding constants + codepoint utilities; substantial pure algorithm coverage |
| `FakePtr.cs` | 127 | 18 | 100% | **S** | Pointer abstraction — flagship test target; 20–30 est. tests |
| `FontInfo.cs` | 1593 | 80 | 90% | **S** | Full stbtt port; needs TTF fixture; glyph index, metrics, kerning, bitmap box all testable; 40–60 est. tests |
| `RectPackContext.cs` | 286 | 18 | 85% | **S** | Rect packing algorithm — pure geometry; 20–30 est. tests |

---

## Font — `Font/System/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `Bounds.cs` | 7 | 0 | 100% | **/** | Trivial struct |
| `Font.cs` | 63 | 8 | 90% | **B** | Pure C# (stbtt wrapper); needs TTF byte fixture; `FromMemory`, `GetGlyphIndex` testable; 10–15 est. tests |
| `FontAtlas.cs` | 315 | 22 | 55% | **B** | Skyline binner (node insert/remove/expand) testable; `Texture2D` rendering methods blocked; 20–30 est. tests |
| `FontAtlasNode.cs` | 11 | 0 | 100% | **S** | Plain data struct |
| `FontCreationException.cs` | 21 | 2 | 100% | **S** | Standard exception |
| `FontGlyph.cs` | 67 | 8 | 65% | **B** | Pure accessors, `PadFromBlur`, `xMax/yMax` testable; `GetKerning` blocked by NativeHashMap allocator |
| `FontGlyphBounds.cs` | 13 | 3 | 100% | **S** | Pure struct with `ToString()` |
| `FontServer.cs` | 274 | 18 | 0% | **F** | Game system; NativeArray + Unity allocator everywhere |
| `FontSystem.cs` | 443 | 28 | 0% | **F** | GameSystemBase; Burst jobs; GPU texture updates |
| `FontSystemData.cs` | 76 | 5 | 0% | **F** | NativeArray/NativeHashMap native collections |
| `StringRenderingJob.cs` | 286 | 12 | 0% | **F** | Burst IJobParallelForBatch; NativeArray |

---

## Font — `Font/Sprites/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `MaxRectsBinPack.cs` | 483 | 24 | 70% | **B** | Bin packing algorithm mostly pure; blocked by `UnityEngine.Rect` struct (needs game DLL ref as metadata); 20–30 est. tests with DLL ref |
| `WEAtlasesLibrary.cs` | 531 | 30 | 0% | **F** | Texture2D atlas management; GPU context required |
| `WEAtlasLoadingUtils.cs` | 74 | 6 | 0% | **F** | Texture2D loading |
| `WEImageInfo.cs` | 147 | 12 | 10% | **E** | Name/ID/format fields testable; `Texture2D`, `GameObject.Destroy` block the rest |
| `WEImageInfoXml.cs` | 25 | 3 | 100% | **B** | XML DTO; serialization round-trip testable; needs `Unity.Mathematics` for float2 fields |
| `WEImages.cs` | 14 | 2 | 0% | **F** | Static registry backed by game texture system |
| `WERenderingHelper.cs` | 229 | 14 | 0% | **F** | Shader/GPU rendering |
| `WESpriteInfo.cs` | 62 | 6 | 80% | **B** | UV rect math mostly pure; `UnityEngine.Rect` struct needed; testable with game DLL metadata ref |
| `WETextureAtlas.cs` | 387 | 22 | 0% | **F** | Texture2D/Sprite GPU objects |

---

## IO — `IO/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `ModFolder.cs` | 8 | 1 | 100% | **S** | Static path helper — pure string ops |
| `ObjFileHandler.cs` | 310 | 8 | 75% | **B** | Parsing loop pure; needs `UnityEngine.Vector3/Vector2` metadata ref; 10–15 est. tests |
| `WEComponentTypeDesc.cs` | 35 | 4 | 85% | **B** | `From(Type)` factory; needs `Unity.Entities.dll` metadata ref for `IBufferElementData` check; 5–8 est. tests |
| `WESelflessTextDataTree.cs` | 43 | 5 | 70% | **B** | XML DTO; round-trip testable sans EntityManager |
| `WEStaticMethodDesc.cs` | 37 | 6 | 100% | **S** | `From(MethodInfo)` factories; pure Reflection; 8–12 est. tests |
| `WETextDataXml.cs` | 812 | 45 | 40% | **B** | XML serialization graph — `ToXML()`/`FromXML()` round-trips pure; `ToXml(EntityManager)` and `FromEntity()` blocked; 20–30 est. tests |
| `WETextDataXmlTree.cs` | 146 | 10 | 65% | **B** | `ToXML()`/`FromXML()` round-trips pure; `FromEntity()` blocked; 15–20 est. tests |
| `WETextItemResume.cs` | 11 | 2 | 100% | **S** | Tiny DTO — just field assignment |
| `WETypeMathOperationDesc.cs` | 42 | 5 | 90% | **S** | Descriptor DTO; factory methods pure |
| `WETypeMemberDesc.cs` | 50 | 7 | 100% | **S** | `FromMemberInfo` factory + `supportsMathOp`; pure Reflection; 10–15 est. tests |
| `WEXmlMetadata.cs` | 10 | 2 | 100% | **S** | Property bag DTO |

---

## Mesh — `Mesh/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `IBasicRenderInformation.cs` | 18 | 0 | 100% | **/** | Interface definition only |
| `CustomMeshRenderInformation.cs` | 95 | 8 | 0% | **F** | Wraps `UnityEngine.Mesh`, GPU context required |
| `PrimitiveRenderInformation.cs` | 322 | 20 | 0% | **F** | UnityEngine.Mesh; GPU context |
| `WECustomMeshLibrary.cs` | 397 | 22 | 0% | **F** | Mesh asset management; game file I/O |

---

## Overrides — `Overrides/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `AssetUploadOverrides.cs` | 74 | 6 | 0% | **F** | MonoMod game hooks; method patches |
| `GameUIResourceHandlerOverrides.cs` | 110 | 8 | 0% | **F** | Web resource handler hook |
| `PrefabSystemOverrides.cs` | 75 | 5 | 0% | **F** | Game system hook; ECS |

---

## Systems — `Systems/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEEmissiveLightSystem.cs` | 121 | 7 | 0% | **F** | GameSystemBase + rendering scheduler |
| `WEMainUISystem.cs` | 36 | 4 | 0% | **F** | EUIS binding system |
| `WENodeExtraDataUpdater.cs` | 150 | 9 | 0% | **F** | IJobChunk; ECS entity processing |
| `WENodeExtraDataUpdater2B.cs` | 95 | 7 | 0% | **F** | IJobChunk; ECS |
| `WEPostRendererSystem.cs` | 230 | 12 | 0% | **F** | Rendering pipeline; GPU |
| `WEPreCullingSystem.cs` | 715 | 35 | 0% | **F** | Large rendering gating system |
| `WERendererSystem.cs` | 168 | 11 | 0% | **F** | Rendering coordinator |
| `WEStringsBank.cs` | 26 | 4 | 100% | **S** | Pure C# singleton; string↔int map; 12–15 est. tests (high-value target!) |
| `WEVarsCacheBank.cs` | 30 | 4 | 100% | **S** | Pure C# dictionary cache; testable like WEStringsBank |
| `WEWorldPickerTooltip.cs` | 93 | 6 | 0% | **F** | EUIS tooltip; game context |

---

## Templates — `Templates/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEPrefabLayoutSystem.cs` | 29 | 3 | 0% | **F** | GameSystemBase |
| `WEPrefabTemplateFilterJob.cs` | 132 | 6 | 0% | **F** | IJobChunk; ECS |
| `WETemplateDisposalSystem.cs` | 206 | 10 | 0% | **F** | GameSystemBase; NativeArray |
| `WETemplateManager.CityTemplates.cs` | 70 | 5 | 0% | **F** | Partial class; game ECS |
| `WETemplateManager.cs` | 267 | 16 | 0% | **F** | Partial class; GameSystemBase core |
| `WETemplateManager.EntityProcessing.cs` | 154 | 8 | 0% | **F** | EntityCommandBuffer; IJobChunk |
| `WETemplateManager.ModSubTemplates.cs` | 119 | 7 | 0% | **F** | Mod template registry; game systems |
| `WETemplateManager.ModulesIntegration.cs` | 285 | 15 | 0% | **F** | Module integration; ECS |
| `WETemplateManager.PrefabLayout.cs` | 305 | 18 | 0% | **F** | Prefab processing; ECS |
| `WETemplateManager.SystemCommunication.cs` | 69 | 5 | 0% | **F** | Bridge to game systems |
| `WETemplateQuerySystem.cs` | 133 | 8 | 0% | **F** | QuerySystem; partial |
| `WETemplateUpdateSystem.cs` | 429 | 22 | 0% | **F** | Large coordinator; IJobChunk; ECS |

---

## Tools — `Tools/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEUISystem.cs` | 32 | 3 | 0% | **F** | EUIS; game input system |
| `WEWorldPickerTool.cs` | 512 | 28 | 0% | **F** | ToolSystem; InputAction; raycast |

---

## UI — `UI/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEEditorTool.cs` | 18 | 2 | 0% | **F** | EUIS binding |
| `WEMainPanel.cs` | 10 | 1 | 0% | **F** | EUIS binding |

---

## Bridge — `Bridge/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `FontManagementBridge.cs` | 18 | 3 | 0% | **F** | Cohtml UI bridge |
| `ImageManagementBridge.cs` | 111 | 12 | 0% | **F** | Cohtml UI bridge |
| `LocalizationBridge.cs` | 12 | 2 | 0% | **F** | Cohtml UI bridge |
| `MeshManagementBridge.cs` | 15 | 2 | 0% | **F** | Cohtml UI bridge |
| `ModuleOptionsBridge.cs` | 50 | 6 | 0% | **F** | Cohtml UI bridge |
| `RoadFnBridge.cs` | 15 | 2 | 0% | **F** | Cohtml UI bridge |
| `TemplatesManagementBridge.cs` | 32 | 4 | 0% | **F** | Cohtml UI bridge |

---

## Controllers — `Controllers/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `Base/WEBindableSystemBase.cs` | 16 | 2 | 0% | **F** | Abstract game system base |
| `Base/WETextDataBaseController.cs` | 15 | 2 | 0% | **F** | Abstract controller base |
| `Data/WETextDataMainController.cs` | 22 | 3 | 0% | **F** | Game system; ECS |
| `Data/WETextDataMaterialController.cs` | 230 | 14 | 0% | **F** | Game system + UI bridge |
| `Data/WETextDataMeshController.cs` | 113 | 8 | 0% | **F** | Game system + ECS |
| `Data/WETextDataTransformController.cs` | 97 | 7 | 0% | **F** | Game system + ECS |
| `Library/WECustomMeshLibraryController.cs` | 26 | 3 | 0% | **F** | Mesh asset management; game |
| `Library/WEFontManagementController.cs` | 73 | 7 | 0% | **F** | Font loading; game I/O |
| `Library/WETextureAtlasController.cs` | 63 | 5 | 0% | **F** | Texture atlas; GPU |
| `DebugController.cs` | 232 | 14 | 0% | **F** | Debug EUIS; game systems |
| `FileController.cs` | 51 | 5 | 0% | **F** | File I/O tied to game paths |
| `WEFormulaeController.cs` | 297 | 18 | 0% | **F** | Game system; ECS queries |
| `WELayoutController.cs` | 208 | 14 | 0% | **F** | Layout manager; ECS |
| `WEModulesSystem.cs` | 647 | 38 | 0% | **F** | Largest controller; module registration; ECS |
| `WEWorldPickerController.cs` | 424 | 24 | 0% | **F** | World picker; game tool |

---

## Utils — `Utils/`

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEAddAndEnableComponentJob.cs` | 30 | 3 | 0% | **F** | IJobChunk ECS job |
| `WEAssetsSettingsLoaderUtility.cs` | 84 | 7 | 0% | **F** | File system + game paths; mod lifecycle |
| `WEConstants.cs` | 22 | 0 | 100% | **S** | Pure constants; 5–8 contract tests |
| `WEFormulaeEvalCore.cs` | 91 | 9 | 60% | **B** | Tokenizer and evaluator core logic testable; `EntityManager` calls block full coverage; seam extraction is high value |
| `WEFormulaeHelper.cs` | 691 | 40 | 70% | **B** | Reflection.Emit + pure C# registration logic testable; `GameSystemBase` query methods blocked; formula discovery and registration testable |
| `WELayoutUtility.cs` | 133 | 10 | 0% | **D** | All methods use EntityManager; fully testable after `IECSReader` extraction |
| `WEMaterialUtils.cs` | 139 | 10 | 0% | **F** | `Material` (UnityEngine); shader property IDs; GPU context |
| `WEModIntegrationUtility.cs` | 22 | 3 | 0% | **E** | Module version queries; game type refs |
| `WEXmlExtensions.cs` | 281 | 16 | 30% | **C** | XML object-graph serialization pure; `ToEntity(EntityManager)` / `FromEntity(EntityManager)` entry points blocked; extractable |

---

## Root Files

| File | Lines | Est. Methods | Coverage% (now) | Tier | Notes |
|---|---|---|---|---|---|
| `WEModData.cs` | 237 | 14 | 0% | **F** | Game lifecycle singleton; DI container |
| `WriteEverywhereCS2Mod.cs` | 107 | 6 | 0% | **F** | Mod entry point; Harmony patches |

---

## Aggregate Summary

| Tier | File Count | Est. Total Lines | Est. Coverable Lines Now | Notes |
|---|---|---|---|---|
| **S** | 28 | ~4,370 | ~4,370 | Ready to test immediately |
| **A** | 5 | ~294 | ~250 | Binding seam or mock-in-place; testable today |
| **B** | 22 | ~4,030 | ~2,300 | Partial now; full after minor seam/refactor |
| **C** | 4 | ~424 | ~140 | Small coverage now; grows with refactor |
| **D** | 2 | ~175 | 0 | Zero now; full after refactor |
| **E** | 9 | ~373 | 0 | Zero now; partial after refactor |
| **F** | 54 | ~13,000 | 0 | Engine-bound; not targetable |
| **/** | 19 | ~223 | N/A | Marker types / interfaces |
| **Total** | **143** | **~23,000** | **~7,060** | **~31% of codebase coverable after full T1+T2 work** |

---

## Top Priority Files by Value/Effort Ratio

| Priority | File | Tier | Est. Tests | Effort | Epic |
|---|---|---|---|---|---|
| 1 | `Systems/WEStringsBank.cs` | S | 12–15 | Low | `pure-logic` |
| 2 | `Font/FileReader/FakePtr.cs` | S | 20–30 | Low | `font-reader` |
| 3 | `Font/FileReader/Buf.cs` | S | 25–40 | Medium | `font-reader` |
| 4 | `Components/WETextData/WETextDataMaterial.cs` | A | 40–60 | Low | `component-data` |
| 5 | `Components/WETextData/WETextDataTransform.cs` | A | 35–50 | Low | `component-data` |
| 6 | `BuiltinFn/WEParameterFn.cs` | S | 15–20 | Low | `builtin-fn` |
| 7 | `BuiltinFn/WEVehicleFn.cs` | A | 15–20 | Low | `builtin-fn` |
| 8 | `IO/WETextDataXmlTree.cs` | B | 15–20 | Medium | `io-xml` |
| 9 | `Font/FileReader/FontInfo.cs` | S | 40–60 | Medium | `font-reader` |
| 10 | `Font/System/FontAtlas.cs` | B | 20–30 | Medium | `font-reader` |
| 11 | `Utils/WEFormulaeHelper.cs` | B | 20–30 | High | `formulae-engine` |
| 12 | `Font/FileReader/Common.cs` | S | 25–40 | Medium | `font-reader` |
