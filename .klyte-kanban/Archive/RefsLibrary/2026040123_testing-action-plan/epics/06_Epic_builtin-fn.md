# Epic: `builtin-fn` — BuiltinFn Tests

## Objective

Test the `BuiltinFn/` formula functions — the building blocks of Write Everywhere's dynamic text system. These are the functions users call in formulae like `{GetVehiclePlateLine1}`, `{PrintVariables}`, `{To4DigitsValue}`. Bugs here silently display wrong data on every text on the map.

Three categories exist:
1. **Pure** (`WEParameterFn`) — dictionary ops, no game deps. Testable today as S-tier.
2. **Binding-seam** (`WEBuildingFn`, `WEVehicleFn`, `WERouteFn`) — already have `static Func<>` binding fields. Testable today using delegate replacement.
3. **Seam-needed** (`WENumberFormattingFn`) — one small static-locale seam needed before full testability.

This epic also covers `WEColorsFn` (if pure) and the `WECalendarFn` partial-binding case.

---

## Target Files

| File | Tier | Est. Tests | Notes |
|---|---|---|---|
| `BuiltinFn/WEParameterFn.cs` | S | 15–20 | Pure dict ops; flagship quick-win |
| `BuiltinFn/WEVehicleFn.cs` | A | 15–20 | Binding seam; plate split, serial mod-math |
| `BuiltinFn/WEBuildingFn.cs` | A | 8–12 | Binding seam; null fallback contract |
| `BuiltinFn/WERouteFn.cs` | A | 10–15 | Binding seam; vars dict path |
| `BuiltinFn/WENumberFormattingFn.cs` | B | 15–20 | Locale seam needed; formatting logic high-value |
| `BuiltinFn/WECalendarFn.cs` | C | 5–8 | Time system binding partial; date helpers testable |
| `BuiltinFn/WEColorsFn.cs` | E | 2–4 | Check if any pure color conversion methods exist |

Total: **~70–99 tests**.

---

## Binding-Seam Pattern Recap

The binding seam pattern (already in place for `WEBuildingFn`, `WEVehicleFn`, `WERouteFn`) allows tests without mocking or launching the game:

```csharp
// In test:
WEVehicleFn.GetVehiclePlate_binding = _ => "ABCD1234";
var result = WEVehicleFn.GetVehiclePlateLine1(Entity.Null);
Assert.That(result, Is.EqualTo("ABCD"));

// In [TearDown]: restore original binding
WEVehicleFn.GetVehiclePlate_binding = originalBinding;
```

All test classes using binding seams **must restore** original bindings in `[TearDown]` to prevent cross-test contamination.

---

## Task Drafts (7 tasks)

### BF-01 — Tests for `WEParameterFn.cs` — pure dictionary operations
**Story:** As a developer, I want tests for all `WEParameterFn` methods so that the template variable resolution logic is verified — this is used in every text item that reads from formula variables.

**DoD checklist:**
- [ ] `BuiltinFn/WEParameterFnTests.cs` exists with ≥15 test methods
- [ ] Tests cover: `PrintVariables` with one var, multiple vars (separator confirmed), empty dict → `""`, `RelVarStr1` key found, key not found → `""`, `RelVarStr2` through `RelVarStr4`, `RelVarInt1` valid int, `RelVarInt1` non-numeric → `0`, `RelVarInt1` missing → `0`
- [ ] Entity parameter is `Entity.Null` for all tests (unused by these methods)

---

### BF-02 — Tests for `WEVehicleFn.cs` — plate and serial logic
**Story:** As a developer, I want tests for the vehicle plate and serial number logic so that the mod's vehicle identification display is verified correct.

**DoD checklist:**
- [ ] `BuiltinFn/WEVehicleFnTests.cs` exists with ≥15 test methods
- [ ] Tests cover:
  - `GetVehiclePlateLine1` splits 8-char plate at midpoint (first 4 chars)
  - `GetVehiclePlateLine2` returns second half of plate
  - Odd-length plates split correctly
  - `GetSerialNumber` returns `entity.Index % 100000` padded to 5 digits
  - `GetSerialNumber` wraps correctly for index > 100000
  - Null-fallback: when `GetVehiclePlate_binding` returns null/empty, `GetVehiclePlateLine1/2` handle it gracefully
- [ ] `[TearDown]` restores all modified bindings

