# [0019] Add InternalsVisibleTo to BelzontWE

**Developed by:** 

## User Story

> Acting as **a test author**, I want **to access internal members of BelzontWE from the test project**, so that I **internal helpers and state can be observed in tests without forcing them to be public**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-03. Add [assembly: InternalsVisibleTo("BelzontWE.Tests")] to BelzontWE so internal members are accessible from the test project.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] [assembly: InternalsVisibleTo("BelzontWE.Tests")] added to BelzontWE/Properties/AssemblyInfo.cs
- [ ] No existing public API changes
- [ ] Verified: at least one internal member is accessible in a test file

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


