# [0030] Tests for IO/WEStaticMethodDesc.cs and IO/WETypeMemberDesc.cs

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the formula/member descriptor factories**, so that I **the reflection-based discovery machinery that powers the formulae system's UI never silently breaks**.

---

## Background

[See epic: tst-pure-logic](..\RefsLibrary\2026040123_testing-action-plan\epics\02_Epic_pure-logic.md)

Task PL-06. Test the formula/member descriptor factories: From(MethodInfo), FromMemberInfo for Property/Method/Field, supportsMathOp for numeric/string types.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] IO/WEStaticMethodDescTests.cs exists with >=8 tests: WEDescType constant, FormulaeString format string, From(MethodInfo) extracts class/method names, supportsMathOp true for int/float, false for string
- [ ] IO/WETypeMemberDescTests.cs exists with >=10 tests: FromMemberInfo for Property/Method/Field, WEMemberType assignment, supportsMathOp, FromIndexing(n, typeof(int))

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


