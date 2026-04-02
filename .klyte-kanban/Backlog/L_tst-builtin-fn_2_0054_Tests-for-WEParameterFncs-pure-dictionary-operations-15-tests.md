# [0054] Tests for WEParameterFn.cs - pure dictionary operations (>=15 tests)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for all WEParameterFn methods**, so that I **the template variable resolution logic is verified — used in every text item that reads from formula variables**.

---

## Background

[See epic: tst-builtin-fn](..\RefsLibrary\2026040123_testing-action-plan\epics\06_Epic_builtin-fn.md)

Task BF-01. Test all WEParameterFn methods: PrintVariables with one/multiple vars, empty dict->empty, RelVarStr1-4 found/not-found, RelVarInt1 valid/non-numeric/missing->0. Entity.Null for all tests.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BuiltinFn/WEParameterFnTests.cs exists with >=15 test methods
- [ ] Tests cover: PrintVariables with one var, multiple vars (separator confirmed), empty dict->"", RelVarStr1 key found, key not found->"", RelVarStr2-4, RelVarInt1 valid int, RelVarInt1 non-numeric->0, RelVarInt1 missing->0
- [ ] Entity parameter is Entity.Null for all tests (unused by these methods)

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


