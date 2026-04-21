# Game Prop Feature — Requirements, Research & Critical Points

**Date:** 2026-04-21  
**Feature:** New `WESimulationTextType.GameProp` — spawn game prefabs as non-serialized sub-objects owned by WE entities.

---

## 1. Requirements Summary

| # | Requirement |
|---|-------------|
| R1 | New `WESimulationTextType` value: `GameProp` |
| R2 | `WETextDataMesh.CustomMeshName` stores the prefab name to instantiate |
| R3 | A new `WESubObject` `IBufferElementData` buffer, exclusive to this mod, analogous to vanilla `Game.Objects.SubObject` |
| R4 | Upon selecting a prefab, the system spawns game-object entities and populates the `WESubObject` buffer |
| R5 | Each spawned entity gets vanilla `Owner` → `TargetEntity`, plus new `WEOwner` → the WE entity holding the `WESubObject` buffer |
| R6 | Spawned entity instantiation follows the vanilla SubObjects creation process |
| R7 | The `WESubObject` buffer is responsible for cleanup when its owning WE entity is removed |
| R8 | **Neither `WESubObject` entities nor `WEOwner` entities shall be serialized** |

---

## 2. Vanilla Reference: Key Structures

### 2.1 `Game.Objects.SubObject` (runtime buffer element)
```csharp
[InternalBufferCapacity(0)]
public struct SubObject : IBufferElementData, IEquatable<SubObject>, IEmptySerializable
{
    public Entity m_SubObject;
}
```
- `IEmptySerializable` → saved/loaded by the vanilla save system along with the owning entity.
- Lives on the **owner entity** as a `DynamicBuffer<SubObject>`.

### 2.2 `Game.Prefabs.SubObject` (prefab definition buffer)
```csharp
[InternalBufferCapacity(0)]
public struct SubObject : IBufferElementData
{
    public Entity m_Prefab;
    public SubObjectFlags m_Flags;
    public float3 m_Position;
    public quaternion m_Rotation;
    public int m_ParentIndex;
    public int m_GroupIndex;
    public int m_Probability;
}
```
- Stored on the **prefab entity** — describes what sub-objects to spawn and at what relative positions.

### 2.3 `Game.Common.Owner`
```csharp
public struct Owner : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Owner;
}
```
- Serializable → if a spawned prop entity holds `Owner`, the vanilla serialization SubObjectSystem (`Game.Serialization.SubObjectSystem`) will pick it up post-deserialization and try to add it back to the `SubObject` buffer on the owner.

### 2.4 Vanilla `Game.Serialization.SubObjectSystem`
After a save load, runs a job that queries all `Object` entities with `Owner` (excluding Vehicles and Creatures) and rebuilds the `SubObject` buffer on their owners. This is the vanilla re-linking mechanism.

---

## 3. Critical Architectural Points & Open Questions

### CP-1: Non-Serialization Strategy — RESOLVED

**Key finding:** `Game.Objects.Secondary` (`IEmptySerializable`) is the "static flag" that exempts entities from vanilla `SubObjectSystem` management. The `UpdateSubObjectsJob` and `FillOldSubObjectsBuffer` both check `!m_SecondaryData.HasComponent(subObject)` — entities with `Secondary` are **never created, updated, or deleted** by the vanilla SubObjectSystem.

**Decision — Two-marker approach:**

| Component | Serializable? | Purpose |
|-----------|---------------|---------|
| `Game.Objects.Object` | Yes (`IEmptySerializable`) | Marks as game object; enables Cim interaction; rendering |
| `Game.Common.Owner` → TargetEntity | Yes (`ISerializable`) | Vanilla ownership for Cim pathfinding/activities |
| `Game.Objects.Secondary` | Yes (`IEmptySerializable`) | Exempts from vanilla SubObjectSystem management |
| `WEChild` (new, `IEmptySerializable`) | **Yes** | Serialization marker; survives save/load as stale-entity detector |
| `WEOwner` → WE entity | **No** (plain struct, no ISerializable/IEmptySerializable) | Back-link to WE entity; intentionally stripped on save |

**Post-load lifecycle:**
1. Save includes `WEChild` (serialized). `WEOwner` is NOT saved (no serialization interface → stripped).
2. After load, a WE cleanup system queries entities **with `WEChild` but without `WEOwner`** → stale entities from save → immediately destroyed.
3. WE re-creates its entities from XML. For each `GameProp` WE node, a fresh prop entity is spawned with both `WEChild` + `WEOwner` correctly set.

