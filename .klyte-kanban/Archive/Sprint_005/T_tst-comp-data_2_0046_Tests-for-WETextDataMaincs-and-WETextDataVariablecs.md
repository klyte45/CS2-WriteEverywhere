**End time:** 2026-04-02 18:53 -0300
**Start time:** 2026-04-02 18:52 -0300
# [0046] Tests for WETextDataMain.cs and WETextDataVariable.cs

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the remaining WETextData types**, so that I **coverage of the component data layer is complete**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-07. Test dirty flag propagation in WETextDataMain. Test WETextDataVariable key/value WEStringsBank index round-trip. SetNewParent() tested at type level only (EntityManager call skipped).

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Components/WETextData/WETextDataMainTests.cs exists: tests dirty flag propagation on mutable property sets; SetNewParent() tested at type level only (EntityManager call skipped)
- [x] Components/WETextData/WETextDataVariableTests.cs exists: key/value WEStringsBank index round-trip, struct initializes to zero

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


