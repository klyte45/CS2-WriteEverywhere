**Start time:** 2026-04-02 02:16 -0300
# [0024] Smoke test: validate full test pipeline end-to-end

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a QA baseline**, I want **a trivial test that always passes and one I can toggle to always fail**, so that I **I can verify the test runner is wired correctly before writing real tests**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-08. Create PipelineSmokeTests.cs with AlwaysPasses() and a toggle-fail smoke test. Verify dotnet test reports at least 1 passed.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] PipelineSmokeTests.cs has AlwaysPasses() returning Assert.Pass()
- [x] Running dotnet test shows 1 passed or more
- [x] Smoke test file documents what NuGet packages and assemblies were successfully resolved

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


