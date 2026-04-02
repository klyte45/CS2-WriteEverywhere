# [0068] Tests for WEFormulaeEvalCore.cs - tokenizer correctness (>=12 tests)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the formula tokenizer in WEFormulaeEvalCore**, so that I **the parsing step that precedes all formula evaluation is verified for correctness across valid and invalid inputs**.

---

## Background

[See epic: tst-formula-eng](..\RefsLibrary\2026040123_testing-action-plan\epics\08_Epic_formulae-engine.md)

Task FE-01. Expand WEFormulaeEvalCoreTests.cs with tokenizer-specific tests: single-segment, multi-segment chain, variable reference syntax, method call syntax, whitespace, empty/null, malformed brackets. Prerequisite: SR-06.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Utils/WEFormulaeEvalCoreTests.cs expanded with tokenizer-specific tests (>=12)
- [ ] Tests cover: single-segment formula, multi-segment chain (a;b;c), variable reference syntax ({varName}), method call syntax ({MethodName;arg1}), whitespace handling, empty string, null input->no exception, malformed brackets->error code
- [ ] All tests use internal tokenizer method via InternalsVisibleTo

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


