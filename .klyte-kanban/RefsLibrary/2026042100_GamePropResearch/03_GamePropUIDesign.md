# Game Prop Feature — UI Design

**Date:** 2026-04-21  
**Related:** `01_GamePropFeatureRequirementsAndCriticalPoints.md`, `02_GamePropDependenciesAndRequestPath.md`

---

## 1. Panel Visibility Matrix

The WE tool panel uses a flag `isMatrixTransform` to hide certain panels when editing a `MatrixTransform` node. `GameProp` requires a similar flag.

**Current flags:**
```typescript
const isMatrixTransform = mesh.TextSourceType.value == WESimulationTextType.MatrixTransform;
```

**New flags to add:**
```typescript
const isGameProp = mesh.TextSourceType.value == WESimulationTextType.GameProp;
```

### Panel visibility table

| Panel / Control | Text | Image | Placeholder | WhiteTexture | MatrixTransform | WhiteCube | **GameProp** |
|---|---|---|---|---|---|---|---|
| Mouse Precision | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Editing Plane | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Pivot | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Position | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Rotation | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| **Appearance button** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | **❌** |
| **Shader button** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | **❌** |
| Variables button | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Instancing button** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | **✅ (same rules as Placeholder)** |
| WETextValueSettings | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| WETextHierarchyView | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| FormulaeEditor (when active) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

**Legend:**
- ✅ = Shown
- ❌ = Hidden

---

## 2. Required Changes to `WriteEverywhereToolOptions.tsx`

### 2.1 New `isGameProp` flag

```diff
 const isMatrixTransform = WorldPickerService.instance.bindingList.mesh.TextSourceType.value == WESimulationTextType.MatrixTransform;
+const isGameProp = WorldPickerService.instance.bindingList.mesh.TextSourceType.value == WESimulationTextType.GameProp;
```

### 2.2 Appearance and Shader buttons gated by `isGameProp`

**Current (`WriteEverywhereToolOptions.tsx` ~line 345):**
```tsx
{!isMatrixTransform && <>
    <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayShaderWindow(!displayShaderWindow)} selected={displayShaderWindow} src={i_ShaderBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_ShaderBtn} />
    <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayAppearenceWindow(!displayAppearenceWindow)} selected={displayAppearenceWindow} src={i_AppearenceBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_AppearenceBtn} />
    <div style={{ width: "10rem" }}></div>
</>}
```

**New:**
```tsx
{!isMatrixTransform && !isGameProp && <>
    <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayShaderWindow(!displayShaderWindow)} selected={displayShaderWindow} src={i_ShaderBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_ShaderBtn} />
    <VanillaComponentResolver.instance.ToolButton onSelect={() => setDisplayAppearenceWindow(!displayAppearenceWindow)} selected={displayAppearenceWindow} src={i_AppearenceBtnIcon} focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} className={VanillaComponentResolver.instance.toolButtonTheme.button} tooltip={T_AppearenceBtn} />
    <div style={{ width: "10rem" }}></div>
</>}
```

### 2.3 Appearance and Shader window rendering gated by `isGameProp`

**Current (`WriteEverywhereToolOptions.tsx` ~line 371):**
```tsx
{currentItemIsValid && !isMatrixTransform && displayAppearenceWindow && <WETextAppearenceSettings />}
{currentItemIsValid && !isMatrixTransform && displayShaderWindow && <WETextShaderProperties />}
```

**New:**
```tsx
{currentItemIsValid && !isMatrixTransform && !isGameProp && displayAppearenceWindow && <WETextAppearenceSettings />}
{currentItemIsValid && !isMatrixTransform && !isGameProp && displayShaderWindow && <WETextShaderProperties />}
```

---

## 3. Required Changes to `WETextValueSettings.tsx`

### 3.1 Content Type Dropdown — add GameProp (7)

**Current (`WETextValueSettings.tsx` ~line 145):**
```tsx
items={[0, 1, 2, 4, 5, 6].map(x => { return { displayName: { __Type: LocElementType.String, value: translate(`textValueSettings.contentType.${x}`) }, value: x } })}
```

**New:**
```tsx
items={[0, 1, 2, 4, 5, 6, 7].map(x => { return { displayName: { __Type: LocElementType.String, value: translate(`textValueSettings.contentType.${x}`) }, value: x } })}
```

> Value `3` (Archetype) remains hidden. Value `7` is GameProp.

### 3.2 GameProp Content Section

Add after the `Placeholder` block (`WETextValueSettings.tsx` ~line 224), modelled on the `Text` type's `FormulaeEditRow` (supports formulae on the prefab name field):

```tsx
{mesh.TextSourceType.value == WESimulationTextType.GameProp &&
    <>
        <FormulaeEditRow formulaeField={valueFormulaeField} formulaeModule={valueFormulaeModule} label={L_gamePropName}
            defaultInputField={<StringInputField
                value={fixedTextTyping}
                onChange={(x) => { setFixedTextTyping(x) }}
                onChangeEnd={() => {
                    mesh.ValueText.set(fixedTextTyping.trim());
                    mesh.ValueTextFormulaeStr.set("");
                }}
                maxLength={400}
            />} />
    </>}
```

Where `L_gamePropName = translate("textValueSettings.gamePropName")` is declared alongside the other label constants at the top of the component.

### 3.3 Flip-Z toggle — hide for GameProp

**Current (`WETextValueSettings.tsx` ~line 233):**
```tsx
{mesh.TextSourceType.value != WESimulationTextType.MatrixTransform && <>
    <ToggleField label={T_flipZ} value={transform.CurrentScale.value[2] < 0} onChange={(x) => transform.CurrentScale.set([transform.CurrentScale.value[0], transform.CurrentScale.value[1], -transform.CurrentScale.value[2]])} />
</>}
```

