# [0056] Tests for WEBuildingFn.cs - binding null-fallback contracts

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the building formula functions**, so that I **null-binding fallbacks are verified (preventing NullReferenceExceptions when the binding isn't initialized)**.

---

## Background

[See epic: tst-builtin-fn](..\RefsLibrary\2026040123_testing-action-plan\epics\06_Epic_builtin-fn.md)

Task BF-03. Test building formula null-fallbacks: when binding returns real Entity, Entity.Null, or binding is null -> method returns Entity.Null (not throws) for GetBuildingRoad, GetBuildingRoadNumber, GetBuildingMainRenter.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BuiltinFn/WEBuildingFnTests.cs exists with >=8 test methods
- [ ] Tests cover: GetBuildingRoad when binding returns real Entity, when binding returns Entity.Null, when binding is null -> returns Entity.Null (not throws)
- [ ] Same for GetBuildingRoadNumber and GetBuildingMainRenter
- [ ] [TearDown] restores all bindings

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


