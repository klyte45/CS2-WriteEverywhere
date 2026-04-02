**Start time:** 2026-04-02 01:16 -0300
# [0020] Create test folder structure and naming conventions doc

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **the team**, I want **a documented, consistent folder structure for test files**, so that I **contributors know exactly where to place new tests**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-04. Create BelzontWE.Tests/ folder structure mirroring BelzontWE/, add README.md with naming conventions, and create TestBase.cs with SetUp/TearDown hooks.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] BelzontWE.Tests/ mirrors BelzontWE/ folder structure (BuiltinFn/, Font/FileReader/, Components/WETextData/)
- [x] README.md inside BelzontWE.Tests/ explains: test file naming (<TestedClass>Tests.cs), test method naming (MethodName_Condition_ExpectedResult)
- [x] TestBase.cs created with [SetUp]/[TearDown] hooks for shared binding restoration

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


