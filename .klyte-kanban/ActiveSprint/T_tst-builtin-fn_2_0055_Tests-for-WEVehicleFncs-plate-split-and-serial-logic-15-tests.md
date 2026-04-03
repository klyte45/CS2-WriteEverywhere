**End time:** 2026-04-02 22:29 -0300
**Start time:** 2026-04-02 22:27 -0300
# [0055] Tests for WEVehicleFn.cs - plate split and serial logic (>=15 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the vehicle plate and serial number logic**, so that I **the mod's vehicle identification display is verified correct**.

---

## Background

[See epic: tst-builtin-fn](..\RefsLibrary\2026040123_testing-action-plan\epics\06_Epic_builtin-fn.md)

Task BF-02. Test vehicle plate split (Line1=first half, Line2=second half, odd-length), GetSerialNumber (entity.Index % 100000 padded 5 digits, wraparound), null-fallback when binding returns null/empty.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] BuiltinFn/WEVehicleFnTests.cs exists with >=15 test methods
- [x] Tests cover: GetVehiclePlateLine1 splits 8-char plate at midpoint, GetVehiclePlateLine2 returns second half, odd-length plates split correctly, GetSerialNumber returns entity.Index % 100000 padded to 5 digits, serial wraps >100000, null-fallback graceful handling
- [x] [TearDown] restores all modified bindings

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


