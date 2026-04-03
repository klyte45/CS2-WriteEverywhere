**End time:** 2026-04-03 16:04 -0300
**Start time:** 2026-04-03 16:01 -0300
# [0057] Tests for WERouteFn.cs - waypoint and transport line logic

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the route formula functions**, so that I **transport line number display and waypoint resolution are verified**.

---

## Background

[See epic: tst-builtin-fn](..\RefsLibrary\2026040123_testing-action-plan\epics\06_Epic_builtin-fn.md)

Task BF-04. Test route formula functions: GetTransportLineNumber binding replacement, GetWaypointStaticDestinationName null-fallback, binding-null -> empty string (not throws), GetNthWaypoint with pre-seeded vars dict.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] BuiltinFn/WERouteFnTests.cs exists with >=10 test methods
- [x] Tests cover: GetTransportLineNumber binding replacement, GetWaypointStaticDestinationName null-fallback, binding-null -> method returns empty string (not throws), GetNthWaypoint vars-dict path with pre-seeded dict
- [x] [TearDown] restores all bindings

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


