# [0050] Tests for IO/WEComponentTypeDesc.cs (component type classifier)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the From(Type) factory that classifies types as buffer vs non-buffer**, so that I **the formulae UI doesn't mis-classify component types**.

---

## Background

[See epic: tst-io-xml](..\RefsLibrary\2026040123_testing-action-plan\epics\05_Epic_io-xml.md)

Task IX-04. Test From(Type) factory: non-buffer classification for int, IBufferElementData classification as buffer=true. Guard with #if GAME_DLLS_AVAILABLE if needed.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] IO/WEComponentTypeDescTests.cs exists with >=5 tests
- [ ] Tests cover: From(typeof(int)) -> isBuffer==false, returnClassName=="System.Int32", From(typeof(SomeIBufferElementData)) -> isBuffer==true, WEDescType=="COMPONENT"
- [ ] Tests guarded with #if GAME_DLLS_AVAILABLE if Unity.Entities.dll not available in CI

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


