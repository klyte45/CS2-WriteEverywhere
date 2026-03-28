# Prompt Template: Add New Field to WETextDataMaterial

Use this prompt as a base when registering a new field on `WETextDataMaterial` and propagating it through all mod layers.
Fill in the `[PLACEHOLDERS]` before using.

---

## Prompt

I need to add a new field called `[FIELD_NAME]` of type `[FIELD_TYPE]` to `WETextDataMaterial` in the BelzontWE mod (Cities: Skylines II).

### Field specification
- **C# name**: `[FIELD_NAME]` (e.g. `UseGlobalLight`, `AffectSmoothness`)
- **C# type**: `[FIELD_TYPE]` – one of:
  - `bool` → simple boolean toggle (follow `AffectSmoothness` / `UseGlobalLight` pattern)
  - `float` with formulae support → follow `EmissiveIntensity` pattern
  - `Color` with formulae support → follow `EmissiveColor` / `MainColor` pattern
- **Default value**: `[DEFAULT_VALUE]` (e.g. `false`, `0`, `Color.white`)
- **Valid range** (float only): min=`[MIN]`, max=`[MAX]`
- **Which XML class(es) it belongs to**: `[DefaultStyleXml | DecalStyleXml | GlassStyleXml]`
  - Choose based on which `WEShader` modes the field affects:
    - `WEShader.Default` → `DefaultStyleXml`
    - `WEShader.Decal` → `DecalStyleXml`
    - `WEShader.Glass` → `GlassStyleXml`
- **Localization key suffix**: `[LOC_KEY]` (e.g. `useGlobalLight`, `affectSmoothness`, `EmissiveIntensity`)
- **English label**: `[EN_LABEL]`
- **Portuguese label**: `[PT_LABEL]`
- **Chinese (zh-HANT) label**: `[ZH_LABEL]`
- **Material shader property name** (if this sets a value on the Unity Material): `[SHADER_PROP]` or `none`
- **UI panel condition** (shader type int for the `[x].includes(...)` check): `[UI_SHADER_INT]`
  - 0 = Default, 1 = Glass, 2 = Decal
- **Description of what the field does**: `[DESCRIPTION]`

### Implementation checklist

Implement the field in ALL of the following locations. For each, follow exactly the pattern of the reference field:

#### 1. `BelzontWE/Components/WETextData/WETextDataMaterial.cs`

**For bool fields (follow `AffectSmoothness` / `UseGlobalLight`):**
- Add private backing field: `private bool [fieldNameCamelCase];`
- Add public property: `public bool [FieldName] { readonly get => [fieldNameCamelCase]; set { [fieldNameCamelCase] = value; dirty = true; } }`
- In `UpdateDefaultMaterial` / `UpdateDecalMaterial` / `UpdateGlassMaterial` (appropriate method):
  - If it sets a shader property: `material.SetFloat("[SHADER_PROP]", [FieldName] ? 1 : 0);`

**For float fields with formulae (follow `EmissiveIntensity`):**
- Add: `private WETextDataValueFloat [fieldNameCamelCase];`
- Add property: `public float [FieldName] { readonly get => [fieldNameCamelCase].defaultValue; set { [fieldNameCamelCase].defaultValue = math.clamp(value, [MIN], [MAX]); } }`
- Add formulae accessor: `public string [FieldName]Formulae => [fieldNameCamelCase].Formulae;`
- Add effective value accessor: `public readonly float [FieldName]Effective => [fieldNameCamelCase].EffectiveValue;`
- Add formulae setter: `public int SetFormulae[FieldName](string value, out string[] cmpErr) => [fieldNameCamelCase].SetFormulae(value, out cmpErr);`
- In `UpdateFormulaes`: add `| [fieldNameCamelCase].UpdateEffectiveValue(em, geometryEntity, vars)`
- In the appropriate `Update*Material` method: `material.SetFloat("[SHADER_PROP]", [fieldNameCamelCase].EffectiveValue);`

**XML conversion (both bool and float fields):**
- In `ToDefaultXml()` / `ToDecalXml()` / `ToGlassXml()` (matching XML class): add `[fieldNameCamelCase] = [fieldNameCamelCase],`
- In `ToComponent(WETextDataXml.[XmlClassName] value)`: add `[fieldNameCamelCase] = value.[fieldNameCamelCase],`

