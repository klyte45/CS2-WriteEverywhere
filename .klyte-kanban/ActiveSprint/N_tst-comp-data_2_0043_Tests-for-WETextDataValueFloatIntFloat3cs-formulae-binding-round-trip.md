# [0043] Tests for WETextDataValueFloat/Int/Float3.cs (formulae binding round-trip)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the value-type formulae bindings in the three numeric TextData value structs**, so that I **the formulae-string <-> WEStringsBank index round-trip is verified**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-04. Test the value-type formulae bindings in numeric TextData value structs: initial state, Formulae setter WEStringsBank index round-trip, SetFormulae('') reset, null reset.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Components/WETextData/WETextDataValueFloatTests.cs (and Int, Float3) exist
- [ ] Tests cover: initial state (EffectiveValue==default, InitializedEffectiveText==false), Formulae setter stores in WEStringsBank and is readable back, SetFormulae("") resets formulaeStrBnk to 0, SetFormulae(null) resets correctly, invalid formula returns non-zero error code

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


