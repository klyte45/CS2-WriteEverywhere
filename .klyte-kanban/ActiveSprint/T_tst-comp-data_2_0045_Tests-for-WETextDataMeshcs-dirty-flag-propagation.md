**End time:** 2026-04-02 18:52 -0300
**Start time:** 2026-04-02 18:51 -0300
# [0045] Tests for WETextDataMesh.cs (dirty-flag propagation)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the dirty-flag mechanics in WETextDataMesh**, so that I **the rendering invalidation chain is verified when text properties change**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-06. Test dirty-flag mechanics: TextType setter sets dirty=true, Atlas setter sets dirty=true and templateDirty=true, Font setter sets dirty=true, ResetBri() sets HasBRI=false and MinLod==0.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Components/WETextData/WETextDataMeshTests.cs exists with >=8 tests
- [x] Tests cover: TextType setter sets dirty=true, Atlas setter sets dirty=true and templateDirty=true, Font setter sets dirty=true, ResetBri() sets HasBRI=false and MinLod==0
- [x] CreateDefault(Entity.Null) produces mesh with ValueData.DefaultValue == "NEW TEXT"

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


