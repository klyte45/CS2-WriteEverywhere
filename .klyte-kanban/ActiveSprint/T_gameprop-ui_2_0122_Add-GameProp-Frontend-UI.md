**End time:** 2026-04-21 19:12 -0300
**Start time:** 2026-04-21 18:13 -0300
# [0122] Add GameProp Frontend UI

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4@kwytco.com.br)
## User Story

> Acting as **a player using the WE editor**, I want **to select GameProp as a content type, enter a prefab name with formula support, and see correct UI without irrelevant panels**, so that I **I can place and configure prop-spawning WE nodes intuitively**.

---

## Background

Doc 03 (all TSX changes + i18n). WEFormulaeElement.ts enum update. WETextValueSettings: dropdown + GameProp block + gate Flip-Z. WriteEverywhereToolOptions: isGameProp flag + gate Shader+Appearance. WETextHierarchyView: BenchAndParkProps.svg icon. i18n: 3 keys.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] WESimulationTextType.GameProp=7 added to TS enum in WEFormulaeElement.ts (or equivalent service file)
- [x] Content type dropdown includes GameProp(7), Archetype(3) still hidden
- [x] GameProp content block shows prefab name field with FormulaeEditRow (formula support; same pattern as Text type)
- [x] Appearance and Shader buttons and windows hidden when isGameProp=true
- [x] Flip-Z toggle hidden for GameProp nodes (alongside MatrixTransform exclusion)
- [x] BenchAndParkProps.svg icon shown for GameProp nodes in WE hierarchy tree
- [x] All 3 i18n keys added to i18n.csv (English + pt-BR) following project convention

---

## Implementation Notes

1. WriteEverywhereToolOptions.tsx: add const isGameProp = ...mesh.TextSourceType.value == WESimulationTextType.GameProp after isMatrixTransform line
2. Gate shader/appearance buttons: change {!isMatrixTransform && <> ... </>} to {!isMatrixTransform && !isGameProp && <> ... </>}
3. WETextValueSettings.tsx: add 7 to items array, add GameProp block with FormulaeEditRow (valueFormulaeField+valueFormulaeModule), extend flip-Z guard
4. WETextHierarchyView.tsx: add const i_typeGameProp = coui://uil/Standard/BenchAndParkProps.svg and case in getIconForTextType
5. i18n keys: K45::WE.vuio[textValueSettings.contentType.7], K45::WE.vuio[textValueSettings.gamePropName], K45::WE.vuio[formulaeTitleName.mesh.7.ValueText]

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0121]

### Is dependent for


