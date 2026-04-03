**End time:** 2026-04-03 16:25 -0300
**Start time:** 2026-04-03 16:20 -0300
# [0062] Inject IECSReader into WELayoutUtility.cs + unit tests

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **WELayoutUtility to accept an IECSReader instead of calling EntityManager directly**, so that I **layout utility methods become unit-testable**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-02. Refactor WELayoutUtility to accept IECSReader instead of World.DefaultGameObjectInjectionWorld.EntityManager. Add WELayoutUtilityTests.cs with >=8 tests using NSubstitute mock. Depends on SR-01.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] WELayoutUtility refactored to accept IECSReader as constructor parameter or static injection field
- [x] All callers updated to pass new EntityManagerECSReader(...)
- [x] WELayoutUtilityTests.cs added with >=8 tests using NSubstitute mock of IECSReader
- [x] No observable behavior change when running in the game

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