---

### BF-03 — Tests for `WEBuildingFn.cs` — binding null-fallback contracts
**Story:** As a developer, I want tests for the building formula functions so that null-binding fallbacks are verified (preventing NullReferenceExceptions when the binding isn't initialized).

**DoD checklist:**
- [ ] `BuiltinFn/WEBuildingFnTests.cs` exists with ≥8 test methods
- [ ] Tests cover: `GetBuildingRoad` when binding returns a real Entity, when binding returns `Entity.Null`, when binding is set to `null` → method returns `Entity.Null` (not throws), same for `GetBuildingRoadNumber` and `GetBuildingMainRenter`
- [ ] `[TearDown]` restores all bindings

---

### BF-04 — Tests for `WERouteFn.cs` — waypoint and transport line logic
**Story:** As a developer, I want tests for the route formula functions so that transport line number display and waypoint resolution are verified.

**DoD checklist:**
- [ ] `BuiltinFn/WERouteFnTests.cs` exists with ≥10 test methods
- [ ] Tests cover: `GetTransportLineNumber` binding replacement, `GetWaypointStaticDestinationName` null-fallback, binding-null → method returns empty string (not throws), `GetNthWaypoint` vars-dict path (if variables dict is used; test with pre-seeded dict)
- [ ] `[TearDown]` restores all bindings

---

### BF-05 — Tests for `WENumberFormattingFn.cs` (after locale seam)
**Story:** As a developer, I want tests for the numeric formatting methods so that the mod's thousands/millions display logic is verified independent of the game's locale state.

**Background:** `WENumberFormattingFn` references `WEModData.InstanceWE.FormatCulture` (game singleton). A static `Func<CultureInfo>` override field must be introduced first (this is the only production change needed).

**DoD checklist:**
- [ ] A `static Func<CultureInfo> FormatCulture_override` field (defaulting to `() => WEModData.InstanceWE.FormatCulture`) added to `WENumberFormattingFn` **as part of this task**
- [ ] `BuiltinFn/WENumberFormattingFnTests.cs` exists with ≥15 tests
- [ ] Tests cover: `To4DigitsValue(1234f) == "1234"`, `To4DigitsValue(12345f) == "12.3k"`, `To3DigitsValue(999f) == "999"`, `To3DigitsValue(1001f) == "1.0k"`, millions range, overflow to `"∞"`, negative values, integer overloads match float equivalents
- [ ] `[SetUp]` sets `FormatCulture_override = () => CultureInfo.InvariantCulture`; `[TearDown]` restores

---

### BF-06 — Tests for `WECalendarFn.cs` — partial binding tests
**Story:** As a developer, I want to test as much of `WECalendarFn` as possible using binding replacement so that the date/time display logic is partially verified even without the game running.

**DoD checklist:**
- [ ] `BuiltinFn/WECalendarFnTests.cs` exists
- [ ] For any method that reads `TimeSystem` via a binding field: test with a fake time value
- [ ] Date string formatting (if isolated in a pure helper): verified with known inputs
- [ ] Tests are clearly marked `[Category("RequiresGameDLL")]` or similar if they depend on game types
- [ ] Note: this is a best-effort task — if zero pure logic exists, document why in test file

---

### BF-07 — Tests for `WEColorsFn.cs` — pure color conversion (if any)
**Story:** As a developer, I want to verify whether `WEColorsFn` has any pure color conversion logic worth testing.

**DoD checklist:**
- [ ] `WEColorsFn.cs` reviewed for any pure computation (e.g., HSV conversion, color math)
- [ ] If pure logic found: `BuiltinFn/WEColorsFnTests.cs` created with appropriate tests
- [ ] If no pure logic found: a note is added to `WEColorsFnTests.cs` explaining why the file is F-tier and not testable

---

## Epic Acceptance Criteria

- [ ] `WEParameterFn` fully covered (15+ tests)
- [ ] `WEVehicleFn` plate split and serial math verified (15+ tests)
- [ ] All binding-seam functions (`WEBuildingFn`, `WEVehicleFn`, `WERouteFn`) have null-fallback tests
- [ ] `WENumberFormattingFn` locale seam introduced and 15+ formatting tests pass
- [ ] `[TearDown]` binding restoration verified in all binding-seam test classes
- [ ] `dotnet test` passes with ≥70 more tests after this epic
