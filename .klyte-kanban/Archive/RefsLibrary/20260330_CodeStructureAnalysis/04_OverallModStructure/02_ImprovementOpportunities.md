# Overall Mod Structure: Improvement Opportunities

> **Purpose**: Identifies actionable improvements to the code organization that would make future maintenance easier, based on analysis of coupling, cohesion, and common modification patterns.

## Current Strengths (Not To Be Changed)

Before suggesting improvements, these patterns should be preserved:
- **Feature-based folder organization** — easy to navigate
- **Consistent WE* naming** — unambiguous grep-ability
- **Partial class usage for WETemplateManager** — prevents God class
- **Flat ECS component composition** — Burst-friendly, cache-friendly
- **Bridge pattern** — isolates game API changes
- **BelzontBasicSystem base class** — reduces boilerplate

## Improvement 1: Separate Systems/Templates Into Its Own Folder

### Observation
`Systems/Templates/` contains 12 files (7 partial WETemplateManager + 5 supporting systems). The Templates subsystem is the largest logical unit in the mod, with its own query systems, update systems, disposal systems, and layout systems. It behaves more like a self-contained module than a subfolder of Systems.

### Current
```
Systems/
├── Templates/
│   ├── WETemplateManager.cs (+ 6 partials)
│   ├── WETemplateUpdateSystem.cs
│   ├── WETemplateQuerySystem.cs
│   ├── WETemplateDisposalSystem.cs
│   ├── WEPrefabLayoutSystem.cs
│   └── WEPrefabTemplateFilterJob.cs
├── WERendererSystem.cs
├── WEPreCullingSystem.cs
├── ... (8 other files)
```

### Suggested
Promote `Templates/` to a top-level folder:
```
Templates/
├── WETemplateManager.cs (+ 6 partials)
├── WETemplateUpdateSystem.cs
├── WETemplateQuerySystem.cs
├── WETemplateDisposalSystem.cs
├── WEPrefabLayoutSystem.cs
└── WEPrefabTemplateFilterJob.cs

Systems/
├── WERendererSystem.cs
├── WEPreCullingSystem.cs
├── ... (8 other files)
```

**Benefit**: The Systems folder becomes focused on runtime ECS systems (rendering, culling, node data). The Templates folder becomes a self-contained module. This is a folder move only — no code changes required.

**Risk**: Low. No logical coupling changes.

## Improvement 2: Consolidate Value Wrapper Types

### Observation
The five `WETextDataValue*` types (Float, Int, Float3, Color, String) in `Components/WETextData/` share nearly identical structure:
- `defaultValue` field
- `EffectiveValue` field
- `Formulae` string via `WEStringsBank`
- `loadingFnDone` lazy flag
- `UpdateEffectiveValue()` method

Each is implemented independently, leading to code duplication in the formula compilation and evaluation logic.

### Suggested
These can't be unified into a generic struct (ECS components must be concrete unmanaged types), but the shared logic (compilation, caching, dirty tracking) could be extracted into a shared utility:

```csharp
// Shared formulae evaluation core
static class WEFormulaeEvalCore {
    public static bool TryEvaluate<T>(
        ref int formulaeStrBnk, ref bool loadingFnDone,
        EntityManager em, Entity geometry, Dictionary<string, string> vars,
        out T result);
}
```

**Benefit**: Single place to update formula compilation logic. Currently, a change to the compilation path may need to be replicated across 5 types.

**Risk**: Low. Internal refactor only.

## Improvement 3: BuiltinFn Registration Pattern

### Observation
Built-in functions are discovered at runtime via reflection (`ReflectionUtils.GetInterfaceImplementations` or method scanning). While reflection-based discovery is extensible, it:
- Has no compile-time verification that functions match the expected signature
- Makes it hard to understand which functions are available without reading all 14 files
- Has a startup cost (one-time assembly scan)

### Suggested
Add an interface or attribute-based registration that makes the contract explicit:

```csharp
[WEBuiltinFunction("WEBuildingFn")]
public static class WEBuildingFn {
    [WEFormula(ReturnType = typeof(Entity))]
    public static Entity GetBuildingRoad(Entity reference) { ... }
    
    [WEFormula(ReturnType = typeof(string))]
    public static string GetBuildingRoadNumber(Entity reference) { ... }
}
```

**Benefit**: Compile-time documentation of the function contract. IDE-discoverable. Enables tooling to auto-generate the function reference document.

**Risk**: Low. Additive change — existing reflection discovery can coexist with attributes.

**Alternative**: If attributes are too much ceremony, a simple `README.md` in the BuiltinFn/ folder documenting the function signatures and contracts would achieve the documentation goal without code changes. (See [02_BuiltinFunctionsReference.md](../02_Formulaes/02_BuiltinFunctionsReference.md) for a complete reference that could serve as a starting template.)

## Improvement 4: Controller Folder Size

### Observation
`Controllers/` contains 15 files spanning different responsibilities:
- **Data controllers** (6): WETextDataBaseController, WETextDataMainController, WETextDataMaterialController, WETextDataMeshController, WETextDataTransformController, WETextureAtlasController
- **System controllers** (4): WEModulesSystem, WEFormulaeController, WEFontManagementController, WECustomMeshLibraryController
- **Tool controllers** (2): WEWorldPickerController, WELayoutController
- **Infrastructure** (2): FileController, DebugController
- **Base** (1): WEBindableSystemBase

