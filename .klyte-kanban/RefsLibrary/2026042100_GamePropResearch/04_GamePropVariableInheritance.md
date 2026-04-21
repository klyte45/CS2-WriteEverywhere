# Game Prop Variable Inheritance Design

**Date:** 2026-04-21  
**Related:** `01_`, `02_`, `03_` documents in same folder  
**Revision:** v2 — updated to use `WEInheritedVarsCache` pattern (user review 2026-04-21)

---

## 1. Requirement Summary

When a spawned GameProp entity (the real CS2 prop entity created via `WEGamePropSpawnSystem`) processes its `WETemplateForPrefab` default prefab layout, it should **inherit the WE variables** from the WE tree context that spawned it.

- Spawned props are **not user-selectable** for custom WE tree editing — only the default prefab template applies.
- The spawned prop can still render text/images from its prefab's default WE layout, and that layout's formulae should have access to the parent tree's variables.
- **Local variables** (key starts with `!`) from the parent are **NOT inherited** — they are local to the parent entity and must not cascade to spawned props.

---

## 2. Key Insight: `inheritableVars` vs `currentVars`

`PopulateVars()` in `WEPreCullingSystem` maintains two strings at each level of the tree:

```csharp
private unsafe void PopulateVars(Entity entity, ref FixedString512Bytes inheritableVars, out FixedString512Bytes localVars)
{
    localVars = new FixedString512Bytes();
    localVars.Append(inheritableVars);
    if (m_weVariablesLookup.TryGetBuffer(entity, out var variableBuffer) && !variableBuffer.IsEmpty)
    {
        for (int i = 0; i < variableBuffer.Length; i++)
        {
            if (variableBuffer[i].Key[0] != '!')           // '!' = local only, NOT inheritable
            {
                inheritableVars.Append(...key...);          // goes into the cascade stream
            }
            localVars.Append(...key...);                    // local sees both
        }
    }
}
```

After `PopulateVars(GamePropNode, ref inheritableVars, out currentVars)`:
- `inheritableVars` = parent vars + GameProp node's **non-`!`** vars → the clean cascade stream ✅
- `currentVars` = parent vars + **all** of GameProp node's vars (including `!`-local)

For the spawned prop, we save `inheritableVars` — it already excludes local variables by construction, no extra filtering needed.

---

## 3. New Component: `WEInheritedVarsCache`

```csharp
public struct WEInheritedVarsCache : IComponentData, IEnableableComponent
{
    public FixedString512Bytes vars;
}
```

**Location:** `BelzontWE/Components/WEInheritedVarsCache.cs`

**Added to:** Spawned prop entities (those carrying `WEOwner`) at spawn time by `WEGamePropSpawnSystem` (initial value empty).

**Semantics:**
- `vars` = the `inheritableVars` string at the point in the parent's `DrawTree` where the GameProp text node was processed — already free of `!`-local vars.
- **Enabled** = parent updated the vars this frame (signals that children should re-evaluate soon).
- **Disabled** = steady state — `vars` still holds valid current data and must be read regardless.

**Why a new component instead of reusing `WETextDataDirtyFormulae`:**
`WETextDataDirtyFormulae.vars` stores `currentVars` (includes `!`-local vars of the GameProp node); `WEInheritedVarsCache.vars` stores `inheritableVars` (excludes `!`-local vars). Additionally, `WETextDataDirtyFormulae` is semantically for WE text data nodes; `WEInheritedVarsCache` is for geometry/spawned entities.

---

## 4. Entity Relationship

```
Source geometry entity (building, road node, etc.)
  └─ WETemplateForPrefab.childEntity → Root WE text node
       └─ ... DrawTree traversal (variables built up level by level) ...
            └─ GameProp WE text node entity (nextEntity; type = GameProp)
                 │  In DrawTree: inheritableVars computed here
                 │  Written → each sub-object prop entity's WEInheritedVarsCache.vars
                 │
                 └─ Spawned Prop entity (real CS2 prop)
                      ├─ WEOwner.m_weOwnerEntity → GameProp WE text node   (stale detection)
                      ├─ WEInheritedVarsCache.vars   ← written by DrawTree, read by PassedCulling
                      ├─ WESubObject buffer              (sub-sub-objects, if any)
                      ├─ WETemplateForPrefab             (from prefab's default WE layout)
                      └─ (not user-selectable; no custom WE layout)
```

