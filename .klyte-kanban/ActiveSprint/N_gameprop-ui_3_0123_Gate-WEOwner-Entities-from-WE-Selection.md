# [0123] Gate WEOwner Entities from WE Selection

**Developed by:** 

## User Story

> Acting as **a player using the WE editor**, I want **spawned GameProp prop entities to be invisible to the WE picker and hierarchy tree**, so that I **I cannot accidentally select and edit spawned props as if they were regular WE layout targets**.

---

## Background

Doc 04 section 9. Spawned prop entities have WEOwner component. WEWorldPickerSystem and WEWorldPickerTooltip must exclude them. Hierarchy tree should not list them.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WEWorldPickerSystem entity query excludes entities with WEOwner component
- [ ] WEWorldPickerTooltip entity query excludes entities with WEOwner component
- [ ] Spawned GameProp props cannot be selected as WE editing targets in-game
- [ ] All existing world-picker behavior for non-WEOwner entities is unchanged

---

## Implementation Notes

1. Add .WithNone<WEOwner>() to all ECS queries in WEWorldPickerSystem that enumerate selectable WE geometry entities
2. Add .WithNone<WEOwner>() to WEWorldPickerTooltip entity queries
3. Verify hierarchy view does not show WEOwner entities (may require separate filter in WETextHierarchyView)

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0118]

### Is dependent for


