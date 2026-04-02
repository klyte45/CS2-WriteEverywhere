# [0069] Tests for WEFormulaeEvalCore.cs - evaluation dispatch

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the evaluation dispatch step**, so that I **the formula engine correctly routes evaluated segments to the right registered function**.

---

## Background

[See epic: tst-formula-eng](..\RefsLibrary\2026040123_testing-action-plan\epics\08_Epic_formulae-engine.md)

Task FE-02. Test evaluation dispatch: formula->known string, variable replaced from dict, unknown formula name->error fallback, type mismatch in chain->error fallback, null/empty dict. Use minimal TestFormulaeClass with WEFormula attributes.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Tests cover: formula that evaluates to a known string, formula evaluation with a variable replaced from dict, unknown formula name->error fallback value, type mismatch in chain->error fallback, null/empty dict handling
- [ ] A minimal TestFormulaeClass with [WEBuiltinFunction]/[WEFormula] attributes created in the test assembly

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


