# [0117] Populate WE Game Prop Prefab Index

**Developed by:** 

## User Story

> Acting as **the spawn system**, I want **a fast O(1) lookup from prefab name to Entity for eligible static-object prefabs**, so that I **prop spawning does not stall gameplay with expensive PrefabSystem lookups**.

---

## Background

Doc 02 sections 1.2-1.4. WEGamePropIndex = Dictionary<string, Entity> populated in same coroutine as PrefabNameToIndex. Eligibility: HasComponent<StaticObjectData> && !HasComponent<BuildingData> && !HasComponent<BuildingExtensionData>.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WEGamePropIndex property exists on WETemplateManager and is populated at game load
- [ ] Eligibility filter correctly includes StaticObjectPrefab-derived props and excludes buildings, extensions, vehicles
- [ ] Index populated in same coroutine pass as PrefabNameToIndex (single pass, no separate system)
- [ ] WEGamePropIndex rebuilt when SpritesAndLayoutsDataVersion changes (same trigger as PrefabNameToIndex)
- [ ] Unit tests verify eligible props appear and ineligible ones do not

---

## Implementation Notes

1. WETemplateManager.PrefabLayout.cs: add public Dictionary<string, Entity> WEGamePropIndex
2. In UpdatePrefabIndexDictionary_Coroutine: check eligibility via EntityManager component queries
3. Eligibility: entity has StaticObjectData AND does NOT have BuildingData AND does NOT have BuildingExtensionData

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0116]

### Is dependent for


