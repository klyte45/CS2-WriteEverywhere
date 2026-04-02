# Epic: `seam-refactor` — Introduce Seams for Coverage Expansion

## Objective

Introduce surgical code changes to unlock testability for B/C/D-tier files. This is the one epic that modifies production code — but all changes are designed as **non-behavioral refactors**: the seam is an additional injection point that defaults to the current behavior in production, and tests use it to substitute fakes.

The changes here are what turn the 31% coverage ceiling (T1+T2) into approximately 50–60% coverage of meaningful business logic.

**Key principle:** No seam should ever change existing behavior. Every seam must default to the current production value.

---

## Seam Types Used in This Epic

| Seam type | Example | Use case |
|---|---|---|
| Static delegate field | `public static Func<X,Y> MethodName_binding = ...` | Replace external call in tests without mocking framework |
| Interface extraction | `IECSReader` wrapping `EntityManager` | Inject fake ECS reader in tests |
| `InternalsVisibleTo` | (already done in `testing-infra`) | Access internal members |
| Static `Func<>` override | `static Func<CultureInfo> _cultureProvider = () => real.Culture` | Override static singletons |

---

## Target Files

| File | Current Tier | Target Tier After Seam | Seam Type |
|---|---|---|---|
| `Utils/WEFormulaeEvalCore.cs` | B | A | Extract tokenizer/parser into testable path |
| `Utils/WEFormulaeHelper.cs` | B | A | `InternalsVisibleTo` + internal test hooks |
| `Utils/WELayoutUtility.cs` | D | B | `IECSReader` interface injection |
| `Utils/WEXmlExtensions.cs` | C | B | Separate pure XML from ECS integration |
| `IO/WETextDataXml.cs` | B | A | `IECSReader` injection for `FromEntity`/`ToXml` |
| `Components/WETextData/WETextDataValueFloat.cs` | B | B+ | `IECSReader` seam for `UpdateEffectiveValue` |
| `BuiltinFn/WECalendarFn.cs` | C | B | Binding delegate for `TimeSystem` access |

---

## Task Drafts (7 tasks)

### SR-01 — Extract `IECSReader` interface from EntityManager usage
**Story:** As a developer, I want an `IECSReader` interface that abstracts the `EntityManager` operations (TryGetComponent, TryGetBuffer, HasComponent) so that any class currently calling `World.DefaultGameObjectInjectionWorld.EntityManager` can receive a test double instead.