**Default value in `CreateDefault()`** (if the field should have a non-zero default):
- Add `[fieldNameCamelCase] = new() { defaultValue = [DEFAULT_VALUE] },` (for float)
- For bool, the default is `false` (struct zero-init), no change needed unless default is `true`

---

#### 2. `BelzontWE/IO/WETextDataXml.cs` — appropriate XML class

Bump `CURRENT_VERSION` by 1 (e.g. from `N` to `N+1`).

**For bool fields:**
- Add: `[XmlAttribute][DefaultValue(false)] public bool [fieldNameCamelCase] = false;`
- In `Serialize`: add `writer.Write([fieldNameCamelCase]);`
- In `Deserialize`: add version guard at the end:
  ```csharp
  if (version >= [NEW_VERSION])
  {
      reader.Read(out [fieldNameCamelCase]);
  }
  else
  {
      [fieldNameCamelCase] = [DEFAULT_VALUE];
  }
  ```

**For float fields:**
- Add: `[XmlElement] public FormulaeFloatXml [fieldNameCamelCase];`
- In `Serialize`: add `writer.WriteNullCheck([fieldNameCamelCase]);`
- In `Deserialize`: add in appropriate version block: `reader.ReadNullCheck(out [fieldNameCamelCase]);`

---

#### 3. `BelzontWE/Controllers/WETextDataMaterialController.cs`

**For bool fields (follow `AffectSmoothness` / `UseGlobalLight`):**
- Add property declaration: `public MultiUIValueBinding<bool> [FieldName] { get; private set; }`
- In `DoInitValueBindings` (with other bool inits):
  `[FieldName] = new(default, $"{PREFIX}{nameof([FieldName])}", EventCaller, CallBinder);`
- In `DoInitValueBindings` (with other change handlers):
  `[FieldName].OnScreenValueChanged += (x) => PickerController.EnqueueModification<bool, WETextDataMaterial>(x, (x, currentItem) => { currentItem.[FieldName] = x; return currentItem; });`
- In `OnCurrentItemChanged`:
  `[FieldName].Value = material.[FieldName];`

**For float fields with formulae (follow `EmissiveIntensity`):**
- Add 4 property declarations: `MultiUIValueBinding<float> [FieldName]`, `MultiUIValueBinding<string> [FieldName]FormulaeStr`, `MultiUIValueBinding<int> [FieldName]FormulaeCompileResult`, `MultiUIValueBinding<string[]> [FieldName]FormulaeCompileResultErrorArgs`
- Initialize all 4 bindings in `DoInitValueBindings`
- Add change handler for the value binding
- Add `SetupOnFormulaeChangedAction(...)` call for the formulae binding
- In `OnCurrentItemChanged`: add `[FieldName].Value = material.[FieldName];` and `ResetScreenFormulaeValue(...)` call

---

#### 4. Frontend: `_Frontends/UI/k45-we-vuio/src/services/WorldPickerService.tsx`

In the `WETextDataMaterialController` type declaration, add:

**For bool:**
```typescript
[FieldName]: MultiUIValueBinding<boolean>,
```

**For float:**
```typescript
[FieldName]: MultiUIValueBinding<number>,
[FieldName]FormulaeStr: MultiUIValueBinding<string>,
[FieldName]FormulaeCompileResult: MultiUIValueBinding<number>,
[FieldName]FormulaeCompileResultErrorArgs: MultiUIValueBinding<string[]>,
```

Also, for float fields with formulae, add the key to `FormulableMaterialKeys` type union.

---

#### 5. Frontend: `_Frontends/UI/k45-we-vuio/src/toolOptions/WETextAppearenceSettings.tsx`

Add translation key constant near the others:
```typescript
const T_[fieldNameCamelCase] = translate("appearenceSettings.[LOC_KEY]");
```

Add the UI element inside the correct shader condition block (`[0]` = Default, `[2]` = Decal, `material.ShaderType.value == 1` = Glass):

