# Epic: `pure-logic` — Pure Logic Tests (S-Tier)

## Objective

Write the first real tests for all files classified as **S-tier** in `01_FileTestabilityMatrix.md`: files with zero Unity/game dependencies that can be run with a plain `dotnet test` immediately after the infrastructure epic. These tests are the fastest to write, impose zero refactoring cost, and form the permanent regression baseline for the mod's core data structures.

The files in this epic are grouped by functional area to keep test suites cohesive.

---

## Target Files

| File | Tier | Est. Tests | Notes |
|---|---|---|---|
| `Enum/WEShader.cs` | S | 3–5 | Enum value identity |
| `Enum/WEMemberSource.cs` | S | 4–6 | Enum + description |
| `Enum/WEMemberType.cs` | S | 3–4 | Enum value count |
| `Components/WEPlacementAlignment.cs` | S | 4 | Enum |
| `Components/WEPlacementPivot.cs` | S | 4 | Enum |
| `Components/WEZPlacementPivot.cs` | S | 4 | Enum |
| `Components/WESimulationTextType.cs` | S | 5 | Enum — TextType used across rendering pipeline |
| `BuiltinFn/WEBuiltinAttributes.cs` | S | 6 | Attribute construction + targets |
| `Systems/WEStringsBank.cs` | S | 15 | **High priority** — string↔int map; idempotency, null, bounds |
| `Systems/WEVarsCacheBank.cs` | S | 8 | Similar pattern to WEStringsBank |
| `Utils/WEConstants.cs` | S | 7 | Contract anchors for separator chars, atlas size, bitmask |
| `IO/WEStaticMethodDesc.cs` | S | 10 | `From(MethodInfo)` factory; format string correctness |
| `IO/WETypeMemberDesc.cs` | S | 12 | `FromMemberInfo` per type; `supportsMathOp` |
| `IO/WETypeMathOperationDesc.cs` | S | 7 | Descriptor factory |
| `IO/WEXmlMetadata.cs` | S | 3 | Property bag |
| `IO/WETextItemResume.cs` | S | 2 | DTO field assignment |

Total: **~107 test cases** across these files.

---

## Task Drafts (7 tasks)

### PL-01 — Tests for Enum files (WEShader, WEMemberSource, WEMemberType, WESimulationTextType, WEPlacementAlignment/Pivot/ZPivot)
**Story:** As a developer, I want enum value-existence and contract tests for all enum types so that accidentally renaming or removing a value is caught at CI before dependent code breaks.

**DoD checklist:**
- [ ] `Enum/WEShaderTests.cs`, `WEMemberSourceTests.cs`, `WEMemberTypeTests.cs` exist
- [ ] `Components/WEPlacementAlignmentTests.cs`, `WEPlacementPivotTests.cs`, `WEZPlacementPivotTests.cs`, `WESimulationTextTypeTests.cs` exist
- [ ] Each test class has: value count assertion, specific named value existence, and `ToString()` / description contract
- [ ] All tests pass in `dotnet test`

---

### PL-02 — Tests for `WEBuiltinAttributes.cs`
**Story:** As a developer, I want tests that verify `WEBuiltinFunctionAttribute` and `WEFormulaAttribute` construction, property assignment, and `AttributeTargets` restriction so that the formula discovery system's contracts are locked.

**DoD checklist:**
- [ ] `BuiltinFn/WEBuiltinAttributesTests.cs` exists
- [ ] Tests cover: constructor property assignment, `AttributeTargets.Class` on `WEBuiltinFunctionAttribute`, `AttributeTargets.Method` on `WEFormulaAttribute`
- [ ] At least one test verifies attribute is found via reflection on a known target class

---

### PL-03 — Tests for `WEStringsBank.cs`
**Story:** As a developer, I want comprehensive tests for the global string-to-integer mapping so that any regression in this fundamental data structure is caught immediately (it is used by every formula evaluation, every saved component, and UI value binding).

**DoD checklist:**
- [ ] `Systems/WEStringsBankTests.cs` exists with ≥12 test methods
- [ ] Tests cover: lazy init, index 0 = `""`, idempotency (`Instance["a"] == Instance["a"]`), string→int→string roundtrip, distinct strings get distinct indices, `null` returns `-1`, out-of-bounds returns `null`, count grows monotonically, whitespace variants are distinct
- [ ] `[TearDown]` resets the singleton or a fresh instance is used per test

---

### PL-04 — Tests for `WEVarsCacheBank.cs`
**Story:** As a developer, I want tests for the variable cache so that the formulae variable-passing system is anchored against regressions.

**DoD checklist:**
- [ ] `Systems/WEVarsCacheBankTests.cs` exists with ≥7 test methods
- [ ] Tests cover: cache stores and retrieves values, missing key returns default, overwrite updates value, different callers/contexts are isolated

---

### PL-05 — Tests for `WEConstants.cs`
**Story:** As a developer, I want tests that pin the specific numeric and character values of mod-wide constants so that accidental modification is caught before it silently breaks parsing or rendering behavior.

**DoD checklist:**
- [ ] `Utils/WEConstantsTests.cs` exists
- [ ] Tests verify: `VARIABLE_ITEM_SEPARATOR != VARIABLE_KV_SEPARATOR`, `MAX_ATLAS_SIZE == 8192`, `RENDERER_FRAME_CHECK_MASK` is a power-of-two minus one (valid bitmask), separator chars are correct ASCII values

---

### PL-06 — Tests for `IO/WEStaticMethodDesc.cs` and `IO/WETypeMemberDesc.cs`
**Story:** As a developer, I want tests for the formula/member descriptor factories so that the reflection-based discovery machinery that powers the formulae system's UI never silently breaks.

**DoD checklist:**
- [ ] `IO/WEStaticMethodDescTests.cs` exists with ≥8 tests: `WEDescType` constant, `FormulaeString` format string, `From(MethodInfo)` extracts class/method names correctly, `supportsMathOp` true for int/float, false for string
- [ ] `IO/WETypeMemberDescTests.cs` exists with ≥10 tests: `FromMemberInfo` for Property, Method, and Field types; `WEMemberType` assignment; `supportsMathOp` for numeric/string; `FromIndexing(n, typeof(int))`

---

### PL-07 — Tests for remaining IO DTOs (`WETypeMathOperationDesc.cs`, `WEXmlMetadata.cs`, `WETextItemResume.cs`)
**Story:** As a developer, I want basic contract tests for the remaining pure IO descriptor types so that the complete DTO layer has test coverage.

**DoD checklist:**
- [ ] `IO/WETypeMathOperationDescTests.cs`, `IO/WEXmlMetadataTests.cs`, `IO/WETextItemResumeTests.cs` exist
- [ ] Tests verify property assignment, factory method outputs, and XML serialization for `WEXmlMetadata`

---

## Epic Acceptance Criteria

- [ ] All S-tier files listed in this epic have at least one test file
- [ ] `dotnet test` passes with ≥90 tests
- [ ] No production code changes
- [ ] `WEStringsBank` is fully covered (greatest regression risk in this group)
