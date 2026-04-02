# [0018] Configure game DLL metadata references (GameDllRefs.targets)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **the test project to reference critical game assemblies as metadata-only references**, so that I **files with Unity types compile in the test assembly**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-02. Create GameDllRefs.targets MSBuild props file. Reference critical game assemblies (UnityEngine.CoreModule, Unity.Entities, Unity.Collections, Unity.Mathematics) as metadata-only.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] GameDllRefs.targets MSBuild props file created under _Build/
- [ ] Game install path set via environment variable or Directory.Build.props local override (not committed)
- [ ] BelzontWE.Tests.csproj imports GameDllRefs.targets
- [ ] UnityEngine.CoreModule.dll, Unity.Entities.dll, Unity.Collections.dll, Unity.Mathematics.dll resolve
- [ ] If game path not found, build emits warning but does not fail

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


