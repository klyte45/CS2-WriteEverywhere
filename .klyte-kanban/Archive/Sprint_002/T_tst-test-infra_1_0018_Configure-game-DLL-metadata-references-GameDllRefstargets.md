**End time:** 2026-04-02 01:13 -0300
**Start time:** 2026-04-02 01:08 -0300
# [0018] Configure game DLL metadata references (GameDllRefs.targets)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
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

- [x] GameDllRefs.targets MSBuild props file created under _Build/
- [x] Game install path set via environment variable or Directory.Build.props local override (not committed)
- [x] BelzontWE.Tests.csproj imports GameDllRefs.targets
- [x] UnityEngine.CoreModule.dll, Unity.Entities.dll, Unity.Collections.dll, Unity.Mathematics.dll resolve
- [x] If game path not found, build emits warning but does not fail

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