**For bool:**
```tsx
<ToggleField label={T_[fieldNameCamelCase]} value={material.[FieldName].value} onChange={(x) => material.[FieldName].set(x)} />
```

**For float (log scale, for wide range like EmissiveIntensity):**
```tsx
<FormulaeEditorRowFloatLog10 formulaeField="[FieldName]" formulaeModule="material" label={T_[fieldNameCamelCase]} max={[LOG_MAX]} min={[LOG_MIN]} />
```

**For float (linear scale):**
```tsx
<FormulaeEditorRowFloat formulaeModule="material" formulaeField="[FieldName]" label={T_[fieldNameCamelCase]} max={[MAX]} min={[MIN]} />
```

---

#### 6. Localization: `BelzontWE/i18n/i18n.csv`

Add a new line (UTF-16 encoded, tab-separated) after a semantically related entry:
```
K45::WE.vuio[appearenceSettings.[LOC_KEY]]	[EN_LABEL]	[PT_LABEL]
```

Use PowerShell to insert (file is UTF-16):
```powershell
$path = "...BelzontWE\i18n\i18n.csv"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::Unicode)
$lines = $content -split "`r`n"
# Find insertion index (0-based) — after the semantically adjacent entry
$idx = ... # find with: for ($i = 0; $i -lt $lines.Length; $i++) { if ($lines[$i] -like "*[NEIGHBOR_KEY]*") { ... ; break } }
$newLine = "K45::WE.vuio[appearenceSettings.[LOC_KEY]]`t[EN_LABEL]`t[PT_LABEL]"
$newLines = $lines[0..$idx] + $newLine + $lines[($idx+1)..($lines.Length-1)]
[System.IO.File]::WriteAllText($path, ($newLines -join "`r`n"), [System.Text.Encoding]::Unicode)
```

---

#### 7. Localization: `BelzontWE/i18n/zh-HANT.csv`

Same procedure as above (UTF-16, tab-separated), single column:
```
K45::WE.vuio[appearenceSettings.[LOC_KEY]]	[ZH_LABEL]
```

---

#### 8. (If applicable) Rendering/system logic

If the field gates behavior in a system (e.g. `WEEmissiveLightSystem.cs`, `WEPreCullingSystem.cs`, or `WERendererSystem.cs`), add the check:
```csharp
// Example: gate WEEmissiveLightSystem on UseGlobalLight
if (!materialData.[FieldName]) { /* skip */ continue; }
```

---

### Verification

After implementing, run:
```powershell
dotnet build "gitWorkspace\BelzontWE.sln" --configuration Debug --nologo 2>&1 | Where-Object { $_ -match 'error CS\d' }
```
No `error CS*` lines should appear. Frontend TypeScript errors unrelated to this field are pre-existing and can be ignored.

---

## Reference implementations

| Field | Type | XML class | Pattern to follow |
|-------|------|-----------|-------------------|
| `UseGlobalLight` | `bool` | `DefaultStyleXml` | AffectSmoothness + this field |
| `AffectSmoothness` | `bool` | `DecalStyleXml` | Simple bool toggle |
| `EmissiveIntensity` | `float` + formulae | `DefaultStyleXml` | Full formulae float |
| `EmissiveColor` | `Color` + formulae | `DefaultStyleXml` | Full formulae color |

---

## Notes

- The `WETextDataMaterial` struct is an ECS `IComponentData`. Fields must be blittable.
  Allowed types: primitives (`bool`, `float`, `int`), Unity `Color` (struct), `WETextDataValueFloat`, `WETextDataValueColor`.
- After a `CURRENT_VERSION` bump, older saves will use the `else` branch default values — choose safe defaults.
- `dirty = true` must always be set in the setter to trigger material re-upload.
- The `UseGlobalLight` field is only meaningful when `Shader == WEShader.Default && EmissiveIntensity > 0`, but the property itself is defined on the base struct and available for all shaders.
- i18n CSV files use UTF-16 LE encoding with BOM — always use `[System.Text.Encoding]::Unicode` in PowerShell.
- Both `gitWorkspace/_Frontends/` and `KlyteMods/BelzontWE/_Frontends/` resolve to the same files via symlink/junction.
