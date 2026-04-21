# [0125] gameprop-prefab-searchable-dropdown-frontend

**Developed by:** 

## User Story

> Acting as **a WE editor user**, I want **a searchable dropdown for the prefab name field when I select GameProp as content type**, so that I **I can find and select the correct prop by typing either its prefab name or localized name**.

---

## Background

PopupSearchField is available at 'game-ui/editor/widgets/search-field/popup-search-field.tsx' export 'PopupSearchField'. Its props: {value, suggestions:{value,favorite}[], onChange, valueIsFavorite?, onChangeFavorite?, uiTag?}. VanillaWidgets.tsx uses getModule(path, export) pattern with registryIndex. WorldPickerService uses engine.call to fetch entries as {prefabName, localizedName}[]. The GameProp block in WETextValueSettings.tsx has FormulaeEditRow with defaultInputField - that defaultInputField should become PopupSearchField.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] PopupSearchField added to VanillaWidgets with correct type definition and registryIndex entry
- [ ] WorldPickerService exposes getGamePropEntries() async method calling dataMesh.getGamePropEntries
- [ ] GameProp block in WETextValueSettings uses PopupSearchField inside FormulaeEditRow as defaultInputField
- [ ] Typing in the field filters suggestions matching either prefabName or localizedName
- [ ] Selecting a suggestion stores the prefabName in mesh.ValueText
- [ ] Frontend build succeeds with no TS errors

---

## Implementation Notes

1. VanillaWidgets.tsx: add PropsPopupSearchField type, add PopupSearchField entry to registryIndex with path 'game-ui/editor/widgets/search-field/popup-search-field.tsx', add public getter PopupSearchField
2. WorldPickerService.tsx: add static getGamePropEntries(): Promise<{prefabName:string,localizedName:string}[]> calling engine.call('k45::we.dataMesh.getGamePropEntries')
3. WETextValueSettings.tsx: add gamePropEntries state, fetch on mount. Replace StringInputField in GameProp FormulaeEditRow with PopupSearchField: value=mesh.ValueText.value, suggestions filtered by mesh.ValueText.value matching either field, onChange stores prefabName

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0124]

### Is dependent for


