**End time:** 2026-04-06 23:57 -0300
**Start time:** 2026-04-06 23:55 -0300
# [0091] The template usage counter is always zeroed in the UI

**Developed by:** Agent-Claude-Sonnet-4.6 (agent@example.com)
## User Story

> Acting as **a player using city templates in Write Everywhere**, I want **the template usage counter in the WE editor UI to show the correct number of entities currently using each template**, so that I **I can tell whether a template is actively in use before deciding to delete or modify it**.

---

## Background

Template usage counts are computed by `WETemplateQuerySystem` (UIUpdate phase). The system runs a parallel job `WEPlaceholderTemplateUsageCount` that iterates entities with `WEIsPlaceholder` + `WETemplateUpdater` and counts how many have a `templateEntity` GUID matching the template being queried.

The root cause of the always-zero count is a **GUID mismatch in `WETemplateManager.EntityProcessing.cs`**:

1. The registered template in `RegisteredTemplates` has GUID `X` (set at creation time).
2. When a prefab layout is spawned, `targetTemplate = targetTemplate.Clone()` is called. `Clone()` uses `XmlUtils.CloneViaXml(this)` — an XML round-trip. Because the `Guid` property is decorated with `[XmlIgnore]`, the property initializer `= System.Guid.NewGuid()` fires on deserialization, assigning a new GUID `Y` to the clone.
3. The newly created entity stores `templateEntity = Y` in its `WETemplateUpdater` buffer.
4. The usage-count job looks for `templateEntity == X` (the registered GUID). Since `X ≠ Y`, the count is always 0.

A secondary issue: the `m_templateBasedEntities` query in `WETemplateQuerySystem` may not include `ComponentType.ReadOnly<WETemplateUpdater>()` in its `All` constraint, causing the `GetBufferAccessor` call to return empty on chunks that do not contain the buffer — also zeroing the count for those chunks.

---

## Definition of Ready (DoR)

- [ ] `WETemplateManager.EntityProcessing.cs` template clone section is read — confirmed that `targetTemplate.Guid` is captured after `Clone()`, not before
- [ ] `WETextDataXmlTree.Guid` property is confirmed as `[XmlIgnore]` with `= System.Guid.NewGuid()` initializer
- [ ] `WETemplateQuerySystem` query setup is read — the `All` constraint list is checked for presence of `WETemplateUpdater`
- [ ] Confirmed `WETemplateUpdater.templateEntity` is the 'template GUID' field that the job compares against

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] After the fix, placing a prefab that uses a registered template increments that template's usage count by 1
- [ ] Deleting a spawned instance decrements the count correctly (or shows 0 once all instances are removed)
- [ ] The fix is in `WETemplateManager.EntityProcessing.cs`: original GUID is captured before the Clone() call and used in the `WETemplateUpdater` entry
- [ ] If the secondary query constraint is missing, `ComponentType.ReadOnly<WETemplateUpdater>()` is added to the `All` array in `WETemplateQuerySystem`
- [ ] No behavior change for template spawning or rendering

---

## Implementation Notes

1. In `WETemplateManager.EntityProcessing.cs`, before `targetTemplate = targetTemplate.Clone()`, capture the GUID: `var originalGuid = targetTemplate.Guid;`. Use `originalGuid` when setting `WETemplateUpdater.templateEntity`.
2. In `WETemplateQuerySystem.OnCreate`, ensure the entity query includes `ComponentType.ReadOnly<WETemplateUpdater>()` in its `All` component list to guarantee the buffer accessor returns data.
3. After the fix, validate by: creating a city template, placing 3 buildings using it, opening the template list — the count should show '3'.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


