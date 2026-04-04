# Manual Regression Validation Checklist — BelzontWE Seam Refactors

> **Purpose:** Validate that seam-refactor changes (IECSReader injection, WECalendarFn binding seams,
> WEFormulaeEvalCore tokenizer extraction) do NOT break formula evaluation in a running Cities: Skylines 2 game session.
>
> **When to run:** After any changes to the following files:
> - `WECalendarFn.cs` (binding seam changes)
> - `WEFormulaeEvalCore.cs` (tokenizer, dispatch logic)
> - `WEFormulaeHelper.cs` (method discovery, SetFormulae, Emit binding)
> - `WETextDataValue*.cs` (IECSReader injection, UpdateEffectiveValue)
>
> **How to run:** Load a save that has text elements using formulae. Verify each scenario below.
> Mark ✅ if working, ❌ if broken, ⏭️ if not applicable.

---

## 1. WECalendarFn — Time and Date Display

| Scenario | Formula Example | Expected Output | Result |
|---|---|---|---|
| 24-hour time | `&WECalendarFn;GetTimeStringWeLocale` | e.g. `14:30` (no AM/PM) | |
| 12-hour time (locale-dependent) | `&WECalendarFn;GetTimeStringWeLocale` with AM/PM locale | e.g. `2:30 PM` | |
| Date with default format | `&WECalendarFn;GetFormattedDateWeLocale` | e.g. `Apr 2026` (MMM yyyy) | |
| Date with custom format | `&WECalendarFn;GetFormattedDateWeLocale` with `format=dd/MM/yyyy` | e.g. `03/04/2026` | |
| Calendar formula on sign entity | Any calendar formula on a text prop | Updates in real-time as in-game time advances | |

---

## 2. WEFormulaeEvalCore — Formula Chains

| Scenario | Formula Example | Expected Output | Result |
|---|---|---|---|
| Single formula (no chain) | `&MyMod;GetRouteName` | Route name string | |
| Two-step chain | `&MyMod;GetRouteName/&WEStringFn;ToUpperCase` | Uppercase route name | |
| Three-step chain (if applicable) | `&A;FnA/&B;FnB/&C;FnC` | Final chained value | |
| Formula reading vars dict | `&WEVarsFn;GetVar` with `name=myVar` in vars | Value of `myVar` | |
| Unknown function name | `&DoesNotExist;NoSuchMethod` | Display `<InvalidFn2>` tag | |
| Empty formula string | _(leave formula blank)_ | Displays `DefaultValue` | |

---

## 3. WEVehicleFn — Plate and Serial Display

| Scenario | Formula Example | Expected Output | Result |
|---|---|---|---|
| Vehicle plate string | `&WEVehicleFn;GetVehiclePlate` | Vehicle plate (e.g. `AB-1234`) | |
| Bus serial number | `&WEVehicleFn;GetSerialNumber` | Bus serial (numeric) | |
| Null vehicle entity | Formula on non-vehicle entity | Graceful fallback (empty/default) | |

---

## 4. WERouteFn / WEBuildingFn — Context Data

| Scenario | Formula Example | Expected Output | Result |
|---|---|---|---|
| Route name on bus | `&WERouteFn;GetCurrentRoute` | Route name string | |
| Building name on sign | `&WEBuildingFn;GetBuildingName` | Building name or empty | |
| Road number on vehicle | `&WEBuildingFn;GetBuildingRoad` | Road entity description | |

---

## 5. Error Handling in Production

| Scenario | How to Trigger | Expected Outcome | Result |
|---|---|---|---|
| Formula compilation error | Set formula to syntactically invalid string, then reload | Display fallback, no crash | |
| Runtime exception in formula | Formula that accesses missing component | Display `<ERROR>`, no crash | |
| Save/load cycle | Save game with formula text props, reload | Formulae re-evaluate correctly | |
| Multiple entities with formulae | 10+ entities simultaneously with formulae | All display correctly, no lag | |

---

## 6. Seam-Refactor Regression (IECSReader)

| Scenario | After Branch | Expected Outcome | Result |
|---|---|---|---|
| Float formula on building entity | After SR-04 changes | Same result as before refactor | |
| String formula on vehicle entity | After SR-04 changes | Same result as before refactor | |
| Int formula on route entity | After SR-04 changes | Same result as before refactor | |

---

## Notes

- If running with debug logging enabled, check log output for unexpected errors.
- Automated tests in `BelzontWE.Tests` cover the unit-testable paths; this checklist covers game runtime integration.
- Test environment: Cities: Skylines 2 v1.x (match the version under `_auxFiles/refs`).
