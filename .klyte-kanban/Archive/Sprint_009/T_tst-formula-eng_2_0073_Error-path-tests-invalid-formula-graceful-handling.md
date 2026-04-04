**End time:** 2026-04-04 00:13 -0300
**Start time:** 2026-04-04 00:10 -0300
# [0073] Error path tests: invalid formula graceful handling

**Developed by:** Claude-Sonnet-4-6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **tests that verify the formulae engine's error handling**, so that I **invalid user formulae fail gracefully (display fallback text, not crash)**.

---

## Background

[See epic: tst-formula-eng](..\RefsLibrary\2026040123_testing-action-plan\epics\08_Epic_formulae-engine.md)

Task FE-06. Test formulae engine error handling: empty formula, non-existent function name, wrong argument count, null result->empty string, deeply nested/recursive formula (cycle or depth limit). Verify error fallback values from WETextDataValueString.s_config.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Tests for: empty formula string evaluation, formula referencing non-existent function, formula with wrong argument count, formula producing null result handled as empty string, deeply nested/recursive formula (cycle detection or depth limit)
- [x] Error fallback values from WETextDataValueString.s_config are verified

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


