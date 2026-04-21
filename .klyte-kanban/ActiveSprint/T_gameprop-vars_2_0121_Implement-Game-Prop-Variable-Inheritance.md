**End time:** 2026-04-21 18:07 -0300
**Start time:** 2026-04-21 18:04 -0300
# [0121] Implement Game Prop Variable Inheritance

**Developed by:** Claude Sonnet 4.6 <claude-sonnet-4@kwytco.com.br>
## User Story

> Acting as **a WE layout author**, I want **variables defined on a parent WE tree to be automatically available in the default prefab layout of any GameProp spawned from that tree**, so that I **dynamic templated prop layouts work without requiring duplicate variable definitions**.

---

## Background

Doc 04 v2. WEInheritedVarsCache on spawned prop stores parent inheritableVars (excludes !-local vars). Written by DrawTree GameProp case. Read by PassedCulling() before PopulateVars(). WETextDataMesh.UpdateFormulaes() needs GameProp case for prefab name eval.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] WEPreCullingSystem.PassedCulling() seeds initial variables from WEInheritedVarsCache when present (read even if disabled)
- [x] DrawTree() GameProp case writes inheritableVars (no !-local vars) to WEInheritedVarsCache on each WESubObject entity
- [x] WETextDataMesh.UpdateFormulaes() has GameProp case calling valueData.UpdateEffectiveValue
- [x] Parent non-local variables visible in spawned prop default prefab layout formulae
- [x] Parent !-local variables are NOT visible in spawned prop layout
- [x] Unit tests verify variable inheritance and local-var exclusion

---

## Implementation Notes

1. WEPreCullingSystem.cs: add m_weSubObjectLookup (readonly) and m_weInheritedVarsCacheLookup (writable) to job struct
2. PassedCulling(): before PopulateVars(entity,...), check TryGetComponent<WEInheritedVarsCache>(entity) and seed variables from it
3. DrawTree() GameProp case: after CheckForUpdates, iterate WESubObject buffer, write inheritableVars via SetComponent+SetComponentEnabled<WEInheritedVarsCache>
4. WETextDataMesh.UpdateFormulaes(): add case WESimulationTextType.GameProp: result |= valueData.UpdateEffectiveValue(em, geometryEntity, vars); break;

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0118]

### Is dependent for


