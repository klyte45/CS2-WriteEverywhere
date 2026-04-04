**End time:** 2026-04-03 19:14 -0300
**Start time:** 2026-04-03 19:07 -0300
# [0065] Introduce binding delegates in WECalendarFn.cs (TimeSystem seam)

**Developed by:** Claude-Sonnet-4-6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **binding delegate fields on WECalendarFn for TimeSystem access**, so that I **calendar formula functions are testable without the game's time simulation running**.

---

## Background

[See epic: tst-seam-refac](..\RefsLibrary\2026040123_testing-action-plan\epics\07_Epic_seam-refactor.md)

Task SR-05. Add static Func<> binding fields for all TimeSystem accesses in WECalendarFn. Update WECalendarFnTests.cs with new time-seam tests. Test date formatting helpers with fixed time values.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Static Func<> binding fields added for all TimeSystem accesses in WECalendarFn
- [x] Existing WECalendarFnTests.cs updated with new time-seam tests
- [x] Date formatting helper logic tested with fixed time values
- [x] [TearDown] restores original bindings

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