**New:**
```tsx
{mesh.TextSourceType.value != WESimulationTextType.MatrixTransform
  && mesh.TextSourceType.value != WESimulationTextType.GameProp && <>
    <ToggleField label={T_flipZ} value={transform.CurrentScale.value[2] < 0} onChange={(x) => transform.CurrentScale.set([transform.CurrentScale.value[0], transform.CurrentScale.value[1], -transform.CurrentScale.value[2]])} />
</>}
```

---

## 4. Hierarchy View — Tree Node Icon

The `WETextHierarchyView` displays a per-type icon in the tree list using `getIconForTextType(type)`. Currently (`WETextHierarchyView.tsx` ~line 35 + 157):

```typescript
const i_typeText = "coui://uil/Standard/PencilPaper.svg";
const i_typeImage = "coui://uil/Standard/Image.svg";
const i_typePlaceholder = "coui://uil/Standard/RotateAngleRelative.svg";
const i_typeWhiteTexture = "coui://uil/Standard/SingleRhombus.svg";
const i_typeWhiteCube = "coui://uil/Standard/BoxSide.svg";
const i_typeMatrixTransform = "coui://uil/Standard/ArrowsMoveAll.svg";

function getIconForTextType(type: WESimulationTextType) {
    switch (type) {
        case WESimulationTextType.Image: return i_typeImage;
        case WESimulationTextType.Text: return i_typeText;
        case WESimulationTextType.Placeholder: return i_typePlaceholder;
        case WESimulationTextType.WhiteTexture: return i_typeWhiteTexture;
        case WESimulationTextType.MatrixTransform: return i_typeMatrixTransform;
        case WESimulationTextType.WhiteCube: return i_typeWhiteCube;
    }
}
```

**New constant and case to add:**
```typescript
// Add constant alongside the others:
const i_typeGameProp = "coui://uil/Standard/BenchAndParkProps.svg";

// Add case inside getIconForTextType():
case WESimulationTextType.GameProp: return i_typeGameProp;
```

**Chosen icon:** `"coui://uil/Standard/BenchAndParkProps.svg"` ✅ (confirmed by user review).

---

## 5. GameProp-Specific Constraints

### 5.1 Instancing Panel — Same Rules as Placeholder

The instancing panel has two independent features:
1. **"Show When" visibility condition** (MustDrawFn formula) — always available, including for GameProp
2. **Array instancing in X/Y/Z** — available for GameProp with the **same limits as Placeholder type** (enforced on C# side)

When array instancing is enabled for a GameProp node, each instance spawns a separate prop entity. The depth and cycle guards still apply per-instance chain.

> **C# note:** Must add `WESimulationTextType.GameProp` alongside `Placeholder` in the C# condition that gates XYZ instancing.

### 5.2 Formula Support for Prefab Name

The prefab name field supports WE formula strings (same as `ValueText` for Text type). This enables dynamic prop selection via formula evaluation.

- `ValueData.DefaultValue` = static prefab name fallback
- `ValueData.Formulae` = formula string to evaluate a string result = the prefab name to look up
- `ValueData.EffectiveValue` = computed result used for the lookup

Spawn triggers when `EffectiveValue` changes (same dirty-detection logic as other formulae fields).

### 5.3 Children WE Nodes Ignored

Like `Placeholder` type, **children WE nodes attached under a GameProp node are ignored** (not evaluated/rendered). This avoids complexity from WE layouts on spawned prop entities interacting with the parent WE node system.

> **Implementation note:** In the rendering/update pipeline, wherever `Placeholder` children are skipped, add `GameProp` to the same guard.

---

## 6. Open Question for Review

| # | Question | Options |
|---|----------|---------|
| 6.1 | **Icon for GameProp in hierarchy view?** | **Resolved:** `BenchAndParkProps.svg` (`coui://uil/Standard/BenchAndParkProps.svg`) ✅ |

---

## 7. Required i18n Entries

New keys to add to `BelzontWE/i18n/i18n.csv`:

| Key | English | Portuguese (pt-BR) |
|-----|---------|-------------------|
| `K45::WE.vuio[textValueSettings.contentType.7]` | Game Prop | Objeto do Jogo |
| `K45::WE.vuio[textValueSettings.gamePropName]` | Prefab name | Nome do prefab |
| `K45::WE.vuio[formulaeTitleName.mesh.7.ValueText]` | Game prop name | Nome do objeto do jogo |

---

## 8. `WESimulationTextType` Frontend Enum Update

The TypeScript enum in `WEFormulaeElement.ts` (or equivalent service file) must be updated to include the new value:

```typescript
export enum WESimulationTextType {
    Text = 0,
    Image = 1,
    Placeholder = 2,
    // 3 = Archetype (not shown in UI)
    WhiteTexture = 4,
    MatrixTransform = 5,
    WhiteCube = 6,
    GameProp = 7,  // NEW
}
```

---

## 9. Summary of Frontend Files to Modify

| File | Change |
|------|--------|
| `WriteEverywhereToolOptions.tsx` | Add `isGameProp` flag; gate Appearance + Shader buttons/windows |
| `WETextValueSettings.tsx` | Add `7` to content type dropdown; add GameProp content block; gate Flip-Z |
| `WETextHierarchyView.tsx` | Add `GameProp` case to icon-selector function |
| `WEFormulaeElement.ts` (or service file) | Add `GameProp = 7` to `WESimulationTextType` enum |
| `i18n/i18n.csv` | Add 3 new keys (see §7) |
