**End time:** 2026-04-21 20:00 -0300
**Start time:** 2026-04-21 19:54 -0300
# [0124] expose-gameprop-entries-call-binding

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4@kwytco.com.br)
## User Story

> Acting as **a WE editor user**, I want **the frontend to retrieve the list of eligible game prop prefabs with their localized names**, so that I **a searchable dropdown can be displayed when configuring a GameProp text node**.

---

## Background

WETemplateManager.WEGamePropIndex already contains Dictionary<string,Entity> of eligible props. We need to expose it to the frontend via a call-binding that returns {prefabName, localizedName}[] sorted by localizedName. NameSystem is used to translate entity names. CallBinder is available in DoInitValueBindings of WETextDataMeshController.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] WETextDataMeshController exposes call-binding 'dataMesh.getGamePropEntries' returning GamePropEntry[]
- [x] Each GamePropEntry has both prefabName and localizedName fields populated
- [x] Entries are sorted by localizedName (case-insensitive)
- [x] Returns empty array (not null) when WEGamePropIndex is empty
- [x] Unit tests verify structure and sort order

---

## Implementation Notes

1. Add m_nameSystem: NameSystem to WETextDataMeshController, obtain in OnCreate
2. Add GamePropEntry struct with public string prefabName and public string localizedName in namespace BelzontWE
3. In DoInitValueBindings add: CallBinder($"{PREFIX}getGamePropEntries", GetGamePropEntries)
4. Add GetGamePropEntries() iterating WETemplateManager.Instance.WEGamePropIndex, using m_nameSystem.GetName(entity).Translate() for localizedName, sorted by localizedName

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for

- [0125]
