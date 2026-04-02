# Epic: `component-data` — WETextData Component Tests

## Objective

Write tests for all `Components/WETextData/` files. These components are the **core data model** of Write Everywhere — every text item on the map is ultimately a set of these structs. Bugs in their clamping logic, dirty-flag propagation, or string indexing silently corrupt user-placed texts or cause subtle rendering differences that are hard to trace.

Most of these files are tier **A** or **B** and require:
- `Unity.Mathematics` NuGet package (configured in `testing-infra`)
- `UnityEngine.dll` metadata reference for `Color` and `math` types (configured in TI-02)

Some methods are blocked by `EntityManager` and will be excluded from this epic's scope (those belong in `seam-refactor`).

---

## Target Files

| File | Tier | Est. Tests | Notes |
|---|---|---|---|
| `Components/WETextData/WETextDataMaterial.cs` | A | 40–60 | Clamped property setters; Color needs game DLL ref |
| `Components/WETextData/WETextDataTransform.cs` | A | 35–50 | Pivot enum mapping; ArrayInstancing clamp; SpacingByAxisOrder |
| `Components/WETextData/WETextDataValueFloat.cs` | B | 8–12 | Formulae setter; WEStringsBank round-trip; initial state |
| `Components/WETextData/WETextDataValueInt.cs` | B | 8–12 | Same pattern as ValueFloat |
| `Components/WETextData/WETextDataValueFloat3.cs` | B | 8–12 | Same pattern |
| `Components/WETextData/WETextDataValueString.cs` | B | 10–14 | DefaultValue, IsEmpty, error fallbacks |
| `Components/WETextData/WETextDataValueColor.cs` | B | 7–10 | Formulae round-trip; Color fallbacks (with game DLL ref) |
| `Components/WETextData/WETextDataMesh.cs` | B | 8–12 | Dirty-flag propagation; ResetBri; CreateDefault |
| `Components/WETextData/WETextDataMain.cs` | C | 4–6 | dirty-flag logic; SetNewParent blocked (EntityManager) |
| `Components/WETextData/WETextDataVariable.cs` | B | 4–6 | Key/value struct; WEStringsBank index round-trip |

Total: **~132–194 tests**.

---

## Task Drafts (7 tasks)

### CD-01 — Tests for `WETextDataMaterial.cs` — float clamping properties
**Story:** As a developer, I want parameterized tests for every clamped float property in `WETextDataMaterial` so that any change to a clamping range is immediately detected.

**DoD checklist:**
- [ ] `Components/WETextData/WETextDataMaterialTests.cs` exists
- [ ] Each clamped property has a `[TestCase]` table: below-min → min, above-max → max, mid-value → unchanged. Properties tested: `NormalStrength` [0,1], `GlassRefraction` [1,1000], `Metallic` [0,1], `Smoothness` [0,1], `EmissiveIntensity` [0,1000], `EmissiveExposureWeight` [0,1], `CoatStrength` [0,1], `GlassThickness` [0,10]
- [ ] Tests for: `Shader` setter stores value, `RenderBackface` setter sets `dirty = true`, `DEFAULT_DECAL_FLAGS` constant value

---

### CD-02 — Tests for `WETextDataMaterial.cs` — Color properties and dirty flag
**Story:** As a developer, I want tests for `Color` and `EmissiveColor` setters so that dirty-flag propagation for color changes is verified (requires game DLL metadata reference for `UnityEngine.Color`).

**DoD checklist:**
- [ ] Tests (in `WETextDataMaterialTests.cs`) verify: `Color` setter sets `dirty = true`, `EmissiveColor` setter sets `dirty = true`
- [ ] Tests are behind `#if GAME_DLLS_AVAILABLE` or similar compile-time guard if game DLL not present on CI
- [ ] `DEFAULT_DECAL_FLAGS == 8` contract test added

---

### CD-03 — Tests for `WETextDataTransform.cs`
**Story:** As a developer, I want tests for the pivot and instancing logic in `WETextDataTransform` so that text layout spatial calculations are regression-proof.

