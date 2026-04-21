**End time:** 2026-04-21 15:02 -0300
**Start time:** 2026-04-21 12:18 -0300
# [0118] Implement Game Prop Spawn System

**Developed by:** Claude Sonnet 4.5 (claude-sonnet-4-5@kwytco.com.br)
## User Story

> Acting as **a player**, I want **GameProp WE nodes to spawn real CS2 prop entities in the world positioned relative to the parent geometry entity**, so that I **WE layouts can include 3D props as part of the information display**.

---

## Background

Doc 02: WEGamePropSpawnSystem at ModificationEnd. Rate-limited. Depth guard max 4. Cycle guard per prefab-name chain. Uses WEGamePropIndex for lookup. Spawned entity gets WEOwner, WEChild, WEInheritedVarsCache (empty), Game.Objects.Secondary. WESubObject buffer on text node updated. Array instancing same C# limits as Placeholder.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Spawn system creates prop entity when GameProp WE node has valid prefab name in WEGamePropIndex
- [x] Spawned prop has WEOwner+WEChild+WEInheritedVarsCache+Game.Objects.Secondary components
- [x] WESubObject buffer on GameProp text node references the spawned prop entity
- [x] Depth guard: spawning blocked beyond depth 4, logged but does not throw
- [x] Cycle guard: spawning blocked if same prefab name already in current chain
- [x] Rate limiting caps dirty-node processing per frame
- [x] Array instancing spawns correct count of props with appropriate transforms
- [x] Unit tests cover: basic spawn, WEOwner assignment, depth guard, cycle guard

---

## Implementation Notes

1. New file: BelzontWE/Systems/WEGamePropSpawnSystem.cs, UpdateGroup=ModificationEndGroup
2. Check ValueData.EffectiveValue in WEGamePropIndex; skip if not found
3. Position via parent geometry entity LocalTransform + WETextDataTransform on the GameProp node
4. For instancing: iterate WETextDataTransform.ArrayInstancing counts, apply per-instance offset

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0117]

### Is dependent for


