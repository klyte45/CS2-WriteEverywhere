# [0042] Tests for WETextDataTransform.cs (pivot enum mapping, >=35 tests)

**Developed by:** 

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

- [ ] Components/WETextData/WETextDataTransformTests.cs exists with >=35 test methods
- [ ] PivotAsFloat3 tested for all pivot enum combinations (minimum 9 cardinal positions x 2 non-trivial Z variants)
- [ ] ArrayInstancing clamp: (0,0,0)->(1,1,1), (200,200,200)->(100,100,100), valid input unchanged
- [ ] SpacingByAxisOrder for each AxisOrder enum value produces correct stride vector
- [ ] Requires Unity.Mathematics NuGet

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


