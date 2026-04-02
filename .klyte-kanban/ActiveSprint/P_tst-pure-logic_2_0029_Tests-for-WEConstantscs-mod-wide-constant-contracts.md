**Start time:** 2026-04-02 02:52 -0300
# [0029] Tests for WEConstants.cs (mod-wide constant contracts)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests that pin the specific numeric and character values of mod-wide constants**, so that I **accidental modification is caught before it silently breaks parsing or rendering behavior**.

---

## Background

[See epic: tst-pure-logic](..\RefsLibrary\2026040123_testing-action-plan\epics\02_Epic_pure-logic.md)

Task PL-05. Pin specific numeric and character values of WEConstants so accidental modification is caught before it silently breaks parsing or rendering.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Utils/WEConstantsTests.cs exists
- [ ] Tests verify: VARIABLE_ITEM_SEPARATOR != VARIABLE_KV_SEPARATOR, MAX_ATLAS_SIZE == 8192, RENDERER_FRAME_CHECK_MASK is a valid bitmask (power-of-two minus one), separator chars are correct ASCII values

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


