**End time:** 2026-04-03 16:28 -0300
**Start time:** 2026-04-03 16:25 -0300
# [0063] Separate pure XML logic from ECS in WEXmlExtensions.cs + tests

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **the pure XML serialization logic in WEXmlExtensions separated from the EntityManager integration methods**, so that I **the XML logic is testable without game context**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-03. Extract pure XML helpers to private static methods in WEXmlExtensions.cs. ECS integration entry points become thin wrappers. Add WEXmlExtensionsTests.cs (>=10 tests). Existing callers unaffected.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Pure helpers extracted into private static methods called by ECS integration entry points
- [x] Utils/WEXmlExtensionsTests.cs added with >=10 tests covering the extracted pure helpers
- [x] ECS integration entry points (ToXml(EntityManager), FromEntity(EntityManager)) are thin wrappers
- [x] Existing callers are unaffected (signatures unchanged)

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