These serve different user-facing features and change at different rates.

### Suggested
Organize by purpose:
```
Controllers/
├── Base/
│   └── WEBindableSystemBase.cs
│   └── WETextDataBaseController.cs
├── Data/
│   ├── WETextDataMainController.cs
│   ├── WETextDataMaterialController.cs
│   ├── WETextDataMeshController.cs
│   └── WETextDataTransformController.cs
├── Library/
│   ├── WEFontManagementController.cs
│   ├── WECustomMeshLibraryController.cs
│   └── WETextureAtlasController.cs
├── WEWorldPickerController.cs
├── WELayoutController.cs
├── WEFormulaeController.cs
├── WEModulesSystem.cs
├── FileController.cs
└── DebugController.cs
```

**Benefit**: Clearer grouping by change frequency. Data controllers change when components change. Library controllers change when asset management changes.

**Risk**: Low. Folder reorganization only.

**Alternative**: Given the project's current size (15 files), this reorganization may not be necessary yet. It becomes more valuable if more controllers are added.

## Improvement 5: Centralize Magic Constants

### Observation
The codebase contains several scattered constants:
- `VARIABLE_ITEM_SEPARATOR = '↓'` and `VARIABLE_KV_SEPARATOR = '→'` in `WEPreCullingSystem`
- Template replacement separators (`|`, `→`, `∫`, `↓`) in `WETemplateManager`
- Frame interval masks (`& 0x1f`, `& 0xFF`) in renderers
- Font atlas limits (`16384`, `512`)
- Mesh batch sizes (`256`, `32`)

### Suggested
Create a `WEConstants.cs` file in the root or Utils/ folder:

```csharp
public static class WEConstants {
    // Variable serialization
    public const char VARIABLE_ITEM_SEPARATOR = '↓';
    public const char VARIABLE_KV_SEPARATOR = '→';
    
    // Template replacement separators
    public const char REPLACEMENT_ITEM_SEPARATOR = '|';
    public const char REPLACEMENT_KV_SEPARATOR = '→';
    public const char REPLACEMENT_SUB_SEPARATOR = '∫';
    public const char REPLACEMENT_SUB_KV_SEPARATOR = '↓';
    
    // Font system
    public const int MAX_ATLAS_SIZE = 16384;
    public const int MIN_ATLAS_SIZE = 512;
    public const int FONT_JOB_BATCH_SIZE = 256;
    public const int STRING_RENDERING_BATCH = 32;
    
    // Rendering
    public const int RENDERER_FRAME_CHECK_MASK = 0x1F;
    public const int DISPOSAL_FRAME_INTERVAL = 256;
}
```

**Benefit**: Single source of truth for magic numbers. Easier to find and modify. Self-documenting.

**Risk**: Very low. No behavioral change.

## Improvement 6: Explicit System Dependency Documentation

### Observation
System dependencies are implicit — discovered only by reading `GetSystem<T>()` calls and `UpdateBefore/UpdateAfter` attributes. There's no centralized view of which system depends on which.

### Suggested
Add a comment block to `WriteEverywhereCS2Mod.DoOnCreateWorld()` documenting the dependency graph:

```csharp
// System Dependency Graph:
// Modification1: WENodeExtraDataUpdater (produces WENetNodeInformation)
// Modification2B: WENodeExtraDataUpdater2B (invalidates WENetNodeInformation)
// ModificationEnd: WEWorldPickerController (produces selection state)
// Rendering: FontServer → WEAtlasesLibrary → WECustomMeshLibrary
//            → WETemplateManager → WETemplateUpdateSystem
//            → WETemplateQuerySystem → WEPrefabLayoutSystem
//            → WETemplateDisposalSystem
// PreCulling: WEPreCullingSystem (consumes game culling, produces m_availToDraw)
// Unity Callback: WERendererSystem (consumes m_availToDraw)
// MainLoop: WEPostRendererSystem, WEEmissiveLightSystem
```

**Benefit**: New developers (or agents) can quickly understand system ordering without reading all files.

**Risk**: None. Documentation only.

## Summary

| ID| Improvement | Type | Effort | Impact |
|---|------------|------|--------|--------|
| 1 | Promote Templates/ to top-level | Folder move | Low | Medium — cleaner Systems/ |
| 2 | Consolidate value wrapper logic | Refactor | Medium | Medium — reduces duplication |
| 3 | BuiltinFn registration pattern | Additive | Low | Low — documentation benefit |
| 4 | Sub-organize Controllers/ | Folder move | Low | Low-Medium — grows with time |
| 5 | Centralize magic constants | New file | Low | Low — self-documenting |
| 6 | System dependency documentation | Comment | Very low | Medium — onboarding benefit |

## Conclusion

The mod's code organization is well-structured for its current size (~120 .cs files, ~25,000 LOC). The suggested improvements are evolutionary — they would make the codebase more navigable and maintainable as it grows, but none are urgent blockers. The highest-value improvement is **system dependency documentation** (#6) because it costs nearly nothing and helps every future reader. The **Templates promotion** (#1) is the most structurally significant change that reduces the Systems folder's cognitive load.