**DoD checklist:**
- [ ] `IECSReader` interface defined in `BelzontWE` (e.g., `Utils/IECSReader.cs`)
- [ ] Operations: `bool TryGetComponent<T>(Entity e, out T result)`, `bool TryGetBuffer<T>(Entity e, bool isReadOnly, out DynamicBuffer<T> buf)`, `bool HasComponent<T>(Entity e)`, `EntityManager RawManager { get; }` (for escape hatch)
- [ ] `EntityManagerECSReader` concrete class wraps the real `EntityManager` — used in production
- [ ] All test files in later epics that use `IECSReader` reference it from this interface
- [ ] **No behavioural change** in any existing caller yet (that's done in SR-02 through SR-04)

---

### SR-02 — Inject `IECSReader` into `WELayoutUtility.cs`
**Story:** As a developer, I want `WELayoutUtility` to accept an `IECSReader` instead of calling `World.DefaultGameObjectInjectionWorld.EntityManager` directly so that layout utility methods become unit-testable.

**DoD checklist:**
- [ ] `WELayoutUtility` refactored to accept `IECSReader` as constructor parameter or via static injection field
- [ ] All callers updated to pass `new EntityManagerECSReader(World.DefaultGameObjectInjectionWorld.EntityManager)`
- [ ] New `WELayoutUtilityTests.cs` added with ≥8 tests using NSubstitute mock of `IECSReader`
- [ ] No observable behavior change when running in the game

---

### SR-03 — Separate pure XML logic from ECS in `WEXmlExtensions.cs`
**Story:** As a developer, I want the pure XML serialization logic in `WEXmlExtensions` (object-graph operations, element building) separated from the `EntityManager` integration methods so that the XML logic is testable without game context.

**DoD checklist:**
- [ ] Pure helpers extracted into `private static` methods called by the ECS integration entry points
- [ ] Tests in `Utils/WEXmlExtensionsTests.cs` cover the extracted pure helpers (≥10 tests)
- [ ] ECS integration entry points (`ToXml(EntityManager)`, `FromEntity(EntityManager)`) are thin wrappers calling the pure helpers
- [ ] Existing callers are unaffected (signatures unchanged)

---

### SR-04 — Inject `IECSReader` into `WETextDataValueFloat/Int/Float3/String/Color.UpdateEffectiveValue`
**Story:** As a developer, I want the `UpdateEffectiveValue` methods in WETextData value structs to accept an `IECSReader` instead of accessing `World` directly so that formulae evaluation logic is testable.

**DoD checklist:**
- [ ] `UpdateEffectiveValue(IECSReader reader, Entity self, ...)` overload added to each value type
- [ ] Original `UpdateEffectiveValue()` (no args) retains existing signature, internally calls new overload with `EntityManagerECSReader`
- [ ] New tests added to existing `WETextDataValue*Tests.cs` files testing `UpdateEffectiveValue` with a mocked `IECSReader`
- [ ] Existing callers unaffected

---

### SR-05 — Introduce binding delegates in `WECalendarFn.cs`
**Story:** As a developer, I want binding delegate fields on `WECalendarFn` (for `TimeSystem` access and date formatting) so that the calendar formula functions are testable without the game's time simulation running.

**DoD checklist:**
- [ ] Static `Func<>` binding fields added for all `TimeSystem` accesses in `WECalendarFn`
- [ ] Existing `BuiltinFn/WECalendarFnTests.cs` (from `builtin-fn` epic) updated with new time-seam tests
- [ ] Date formatting helper logic tested with fixed time values
- [ ] `[TearDown]` restores original bindings

---

### SR-06 — Extract pure formula tokenizer from `WEFormulaeEvalCore.cs`
**Story:** As a developer, I want the formula string tokenizer/parser logic extracted into a testable internal path in `WEFormulaeEvalCore` so that formula parsing correctness is independently verified.

**DoD checklist:**
- [ ] Tokenizer/parser logic extracted to `internal static` method(s)
- [ ] `Utils/WEFormulaeEvalCoreTests.cs` added with ≥12 tests: valid formula string → correct token list, variable references parsed correctly, method call syntax parsed, nested references, empty formula, malformed formula → error code, edge cases (separators, whitespace)
- [ ] Production `EvaluateFormula(...)` remains the public entry point (no signature change)

---

### SR-07 — Test `WEFormulaeHelper.cs` — formula registration and discovery
**Story:** As a developer, I want tests for the `WEFormulaeHelper` formula registration system so that the Reflection.Emit-based formula-to-method binding is verified to discover and bind the correct methods.

**DoD checklist:**
- [ ] `Utils/WEFormulaeHelperTests.cs` exists with ≥12 tests
- [ ] Tests cover: `SetFormulae<T>()` discovers methods decorated with `[WEFormula]`, returns correct count, discovered names match known formula names, `GetFormulaForType(typeof(string))` returns string formulae, calling a discovered formula delegate returns expected value (using a simple test formulae class), error handling for types with no formulae
- [ ] No production code changes beyond `InternalsVisibleTo` (already done)

---

## Epic Acceptance Criteria

- [ ] `IECSReader` interface exists and has at least one test using NSubstitute
- [ ] `WELayoutUtility` is testable with mocked `IECSReader`
- [ ] `WEFormulaeEvalCore` tokenizer has ≥12 tests
- [ ] `WEFormulaeHelper` registration has ≥12 tests
- [ ] All seams default to pre-existing behavior (no regression in game)
- [ ] Regression test: existing formula evaluation still works in a manual game session (document in task DoD)