**Why this works:** `WEOwner`'s absence post-load uniquely and deterministically marks stale entities. No WE entity IDs are stored in serializable components. `Secondary` prevents vanilla from touching them between save events.

---

### CP-2: Component Composition of Spawned Prop Entity — RESOLVED

**Decision — Full game-object, using `Secondary` to prevent vanilla management:**

| Component | Value | Notes |
|-----------|-------|-------|
| `Game.Objects.Object` | (empty) | Required for vanilla rendering, AI, LOD |
| `Game.Common.Owner` | → TargetEntity | Cim ownership/pathfinding |
| `Game.Objects.Secondary` | (empty) | Prevents vanilla SubObjectSystem from managing this entity |
| `WEChild` (new) | (empty) | Serialization stale-entity marker |
| `WEOwner` (new) | → WE entity | Back-link; NOT serialized |
| `Game.Objects.Transform` | computed from WETextDataTransform | World position from WE transform |
| `PrefabRef` | → resolved prefab entity | |
| Additional prefab-type-specific components | as spawned by vanilla SubObjectSystem equivalent | copied from vanilla initialization |

---

### CP-3: The `WESubObject` Buffer — Where Does It Live?

Per the requirements, `WESubObject` is a `DynamicBuffer` on the WE entity (the entity that has `WETextDataMesh` with `TextType == GameProp`). This is analogous to how a vanilla owner entity has `DynamicBuffer<Game.Objects.SubObject>`.

**Proposed struct:**
```csharp
[InternalBufferCapacity(0)]
public struct WESubObject : IBufferElementData, ICleanupBufferElementData
{
    public Entity m_SubObject; // the spawned prop entity
}
```

`ICleanupBufferElementData` ensures Unity ECS keeps the buffer component alive on the WE entity even after it is destroyed, so the cleanup system can detect and destroy the spawned prop entities.

---

### CP-4: The `WEOwner` Component — RESOLVED

```csharp
// On each spawned prop entity — NOT serialized (no ISerializable/IEmptySerializable)
public struct WEOwner : IComponentData, ICleanupComponentData
{
    public Entity m_weOwnerEntity; // WE entity holding the WESubObject buffer
}
```

- `ICleanupComponentData`: keeps it alive post-entity-removal so cleanup can destroy the spawned props  
- **NOT serializable**: intentionally absent after save/load → combined with `WEChild` creates the stale-entity detection signal
- The spawned entity ALSO has vanilla `Owner` → TargetEntity (per R5) — separate from `WEOwner`

```csharp
// On spawned prop entity — IS serialized (IEmptySerializable)
public struct WEChild : IComponentData, IQueryTypeParameter, IEmptySerializable { }
```

---

### CP-5: Vanilla Owner Conflict with SubObjectSystem — RESOLVED

**Decision:** `WESubObject` only — WE manages its own buffer exclusively.

`Game.Objects.Secondary` on the prop entity prevents the vanilla `Game.Objects.SubObjectSystem` from filling/removing the entity in the vanilla `SubObject` buffer. The `Game.Serialization.SubObjectSystem` (post-load re-linker) does NOT check `Secondary`, so it may still add stale entities to the vanilla buffer on load — but those stale entities are immediately destroyed by WE's cleanup system before any meaningful interaction occurs.

---

### CP-6: Lifecycle / Trigger System

The lifecycle events that need handling:

| Event | Required Action |
|-------|-----------------|
| `WETextDataMesh.TextType` changes to `GameProp`, `CustomMeshName` set | Resolve prefab by name → create prop entities → populate `WESubObject` buffer |
| `CustomMeshName` changes | Destroy old prop entities → create new ones |
| WE entity is removed (`Deleted`) | Destroy all spawned prop entities in `WESubObject` buffer |
| Game load | WE entities re-created from XML → trigger GameProp creation on WE entity initialization |

---

### CP-7: Prefab Lookup by Name — UPDATED

**Field decision:** Prefab name is stored in **`ValueData`** (type `WETextDataValueString`, 512 bytes), NOT `CustomMeshName`. `ValueData` has formula support via `UpdateEffectiveValue`; its `EffectiveValue` will hold the resolved prefab name string. This is consistent with how the `Text` and `Image` types use `ValueData` for their primary content.

