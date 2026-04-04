**End time:** 2026-04-03 21:05 -0300
**Start time:** 2026-04-03 19:55 -0300
# [0070] Integration tests: formula registration to evaluation round-trip

**Developed by:** Claude-Sonnet-4-6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **end-to-end tests from formula registration through evaluation**, so that I **the full pipeline is verified without the game**.

---

## Background

[See epic: tst-formula-eng](..\RefsLibrary\2026040123_testing-action-plan\epics\08_Epic_formulae-engine.md)

Task FE-03. End-to-end tests: fixture formulae class -> WEFormulaeHelper.SetFormulae -> WEFormulaeEvalCore.Evaluate. Cover const string formula, dict-reading formula, chained transforms. TearDown unregisters test formulae.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Test-fixture formulae class defined in test assembly with 3+ formulae functions
- [x] WEFormulaeHelper.SetFormulae<TestFormulaeClass>() called in [SetUp]
- [x] WEFormulaeEvalCore.Evaluate(formulaString, ...) called and result verified
- [x] At minimum: const string formula, reads from variable dict, chains two transforms
- [x] [TearDown] unregisters test formulae to prevent interaction with other test classes

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