---

## 5. Write Path: `DrawTree` GameProp Case

In `WEPreCullingSystem.WERenderingJob.DrawTree()`, add a `case WESimulationTextType.GameProp:`:

```csharp
case WESimulationTextType.GameProp:
    CheckForUpdates(geometryEntity, nextEntity, unfilteredChunkIndex, in currentVars, 2000);
    // Write the clean inheritable vars to each spawned prop's cache
    if (m_weSubObjectLookup.TryGetBuffer(nextEntity, out var subObjects))
    {
        for (int k = 0; k < subObjects.Length; k++)
        {
            var propEntity = subObjects[k].m_SubObject;
            if (propEntity == Entity.Null) continue;
            if (!m_weInheritedVarsCacheLookup.HasComponent(propEntity)) continue;
            m_CommandBuffer.SetComponent(unfilteredChunkIndex, propEntity,
                new WEInheritedVarsCache { vars = inheritableVars });
            m_CommandBuffer.SetComponentEnabled<WEInheritedVarsCache>(
                unfilteredChunkIndex, propEntity, true);
        }
    }
    break;
```

**Variables in context:**
- `inheritableVars` — local variable in `DrawTree` after `PopulateVars(nextEntity, ref inheritableVars, out currentVars)`. Already excludes `!`-prefix vars.
- `m_weSubObjectLookup` = `GetBufferLookup<WESubObject>(true)` — must be added to job struct.
- `m_weInheritedVarsCacheLookup` = `GetComponentLookup<WEInheritedVarsCache>(false)` — must be added to job struct (writable to support `SetComponent`/`SetComponentEnabled`).

---

## 6. Read Path: `PassedCulling()` — Initial Variables Seeding

In `WEPreCullingSystem.WERenderingJob.PassedCulling()`, seed `variables` from the prop's own cache when present:

**Current (`WEPreCullingSystem.cs` ~line 314):**
```csharp
FixedString512Bytes variables = new();
PopulateVars(entity, ref variables, out _);
```

**New:**
```csharp
FixedString512Bytes variables = new();
// Seed with inherited vars from parent GameProp context — read even if component is disabled
if (m_weInheritedVarsCacheLookup.TryGetComponent(entity, out var inheritedCache))
{
    variables = inheritedCache.vars;
}
PopulateVars(entity, ref variables, out _);  // appends prop's own WETextDataVariable on top
```

> `m_weInheritedVarsCacheLookup` is the same lookup from §5 — if it's in the job as writable (`false`), confirm there is no chunk aliasing; otherwise add a separate read-only copy.

---

## 7. Variable Merging / Priority

Variables are **concatenated** following the existing WE convention:

```
[WEInheritedVarsCache.vars  (inherited from parent, no '!'-local)]
        + [prop entity's own WETextDataVariable buffer]
```

The WE formula engine resolves keys by first-occurrence. Since the prop's own buffer is appended after the inherited vars, **if the same key appears in both, the prop's own value wins** (local override). This is consistent with the existing tree traversal behaviour.

---

## 8. `WEOwner.m_weOwnerEntity` — Role in This Feature

**Confirmed:** `WEOwner.m_weOwnerEntity` points to the **GameProp WE text data node entity** — the entity where `WETextDataMesh.TextType == WESimulationTextType.GameProp` (the `nextEntity` in `DrawTree`).

**Purpose:**
- **Stale detection:** if `m_weOwnerEntity` no longer has `WETextDataMesh` → prop is orphaned → cleanup.
- **Non-selectability:** `HasComponent<WEOwner>()` means "this is a spawned prop, exclude from WE picker/tree."
- **NOT used for variable inheritance** — that is handled by `WEInheritedVarsCache` on the prop entity itself.

---

## 9. Non-Selectability Constraint

