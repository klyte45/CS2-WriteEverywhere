**End time:** 2026-04-03 16:11 -0300
**Start time:** 2026-04-03 16:04 -0300
# [0058] WENumberFormattingFn.cs: add locale seam + formatting tests (>=15 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the numeric formatting methods**, so that I **the mod's thousands/millions display logic is verified independent of the game's locale state**.

---

## Background

[See epic: tst-builtin-fn](..\RefsLibrary\2026040123_testing-action-plan\epics\06_Epic_builtin-fn.md)

Task BF-05. Introduce static Func<CultureInfo> FormatCulture_override field (only production change needed). Test To4DigitsValue, To3DigitsValue, millions range, overflow to infinity, integer overloads match float.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] static Func<CultureInfo> FormatCulture_override field added to WENumberFormattingFn (defaults to real culture)
- [x] BuiltinFn/WENumberFormattingFnTests.cs exists with >=15 tests
- [x] Tests cover: To4DigitsValue(1234f)=="1234", To4DigitsValue(12345f)=="12.3k", To3DigitsValue(999f)=="999", To3DigitsValue(1001f)=="1.0k", millions range, overflow, negative values, integer overloads match float
- [x] [SetUp] sets FormatCulture_override to InvariantCulture; [TearDown] restores

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


