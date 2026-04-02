# [0064] Inject IECSReader into WETextDataValue*.UpdateEffectiveValue + tests

**Developed by:** 

## User Story

> Acting as **a developer**, I want **UpdateEffectiveValue methods to accept an IECSReader instead of accessing World directly**, so that I **formulae evaluation logic is testable**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-04. Add UpdateEffectiveValue(IECSReader, Entity, ...) overload to each value type. Original method retains signature, calls new overload with EntityManagerECSReader. Add IECSReader mock tests. Depends on SR-01.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] UpdateEffectiveValue(IECSReader reader, Entity self, ...) overload added to Float, Int, Float3, String, Color value types
- [ ] Original UpdateEffectiveValue() retains existing signature, internally calls new overload with EntityManagerECSReader
- [ ] New tests added to existing WETextDataValue*Tests.cs testing UpdateEffectiveValue with mocked IECSReader
- [ ] Existing callers unaffected

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