**DoD checklist:**
- [ ] `Components/WETextData/WETextDataTransformTests.cs` exists with ≥35 test methods
- [ ] `PivotAsFloat3` is tested for all pivot enum combinations (at minimum 9 cardinal positions × 2 non-trivial Z variants)
- [ ] `ArrayInstancing` clamp: input `(0,0,0)` → `(1,1,1)`, input `(200,200,200)` → `(100,100,100)`, valid input passes through unchanged
- [ ] `SpacingByAxisOrder` for each AxisOrder enum value produces the correct stride vector
- [ ] Requires `Unity.Mathematics` NuGet

---

### CD-04 — Tests for `WETextDataValueFloat.cs`, `WETextDataValueInt.cs`, `WETextDataValueFloat3.cs`
**Story:** As a developer, I want tests for the value-type formulae bindings in the three numeric TextData value structs so that the formulae-string ↔ WEStringsBank index round-trip is verified.

**DoD checklist:**
- [ ] `Components/WETextData/WETextDataValueFloatTests.cs` (and Int, Float3) exist
- [ ] Tests cover: initial state (`EffectiveValue == default`, `InitializedEffectiveText == false`), `Formulae` setter stores in WEStringsBank and is readable back, `SetFormulae("")` resets `formulaeStrBnk` to 0, `SetFormulae(null)` resets correctly, invalid formula returns non-zero error code

---

### CD-05 — Tests for `WETextDataValueString.cs` and `WETextDataValueColor.cs`
**Story:** As a developer, I want tests for the string and color value formulae bindings so that their distinct behaviors (string default+IsEmpty, color fallbacks) are locked.

**DoD checklist:**
- [ ] `Components/WETextData/WETextDataValueStringTests.cs` exists: `DefaultValue` round-trip through WEStringsBank, `IsEmpty` true when both bank indices are 0, `IsEmpty` false after `DefaultValue` set, `s_config` `errorFallback == "<ERROR>"` and `nullFnFallback == "<InvalidFn2>"`
- [ ] `Components/WETextData/WETextDataValueColorTests.cs` exists: `Formulae` round-trip, `SetFormulae("")` clears; color fallback values (needs game DLL ref for `Color.cyan`/`Color.magenta`)

---

### CD-06 — Tests for `WETextDataMesh.cs`
**Story:** As a developer, I want tests for the dirty-flag mechanics in `WETextDataMesh` so that the rendering invalidation chain is verified when text properties change.

**DoD checklist:**
- [ ] `Components/WETextData/WETextDataMeshTests.cs` exists with ≥8 tests
- [ ] Tests cover: `TextType` setter sets `dirty = true`, `Atlas` setter sets `dirty = true` and `templateDirty = true`, `Font` setter sets `dirty = true`, `ResetBri()` sets `HasBRI = false` and `MinLod == 0`
- [ ] `CreateDefault(Entity.Null)` produces mesh with `ValueData.DefaultValue == "NEW TEXT"` (requires `Unity.Entities` assembly metadata reference for `Entity` type)

---

### CD-07 — Tests for `WETextDataMain.cs`, `WETextDataVariable.cs`
**Story:** As a developer, I want tests for the remaining WETextData types to complete coverage of the component data layer.

**DoD checklist:**
- [ ] `Components/WETextData/WETextDataMainTests.cs` exists: tests the `dirty` flag propagation on any mutable property set; `SetNewParent()` tested only at the type level (EntityManager call skipped)
- [ ] `Components/WETextData/WETextDataVariableTests.cs` exists: `key`/`value` WEStringsBank index round-trip, struct initializes to zero

---

## Epic Acceptance Criteria

- [ ] `WETextDataMaterial` clamping tests cover all 8 clamped properties with boundary cases
- [ ] `WETextDataTransform` pivot tests cover all enum values
- [ ] All value-type structs (Float, Int, Float3, String, Color) have formulae round-trip tests
- [ ] `dotnet test` passes with ≥130 more tests after this epic
- [ ] No production code changes in this epic (all seam work deferred to `seam-refactor`)