`CustomMeshName` remains unused for `GameProp` type.

**Prefab resolution:**
```csharp
prefabSystem.TryGetPrefab<PrefabBase>(meshData.ValueData.EffectiveValue.ToString(), out var prefab);
prefabSystem.TryGetEntity(prefab, out var prefabEntity);
```

Prefab entity must have `Game.Prefabs.ObjectData` to be spawnable as a game object. Resolution happens on the main thread via a managed system — see document 02 for the full request path.

---

### CP-8: Transform / Position Logic — RESOLVED

**Decision:** Use the WE entity's `WETextDataTransform` offset/rotation relative to `TargetEntity`.

The spawned prop's world `Transform` is computed as:  
`TargetEntity.Transform + WEEntity.WETextDataTransform` (offset/rotation applied in local space).

---

## 4. Vanilla SubObject Creation Process (Abbreviated)

The vanilla `Game.Objects.SubObjectSystem.UpdateSubObjectsJob`:
1. Iterates over owners that have `SubObject` buffer and are `Updated`/`Deleted`
2. For each prefab's `DynamicBuffer<Game.Prefabs.SubObject>`, creates/updates/destroys child `Object` entities
3. Each child gets: `Transform`, `PrefabRef`, `Owner` → parent, `Game.Objects.Object`, and type-specific components
4. Uses an `EntityCommandBuffer` (via `ModificationBarrier2B`) to defer entity creation

For WE's purposes, a simplified version is needed:
- Only one "sub object" per `WESubObject` entity (one spawned prop per GameProp WE node — or possibly multiple if the prefab itself has sub-objects)
- Created via a managed system on the main thread (because prefab resolution requires `PrefabSystem`)

---

## 5. Non-Serialization — Current WE Entity Pattern

WE currently creates entities with:
- `WETextDataMain`, `WETextDataMesh`, `WETextDataMaterial`, `WETextDataTransform` — **no** vanilla `ISerializable` components
- No `Game.Objects.Object`, no vanilla `Owner`

These entities are NOT picked up by the vanilla serialization system because they have no serializable components. The WE data is stored separately (in an XML-based save extension). WE entities are re-created from that XML on game load.

**If GameProp spawned entities also lack `Game.Objects.Object` and vanilla `Owner`**: the vanilla serialization system won't touch them, satisfying R8. BUT they may not receive all vanilla rendering passes.

---

## 6. Proposed WE Component Additions

| Component | Type | Purpose |
|-----------|------|---------|
| `WESubObject` | `IBufferElementData, ICleanupBufferElementData` | Buffer on WE GameProp entity; holds references to spawned prop entities |
| `WEOwner` | `IComponentData, ICleanupComponentData` | On each spawned prop entity; back-reference to the WE entity holding the buffer |

Both are suffixed to avoid collision with vanilla names.

---

## 7. Decisions Recorded

| # | Question | Decision |
|---|----------|----------|
| CP-1 | Serialization + Cim interaction | **`WEChild` (serialized) + `WEOwner` (NOT serialized)**. Post-load cleanup: entities with `WEChild` but without `WEOwner` are stale → destroy. Full Cim interaction via `Object`+`Owner`+`Secondary`. |
| CP-2 | Component composition | Full game-object: `Object`, `Owner`→TargetEntity, `Secondary`, `WEChild`, `WEOwner`, `Transform`, `PrefabRef` + prefab-type extras |
| CP-3 | WESubObject buffer | `IBufferElementData, ICleanupBufferElementData` on the WE entity |
| CP-4 | WEOwner | `IComponentData, ICleanupComponentData`, NOT serializable |
| CP-5 | Vanilla SubObject buffer | **WESubObject only** — vanilla SubObject buffer NOT touched by WE |
| CP-8 | Prop position | **WE entity `WETextDataTransform` offset/rotation** relative to TargetEntity |
| CP-Multi | Spawn count | **Recursive like vanilla** — spawn prefab + its own vanilla sub-objects |
| CP-Scope | Rendering depth | **Target is full game-object** (LOD, collision, AI, shadows); may be reviewed |

## 8. Open Questions for Review

All critical points resolved. No open questions remain at this stage.
