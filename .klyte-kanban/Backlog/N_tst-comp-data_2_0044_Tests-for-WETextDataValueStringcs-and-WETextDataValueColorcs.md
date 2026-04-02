# [0044] Tests for WETextDataValueString.cs and WETextDataValueColor.cs

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the string and color value formulae bindings**, so that I **their distinct behaviors (string default+IsEmpty, color fallbacks) are locked**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-05. Test string DefaultValue round-trip, IsEmpty behavior, error fallbacks (errorFallback="<ERROR>", nullFnFallback="<InvalidFn2>"). Test color formulae round-trip.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Components/WETextData/WETextDataValueStringTests.cs exists: DefaultValue round-trip through WEStringsBank, IsEmpty true when both bank indices are 0, IsEmpty false after DefaultValue set, s_config errorFallback=="<ERROR>" and nullFnFallback=="<InvalidFn2>"
- [ ] Components/WETextData/WETextDataValueColorTests.cs exists: Formulae round-trip, SetFormulae("") clears; color fallback values

---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


