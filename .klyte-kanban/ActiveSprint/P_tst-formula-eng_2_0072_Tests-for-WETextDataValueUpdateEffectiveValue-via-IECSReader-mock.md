**Start time:** 2026-04-04 00:03 -0300
# [0072] Tests for WETextDataValue*.UpdateEffectiveValue via IECSReader mock

**Developed by:** Claude-Sonnet-4-6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **tests for UpdateEffectiveValue with a mocked IECSReader**, so that I **the formula-evaluation-to-component-update path is verified**.

---

## Background

[See epic: tst-formula-eng](..\RefsLibrary\2026040123_testing-action-plan\epics\08_Epic_formulae-engine.md)

Task FE-05. Add >=6 tests each to WETextDataValueFloat/Int/String for UpdateEffectiveValue: mock returns known component data, EffectiveValue updated, InitializedEffectiveText=true. Cover: no formula->DefaultValue, valid formula->evaluated value, invalid formula->fallback. Prerequisite: SR-04.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WETextDataValueFloatTests.cs, WETextDataValueIntTests.cs, WETextDataValueStringTests.cs each gain >=6 tests for UpdateEffectiveValue
- [ ] Tests use NSubstitute mock of IECSReader: mock returns known component data, call UpdateEffectiveValue, verify EffectiveValue updated and InitializedEffectiveText=true
- [ ] Tests cover: no formula->uses DefaultValue, valid formula->evaluated value, invalid/unknown formula->fallback value

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


