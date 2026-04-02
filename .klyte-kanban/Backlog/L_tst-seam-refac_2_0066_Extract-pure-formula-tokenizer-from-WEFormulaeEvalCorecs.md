# [0066] Extract pure formula tokenizer from WEFormulaeEvalCore.cs

**Developed by:** 

## User Story

> Acting as **a developer**, I want **the formula string tokenizer/parser logic extracted into a testable internal path in WEFormulaeEvalCore**, so that I **formula parsing correctness is independently verified**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-06. Extract tokenizer/parser logic to internal static methods in WEFormulaeEvalCore. Add WEFormulaeEvalCoreTests.cs with >=12 tests via InternalsVisibleTo. Production EvaluateFormula remains public entry point.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Tokenizer/parser logic extracted to internal static method(s)
- [ ] Utils/WEFormulaeEvalCoreTests.cs added with >=12 tests: valid formula, multi-segment chain, variable reference, method call syntax, whitespace, empty, null input, malformed brackets
- [ ] Production EvaluateFormula(...) remains the public entry point (no signature change)

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


