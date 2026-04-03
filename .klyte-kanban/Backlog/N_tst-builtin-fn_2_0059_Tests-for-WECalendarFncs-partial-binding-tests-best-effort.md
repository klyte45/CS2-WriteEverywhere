# [0059] Tests for WECalendarFn.cs - partial binding tests (best effort)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **to test as much of WECalendarFn as possible using binding replacement**, so that I **the date/time display logic is partially verified even without the game running**.

---

## Background

[See epic: tst-builtin-fn](..\RefsLibrary\2026040123_testing-action-plan\epics\06_Epic_builtin-fn.md)

Task BF-06. Test as much of WECalendarFn as possible using binding replacement for TimeSystem access. Date string formatting helpers if isolated. Mark RequiresGameDLL category for game-type-dependent tests.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BuiltinFn/WECalendarFnTests.cs exists
- [ ] For any method reading TimeSystem via binding field: test with fake time value
- [ ] Date string formatting (if isolated): verified with known inputs
- [ ] Tests marked [Category("RequiresGameDLL")] if they depend on game types
- [ ] If zero pure logic exists: document why in test file

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


