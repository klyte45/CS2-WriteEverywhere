# [0119] Implement Game Prop Cleanup System

**Developed by:** 

## User Story

> Acting as **a player**, I want **spawned GameProp entities to be cleaned up when their parent WE node is removed or becomes invalid**, so that I **orphaned props do not remain in the world after layout changes or save/load cycles**.

---

## Background

Doc 01 CPs 1-3. Stale detection: entities with WEChild but without WEOwner are stale (serialized across save but owner not persisted). Cleanup also runs when WEOwner.m_weOwnerEntity no longer has WETextDataMesh.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Entities with WEChild but without WEOwner (post-load stale) are destroyed during startup cleanup
- [ ] Props whose WEOwner.m_weOwnerEntity entity no longer has WETextDataMesh are destroyed
- [ ] WESubObject buffer on the GameProp text node is updated when props are destroyed
- [ ] Props survive save/load correctly when parent is still valid
- [ ] Unit tests cover post-load stale cleanup and runtime orphan detection

---

## Implementation Notes

1. New file: BelzontWE/Systems/WEGamePropCleanupSystem.cs
2. Query 1: WithAll<WEChild> WithNone<WEOwner> -> destroy (post-load stale)
3. Query 2: WithAll<WEChild, WEOwner> -> check m_weOwnerEntity has WETextDataMesh; if not, destroy and remove from WESubObject buffer

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0118]

### Is dependent for


