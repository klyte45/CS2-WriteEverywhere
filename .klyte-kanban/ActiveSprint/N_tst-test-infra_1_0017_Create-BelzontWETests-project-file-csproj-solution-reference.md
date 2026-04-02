# [0017] Create BelzontWE.Tests project file (.csproj + solution reference)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **a .csproj that targets net472, references BelzontWE.csproj, and declares NUnit + NSubstitute NuGet dependencies**, so that I **the build system can compile the test assembly**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-01. Create the .csproj targeting net472, referencing BelzontWE.csproj project-to-project, with NUnit + NSubstitute NuGet dependencies, added to BelzontWE.sln.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BelzontWE.Tests/BelzontWE.Tests.csproj exists with net472 target
- [ ] References BelzontWE.csproj project-to-project
- [ ] NUnit, NUnit3TestAdapter, NSubstitute, NSubstitute.Analyzers.CSharp declared as PackageReference
- [ ] Added to BelzontWE.sln under a Tests solution folder
- [ ] dotnet build BelzontWE.Tests.csproj succeeds with zero test files

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


