**End time:** 2026-04-02 02:16 -0300
**Start time:** 2026-04-02 01:22 -0300
# [0023] Create CI pipeline file (GitHub Actions)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **the project maintainer**, I want **CI to run tests on every push**, so that I **regressions are caught before manual testing**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-07. Add .github/workflows/tests.yml. CI restores NuGet packages, builds, runs dotnet test. Note: game DLLs unavailable in CI — Unity-dep tests skipped via #if.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] .github/workflows/tests.yml added
- [x] CI restores NuGet packages and builds the test project
- [x] CI runs dotnet test and reports exit code
- [x] README.md in BelzontWE.Tests/ updated to describe CI status badge
- [x] Build matrix note: game DLLs not available in CI — Unity-dep tests skipped via #if or [Ignore]

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


