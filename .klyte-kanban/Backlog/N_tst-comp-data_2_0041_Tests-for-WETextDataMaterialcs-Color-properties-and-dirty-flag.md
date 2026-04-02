# [0041] Tests for WETextDataMaterial.cs - Color properties and dirty flag

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for Color and EmissiveColor setters that verify dirty-flag propagation**, so that I **dirty-flag propagation for color changes is verified (requires game DLL metadata reference)**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-02. Tests for Color and EmissiveColor setters verifying dirty-flag propagation. Requires game DLL metadata reference for UnityEngine.Color — guard with #if GAME_DLLS_AVAILABLE if needed.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Tests in WETextDataMaterialTests.cs verify: Color setter sets dirty=true, EmissiveColor setter sets dirty=true
- [ ] Tests are behind #if GAME_DLLS_AVAILABLE if game DLL not present in CI
- [ ] DEFAULT_DECAL_FLAGS == 8 contract test added

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


