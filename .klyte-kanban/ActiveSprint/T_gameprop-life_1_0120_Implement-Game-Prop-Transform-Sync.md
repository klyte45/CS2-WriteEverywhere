**End time:** 2026-04-21 15:19 -0300
**Start time:** 2026-04-21 15:13 -0300
# [0120] Implement Game Prop Transform Sync

**Developed by:** Claude Sonnet 4.6 <claude-sonnet-4@kwytco.com.br>
## User Story

> Acting as **a player**, I want **spawned GameProp entities to follow their parent geometry entity position and rotation every frame**, so that I **props on moving vehicles or relocated buildings remain correctly positioned**.

---

## Background

Doc 02: WEGamePropTransformSystem at PreRendering. Reads WEOwner.m_weOwnerEntity (GameProp text node) -> get WETextDataMain.targetEntity (geometry entity) -> compose transforms -> write to spawned prop LocalTransform.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Spawned prop LocalTransform updates every frame to match parent geometry entity + WE node offset
- [x] Transform sync works for static buildings, road segment nodes, and vehicles
- [x] System uses IJobChunk and is Burst-compiled
- [x] Graceful skip when parent geometry entity does not exist (no error)

---

## Implementation Notes

1. New file: BelzontWE/Systems/WEGamePropTransformSystem.cs, UpdateGroup=PreRenderingGroup
2. Query: WithAll<WEOwner, LocalTransform> on spawned prop entities
3. Compose: geometry LocalTransform * WETextDataTransform.offsetPosition/offsetRotation -> write to prop LocalTransform

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0118]

### Is dependent for


