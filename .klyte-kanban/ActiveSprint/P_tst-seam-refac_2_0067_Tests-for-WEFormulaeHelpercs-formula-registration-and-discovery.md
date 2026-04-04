**Start time:** 2026-04-03 23:58 -0300
# [0067] Tests for WEFormulaeHelper.cs - formula registration and discovery

**Developed by:** Claude-Sonnet-4-6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **tests for the WEFormulaeHelper formula registration system**, so that I **the Reflection.Emit-based formula-to-method binding is verified to discover and bind the correct methods**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-07. Test WEFormulaeHelper formula registration system: SetFormulae<T>() discovers [WEFormula] methods, returns correct count, names match known formulae, GetFormulaForType returns correct type, calling generated delegate returns expected value. Depends on formulae-engine work.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Utils/WEFormulaeHelperTests.cs exists with >=12 tests
- [ ] Tests cover: SetFormulae<T>() discovers methods decorated with [WEFormula], returns correct count, discovered names match known formula names, GetFormulaForType(typeof(string)) returns string formulae, calling a discovered formula delegate returns expected value using a test formulae class
- [ ] No production code changes beyond InternalsVisibleTo (already done)

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