GameProp-spawned prop entities (those carrying `WEOwner`) must be excluded from:
1. `WEWorldPickerSystem` / `WEWorldPickerTooltip` — cannot be picked/selected for WE editing.
2. The WE hierarchy tree view — do not appear as editable items.

**Implementation:** Add `.WithNone<WEOwner>()` to entity selection/tooltip queries.

---

## 10. Update Propagation

### 10.1 Natural Propagation Path (Every Visible Frame)

`DrawTree` runs every frame for visible source geometry entities. When the GameProp text node is visited, `WEInheritedVarsCache` on each spawned prop is written and **enabled** with the current `inheritableVars`.

On the next `PassedCulling()` for the spawned prop entity, the new vars are read from `WEInheritedVarsCache`. The existing `CheckForUpdates` interval then propagates updates to individual WE text nodes within the prop's default layout.

### 10.2 Optional Immediate Re-Eval Trigger

When `WEInheritedVarsCache` is newly enabled on a prop, we can trigger immediate re-evaluation of its WE text nodes by resetting `nextUpdateFrame = 0` on the root WE text node:

```csharp
// In DrawTree GameProp case, after writing WEInheritedVarsCache (optional):
if (m_weTemplateForPrefabLookup.TryGetComponent(propEntity, out var propLayout)
    && propLayout.childEntity != Entity.Null
    && m_weMainLookup.TryGetComponent(propLayout.childEntity, out var rootMain))
{
    rootMain.nextUpdateFrame = 0;
    m_CommandBuffer.SetComponent(unfilteredChunkIndex, propLayout.childEntity, rootMain);
}
```

> **Deferred to a later sprint if natural propagation is acceptable for launch.**

### 10.3 Do NOT Use `WETemplateForPrefabDirty` for Variable Propagation

`WETemplateForPrefabDirty` triggers `WEPrefabTemplateDirtyJob`, which **destroys and rebuilds the entire WE layout tree** — only appropriate when the prefab template itself changes. For variable updates, use `nextUpdateFrame = 0` or rely on natural polling.

---

## 11. `WETextDataMesh.UpdateFormulaes()` — GameProp Case

Add a `case WESimulationTextType.GameProp:` in `WETextDataMesh.UpdateFormulaes()` (`WETextDataMesh.cs` ~line 135) to enable formula evaluation of the prefab name field:

```csharp
case WESimulationTextType.GameProp:
    result |= valueData.UpdateEffectiveValue(em, geometryEntity, vars);
    break;
```

This makes the prefab name field respect the WE formula engine: formula → evaluated string → prefab name → looked up in `WEGamePropIndex`.

---

## 12. Summary of New Files / Components

| Component | File | Type | Purpose |
|-----------|------|------|---------|
| `WEInheritedVarsCache` (NEW) | `BelzontWE/Components/WEInheritedVarsCache.cs` | `IComponentData, IEnableableComponent` | Stores parent's `inheritableVars` on spawned prop entity |

---

## 13. Summary of Implementation Changes

| Location | Change |
|----------|--------|
| `WEInheritedVarsCache.cs` (NEW) | New component — `IComponentData, IEnableableComponent`, field `FixedString512Bytes vars` |
| `WEGamePropSpawnSystem.cs` (NEW) | Add `WEInheritedVarsCache` (initial empty, disabled) to spawned prop entity at spawn time |
| `WEPreCullingSystem.cs` job struct | Add `m_weSubObjectLookup` (read-only) + `m_weInheritedVarsCacheLookup` (writable) |
| `WEPreCullingSystem.cs` — `PassedCulling()` | Seed `variables` from `WEInheritedVarsCache.vars` before `PopulateVars(entity, ...)` |
| `WEPreCullingSystem.cs` — `DrawTree()` | Add `case WESimulationTextType.GameProp:` — write `inheritableVars` to `WEInheritedVarsCache` on each `WESubObject` entity |
| `WETextDataMesh.cs` — `UpdateFormulaes()` | Add `case WESimulationTextType.GameProp:` calling `valueData.UpdateEffectiveValue` |
| World picker / hierarchy selector | Add `.WithNone<WEOwner>()` guard to entity queries |
