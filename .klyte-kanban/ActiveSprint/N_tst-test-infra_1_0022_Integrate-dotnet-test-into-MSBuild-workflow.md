# [0022] Integrate dotnet test into MSBuild workflow

**Developed by:** 

## User Story

> Acting as **a developer**, I want **to run all tests from the same MSBuild command used for the mod build**, so that I **the test suite runs automatically during local development builds**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-06. Add AfterBuild target that runs dotnet test for Debug configuration. Running MSBuild.exe BelzontWE.sln /p:Configuration=Debug should report test pass/fail.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Frontend.targets or Tests.targets adds AfterBuild target running dotnet test --no-build
- [ ] Tests only run when Configuration=Debug
- [ ] Build does not fail if test runner is not installed — emits a warning instead
- [ ] Running MSBuild.exe BelzontWE.sln /p:Configuration=Debug reports test pass/fail

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


