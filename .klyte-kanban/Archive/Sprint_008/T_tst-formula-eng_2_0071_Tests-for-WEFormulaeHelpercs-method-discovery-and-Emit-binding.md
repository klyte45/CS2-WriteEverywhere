**End time:** 2026-04-03 21:08 -0300
**Start time:** 2026-04-03 21:05 -0300
# [0071] Tests for WEFormulaeHelper.cs - method discovery and Emit binding

**Developed by:** Claude-Sonnet-4-6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **tests that verify the Reflection.Emit-based delegate generation in WEFormulaeHelper**, so that I **the compile-time JIT binding between formula strings and methods is verified**.

---

## Background

[See epic: tst-formula-eng](..\RefsLibrary\2026040123_testing-action-plan\epics\08_Epic_formulae-engine.md)

Task FE-04. Extend WEFormulaeHelperTests.cs (from SR-07): GetRegisteredFormulaeCount, calling generated delegate returns correct value, formulae with different return types stored separately, two SetFormulae<T> calls for same type are idempotent.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Utils/WEFormulaeHelperTests.cs extended with >=8 more tests
- [x] Tests cover: GetRegisteredFormulaeCount() returns expected count after SetFormulae<T>, calling generated delegate returns correct value, formulae with different return types stored separately by type
- [x] Test verifies that two distinct calls to SetFormulae<T> for same type are idempotent (no duplicate binding)

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


