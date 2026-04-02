**End time:** 2026-04-02 18:44 -0300
**Start time:** 2026-04-02 18:39 -0300
# [0042] Tests for WETextDataTransform.cs (pivot enum mapping, >=35 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the pivot and instancing logic in WETextDataTransform**, so that I **text layout spatial calculations are regression-proof**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-03. Test pivot and instancing logic: PivotAsFloat3 for all enum combinations, ArrayInstancing clamp, SpacingByAxisOrder for each AxisOrder enum value. Requires Unity.Mathematics NuGet.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Components/WETextData/WETextDataTransformTests.cs exists with >=35 test methods
- [x] PivotAsFloat3 tested for all pivot enum combinations (minimum 9 cardinal positions x 2 non-trivial Z variants)
- [x] ArrayInstancing clamp: (0,0,0)->(1,1,1), (200,200,200)->(100,100,100), valid input unchanged
- [x] SpacingByAxisOrder for each AxisOrder enum value produces correct stride vector
- [x] Requires Unity.Mathematics NuGet

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


