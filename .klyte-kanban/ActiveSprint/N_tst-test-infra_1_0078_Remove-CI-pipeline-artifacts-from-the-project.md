# [0078] Remove CI pipeline artifacts from the project

**Developed by:** 

## User Story

> Acting as **a project maintainer**, I want **to remove the automated CI pipeline scaffolding added in Sprint 002**, so that I **the project stays lightweight without unnecessary build automation overhead**.

---

## Background

During Sprint 002, two tasks (TI-06 and TI-07) created CI/MSBuild pipeline artifacts that the project maintainer has decided are not needed: the GitHub Actions workflow file (.github/workflows/tests.yml) and the RunTestsAfterBuild MSBuild target inside BelzontWE.Tests.csproj.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Delete .github/workflows/tests.yml
- [ ] Delete .github/workflows/ folder if empty; delete .github/ folder if empty
- [ ] Remove the RunTestsAfterBuild <Target> block from BelzontWE.Tests.csproj
- [ ] Remove the WarnIfGameDllsNotFound <Target> block if it was only relevant to CI scenario
- [ ] MSBuild.exe BelzontWE.sln /p:Configuration=Debug still builds successfully
- [ ] dotnet test BelzontWE.Tests/BelzontWE.Tests.csproj reports 1 test passed (smoke test)

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


