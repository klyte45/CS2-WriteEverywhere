# [0027] Tests for WEStringsBank.cs (string-to-int mapping, >=12 tests)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **comprehensive tests for the global string-to-integer mapping**, so that I **any regression in this fundamental data structure is caught immediately**.

---

## Background

[See epic: tst-pure-logic](..\RefsLibrary\2026040123_testing-action-plan\epics\02_Epic_pure-logic.md)

Task PL-03. Comprehensive tests for the global string-to-integer mapping. Idempotency, null handling, round-trip, bounds. High priority — used by every formula evaluation.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Systems/WEStringsBankTests.cs exists with >=12 test methods
- [ ] Tests cover: lazy init, index 0 = "", idempotency, string->int->string roundtrip, distinct strings get distinct indices, null returns -1, out-of-bounds returns null, count grows monotonically, whitespace variants are distinct
- [ ] [TearDown] resets singleton or fresh instance used per test

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


