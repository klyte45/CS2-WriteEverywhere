**End time:** 2026-04-02 02:43 -0300
**Start time:** 2026-04-02 02:42 -0300
# [0026] Tests for WEBuiltinAttributes.cs (formula attribute contracts)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests that verify WEBuiltinFunctionAttribute and WEFormulaAttribute construction and AttributeTargets restriction**, so that I **the formula discovery system's contracts are locked**.

---

## Background

[See epic: tst-pure-logic](..\RefsLibrary\2026040123_testing-action-plan\epics\02_Epic_pure-logic.md)

Task PL-02. Verify WEBuiltinFunctionAttribute and WEFormulaAttribute construction, property assignment, and AttributeTargets restriction.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BuiltinFn/WEBuiltinAttributesTests.cs exists
- [ ] Tests cover: constructor property assignment, AttributeTargets.Class on WEBuiltinFunctionAttribute, AttributeTargets.Method on WEFormulaAttribute
- [ ] At least one test verifies attribute is found via reflection on a known target class

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


