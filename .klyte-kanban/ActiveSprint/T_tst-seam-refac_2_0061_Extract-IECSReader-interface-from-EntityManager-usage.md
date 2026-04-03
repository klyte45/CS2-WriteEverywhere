**End time:** 2026-04-03 16:20 -0300
**Start time:** 2026-04-03 16:17 -0300
# [0061] Extract IECSReader interface from EntityManager usage

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **an IECSReader interface that abstracts the EntityManager operations**, so that I **any class currently calling World.DefaultGameObjectInjectionWorld.EntityManager can receive a test double instead**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-01. Define IECSReader interface (TryGetComponent, TryGetBuffer, HasComponent, RawManager). Create EntityManagerECSReader concrete wrapper. No behavioral change in callers yet.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] IECSReader interface defined in BelzontWE (e.g., Utils/IECSReader.cs)
- [x] Operations: TryGetComponent<T>, TryGetBuffer<T>, HasComponent<T>, EntityManager RawManager
- [x] EntityManagerECSReader concrete class wraps the real EntityManager — used in production
- [x] No behavioural change in any existing caller

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


