# Epic: `formulae-engine` — Formulae Engine Tests

## Objective

Write tests for the complete formulae evaluation pipeline — the system that takes a user-entered formula string (like `{GetVehiclePlateLine1}` or `{To4DigitsValue;cityPopulation}`), resolves it against registered functions, evaluates any chains, and returns the display value. This is the **highest-complexity business logic** in the mod and the most likely source of subtle bugs.

This epic depends on `seam-refactor` completing first (specifically SR-06 for tokenizer extraction and SR-07 for `WEFormulaeHelper`).

---

## Target Files

| File | Tier (after seams) | Est. Tests | Notes |
|---|---|---|---|
| `Utils/WEFormulaeEvalCore.cs` | A | 15–25 | Tokenizer coverage + eval logic via binding seams |
| `Utils/WEFormulaeHelper.cs` | A | 15–20 | Registration, discovery, Emit binding |
| `Components/WETextData/WETextDataValueFloat.cs` | B | 8–12 | `UpdateEffectiveValue` via IECSReader mock |
| `Components/WETextData/WETextDataValueString.cs` | B | 8–12 | `UpdateEffectiveValue` via IECSReader mock |
| `Components/WETextData/WETextDataValueInt.cs` | B | 6–8 | Same pattern |
| `BuiltinFn/WEBuiltinAttributes.cs` | S | 2–4 | Already done in `pure-logic`; extend here for discovery |
| Integration test: formula round-trip | N/A | 10–15 | Full registration → evaluation with test formula class |

Total: **~64–96 tests**.

---

## Task Drafts (7 tasks)

### FE-01 — Tests for `WEFormulaeEvalCore.cs` — tokenizer correctness
**Story:** As a developer, I want tests for the formula tokenizer in `WEFormulaeEvalCore` so that the parsing step that precedes all formula evaluation is verified for correctness across valid and invalid inputs.

**Prerequisite:** SR-06 (tokenizer extraction) completed.

**DoD checklist:**
- [ ] `Utils/WEFormulaeEvalCoreTests.cs` expanded with tokenizer-specific tests (≥12)
- [ ] Tests cover: single-segment formula, multi-segment chain (`a;b;c`), variable reference syntax (`{varName}`), method call syntax (`{MethodName;arg1}`), whitespace handling, empty string → expected empty behavior, `null` input → no exception, malformed brackets → error code
- [ ] All tests use internal tokenizer method via `InternalsVisibleTo`

---

### FE-02 — Tests for `WEFormulaeEvalCore.cs` — evaluation dispatch
**Story:** As a developer, I want tests for the evaluation dispatch step so that the formula engine correctly routes evaluated segments to the right registered function.

**DoD checklist:**
- [ ] Tests cover: formula that evaluates to a known string, formula evaluation with a variable replaced from dict, unknown formula name → error fallback value, type mismatch in chain → error fallback, null/empty dict handling
- [ ] A minimal `TestFormulaeClass` with `[WEBuiltinFunction]`/`[WEFormula]` attributes is created in the test assembly to avoid game dependency

---

### FE-03 — Integration tests: formula registration → evaluation round-trip
**Story:** As a developer, I want end-to-end tests that start from formula registration (via `WEFormulaeHelper`) through evaluation (via `WEFormulaeEvalCore`) so that the full pipeline is verified without the game.

**DoD checklist:**
- [ ] A test-fixture formulae class is defined in the test assembly with 3+ formulae functions
- [ ] `WEFormulaeHelper.SetFormulae<TestFormulaeClass>()` called in `[SetUp]`
- [ ] `WEFormulaeEvalCore.Evaluate(formulaString, ...)` called and result verified
- [ ] At least: one formula that returns a const string, one that reads from the variable dict, one that chains two transforms
- [ ] `[TearDown]` unregisters test formulae to prevent interaction with other test classes

---

### FE-04 — Tests for `WEFormulaeHelper.cs` — method discovery and Emit binding
**Story:** As a developer, I want tests that verify the Reflection.Emit-based delegate generation in `WEFormulaeHelper` so that the compile-time JIT binding between formula strings and methods is verified.

**DoD checklist:**
- [ ] `Utils/WEFormulaeHelperTests.cs` (extended from SR-07) gains ≥8 more tests here
- [ ] Tests cover: `GetRegisteredFormulaeCount()` returns expected count after `SetFormulae<T>`, calling the generated delegate returns the correct value, formulae with different return types are stored separately by type
- [ ] Test verifies that two distinct calls to `SetFormulae<T>` for the same type are idempotent (no duplicate binding)

---

### FE-05 — Tests for `WETextDataValueFloat/Int/String.UpdateEffectiveValue` via `IECSReader` mock
**Story:** As a developer, I want tests for `UpdateEffectiveValue` with a mocked `IECSReader` so that the formula-evaluation-to-component-update path is verified.

**Prerequisite:** SR-04 (`IECSReader` injection into value types) completed.

**DoD checklist:**
- [ ] `WETextDataValueFloatTests.cs`, `WETextDataValueIntTests.cs`, `WETextDataValueStringTests.cs` each gain ≥6 tests for `UpdateEffectiveValue`
- [ ] Tests use NSubstitute mock of `IECSReader`: mock returns known component data, call `UpdateEffectiveValue`, verify `EffectiveValue` updated, `InitializedEffectiveText = true`
- [ ] Test covers: no formula → uses `DefaultValue`, valid formula → evaluated value, invalid/unknown formula → fallback value

---

### FE-06 — Error path tests: invalid formula handling
**Story:** As a developer, I want tests that verify the formulae engine's error handling so that invalid user formulae fail gracefully (display fallback text, not crash).

**DoD checklist:**
- [ ] Tests for: empty formula string evaluation, formula referencing a non-existent function, formula with wrong argument count, formula producing null result handled as empty string, deeply nested/recursive formula (cycle detection or depth limit)
- [ ] Error fallback values from `WETextDataValueString.s_config` are verified

---

### FE-07 — Tests for `WEFormulaeController.cs` pure logic (if extractable)
**Story:** As a developer, I want to test any pure query/discovery logic in `WEFormulaeController` that can be extracted without the game context, specifically the logic that builds the formulae catalog sent to the UI.

**DoD checklist:**
- [ ] `WEFormulaeController.cs` reviewed for extractable pure logic (formulae catalog building, type classification)
- [ ] If extractable: a `static` helper method extracted and tested (≥5 tests)
- [ ] If not extractable: this task documents why `WEFormulaeController` is F-tier and closes as valid skip

---

## Epic Acceptance Criteria

- [ ] Formula tokenizer has ≥12 tests
- [ ] Formula evaluation dispatch has ≥10 tests
- [ ] At least one end-to-end formula round-trip test (registration → evaluation) passes
- [ ] `UpdateEffectiveValue` for Float, Int, String has IECSReader mock tests
- [ ] All error paths produce fallback values (not exceptions)
- [ ] `dotnet test` passes with ≥60 more tests after this epic
