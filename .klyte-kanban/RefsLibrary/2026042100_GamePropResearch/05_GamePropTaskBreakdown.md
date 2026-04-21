# GameProp Feature — Task Breakdown

**Date:** 2026-04-21  
**Source documents:** 01_ through 04_ in this folder  
**Tool:** `kk` — use `kk task new --from-json <file> --consume` to create tasks  
**Note:** Task titles below are human-readable; `kk` converts to kebab-case automatically.

---

## Epics

| Tag | Epic Name | Description |
|-----|-----------|-------------|
| `epic-gameprop-core` | Core Infrastructure | Enum, data components, prefab index |
| `epic-gameprop-lifecycle` | Spawn Lifecycle | Spawn, cleanup, transform sync systems |
| `epic-gameprop-vars` | Variable Inheritance | WEInheritedVarsCache pipeline |
| `epic-gameprop-ui` | UI & Frontend | TSX changes, i18n, selection gating |

---

## Dependencies Overview

```
A1 (enum + components)
  └─ A2 (prefab index)
       └─ B1 (spawn system) ── B2 (cleanup)
                           └── B3 (transform)
                           └── C1 (var inheritance)
                                └─ D1 (UI frontend)
                                └─ D2 (selection gating)
```

---

## EPIC A — Core Infrastructure

### A1 — `Add GameProp Enum and Data Components`

```json
{
  "title": "Add GameProp Enum and Data Components",
  "priority": 1,
  "tags": ["epic-gameprop-core"],
  "userStory": "As a mod developer, I want WESimulationTextType.GameProp=7 to exist and all supporting data components to be available so that subsequent systems can compile and reference them.",
  "background": "Docs 01+02: Foundational types for the entire feature. No logic — just type declarations. WESubObject: buffer of spawned prop entity refs on GameProp text node. WEOwner: ICleanupComponentData with m_weOwnerEntity pointing to the GameProp text node that spawned the prop. WEChild: IEmptySerializable marker on spawned props for stale detection. WEInheritedVarsCache: IEnableableComponent holding inheritableVars for spawned props (Doc 04).",
  "implementationNotes": "1) WESimulationTextType.cs: add GameProp=7 after WhiteCube=6. 2) WESubObject.cs: IBufferElementData+ICleanupBufferElementData, field Entity m_SubObject. 3) WEOwner.cs: IComponentData+ICleanupComponentData, field Entity m_weOwnerEntity, NOT ISerializable. 4) WEChild.cs: IComponentData+IEmptySerializable (stale marker, serialized so survives load). 5) WEInheritedVarsCache.cs: IComponentData+IEnableableComponent, field FixedString512Bytes vars.",
  "dod": [
    "WESimulationTextType.GameProp = 7 exists and project compiles",
    "WESubObject.cs created with correct interface implementations",
    "WEOwner.cs created, NOT serializable, has m_weOwnerEntity field",
    "WEChild.cs created as IEmptySerializable stale marker",
    "WEInheritedVarsCache.cs created as IEnableableComponent with vars field",
    "Unit tests added for any serialization edge cases on the new components"
  ]
}
```

### A2 — `Populate WE Game Prop Prefab Index`

```json
{
  "title": "Populate WE Game Prop Prefab Index",
  "priority": 1,
  "tags": ["epic-gameprop-core"],
  "dependsOn": ["A1"],
  "userStory": "As the spawn system, I need a fast O(1) lookup from prefab name to Entity for eligible static object prefabs so that prop spawning does not stall during gameplay.",
  "background": "Doc 02 §1.2–1.4: WEGamePropIndex = Dictionary<string, Entity> populated in the same UpdatePrefabIndexDictionary_Coroutine loop as PrefabNameToIndex. Eligibility filter: HasComponent<StaticObjectData> && !HasComponent<BuildingData> && !HasComponent<BuildingExtensionData>.",
  "implementationNotes": "In WETemplateManager.PrefabLayout.cs — add public Dictionary<string, Entity> WEGamePropIndex alongside PrefabNameToIndex. In the coroutine loop, after building PrefabNameToIndex entry, check eligibility and add to WEGamePropIndex if eligible. Both dictionaries share one pass over PrefabSystemOverrides.LoadedPrefabBaseList().",
  "dod": [
    "WEGamePropIndex property exists on WETemplateManager",
    "Eligibility filter (StaticObjectData present, no BuildingData/BuildingExtensionData) correctly implemented",
    "Index populated in same coroutine pass as PrefabNameToIndex (no separate system)",
    "WEGamePropIndex rebuilt when SpritesAndLayoutsDataVersion changes (same invalidation as PrefabNameToIndex)",
    "Unit tests verify eligible props appear and ineligible ones (buildings, vehicles) do not"
  ]
}
```

---

## EPIC B — Spawn Lifecycle

### B1 — `Implement Game Prop Spawn System`

```json
{
  "title": "Implement Game Prop Spawn System",
  "priority": 1,
  "tags": ["epic-gameprop-lifecycle"],
  "dependsOn": ["A2"],
  "userStory": "As a player, I want GameProp WE nodes to spawn real CS2 prop entities in the world, positioned relative to their parent geometry entity, with depth and cycle guards preventing infinite recursion.",
  "background": "Doc 02: WEGamePropSpawnSystem runs at ModificationEnd phase. Rate-limited (max N dirty GameProp nodes per frame, configurable). Depth guard: max depth 4 (conservative vs vanilla 7). Cycle guard: tracks seen prefab names per chain. Uses WEGamePropIndex for O(1) lookup. Spawned entity gets: WEOwner (points to GameProp text node), WEChild, WEInheritedVarsCache (empty), Game.Objects.Secondary (exempt from vanilla SubObjectSystem). WESubObject buffer on text node updated with ref to spawned entity. Instancing: for array instancing on the GameProp node, spawn one prop per instance (same C# limits as Placeholder type).",
  "implementationNotes": "1) Check if text node already has WESubObject entries with valid entities — if so, skip unless dirty. 2) Look up prefab by ValueData.EffectiveValue in WEGamePropIndex. 3) Instantiate prefab entity, add WEOwner+WEChild+WEInheritedVarsCache+Secondary. 4) Position via LocalTransform matching parent geometry + WE node transform. 5) Update WESubObject buffer on the GameProp text node. 6) Handle instancing: for ArrayInstancing counts > 1, spawn one prop per instance index with appropriate offset.",
  "dod": [
    "Spawn system creates prop entity when GameProp WE node has a valid prefab name",
    "Spawned prop has WEOwner, WEChild, WEInheritedVarsCache, Game.Objects.Secondary components",
    "WESubObject buffer on GameProp text node references spawned prop entity",
    "Depth guard prevents spawning beyond depth 4",
    "Cycle guard prevents spawning same prefab name in its own WE tree chain",
    "Rate limiting caps dirty-node processing per frame",
    "Array instancing spawns correct count of props with appropriate transforms",
    "Unit tests verify D spawn, depth guard, cycle guard, and WEOwner assignment"
  ]
}
```

### B2 — `Implement Game Prop Cleanup System`

```json
{
  "title": "Implement Game Prop Cleanup System",
  "priority": 2,
  "tags": ["epic-gameprop-lifecycle"],
  "dependsOn": ["B1"],
  "userStory": "As a player, I want spawned GameProp entities to be cleaned up when their parent WE node is removed or the prefab name changes, so that orphaned props do not litter the world.",
  "background": "Doc 01 CPs 1-3: Stale detection via WEChild+WEOwner pattern. Post-load: entities with WEChild but without WEOwner are stale (serialized across save but owner not persisted). Also cleans up when WEOwner.m_weOwnerEntity no longer exists or no longer has WETextDataMesh.",
  "implementationNotes": "1) Query entities with WEChild+WEOwner: check if m_weOwnerEntity still has WETextDataMesh — if not, destroy the prop and remove from WESubObject buffer of the (now invalid) owner text node. 2) Query entities with WEChild but WITHOUT WEOwner (post-load stale) — destroy them. 3) Also handle the case where the GameProp text type changes away from GameProp — destroy all WESubObject entries on that node.",
  "dod": [
    "Orphaned props (WEChild + no WEOwner) are destroyed during cleanup phase",
    "Props whose WEOwner.m_weOwnerEntity no longer has WETextDataMesh are destroyed",
    "WESubObject buffer on GameProp text node is cleaned up when props are destroyed",
    "Props survive save/load correctly (WEChild persists, WEOwner does not, cleanup runs on load)",
    "Unit tests verify stale detection post-load and orphan cleanup"
  ]
}
```

### B3 — `Implement Game Prop Transform Sync`

```json
{
  "title": "Implement Game Prop Transform Sync",
  "priority": 2,
  "tags": ["epic-gameprop-lifecycle"],
  "dependsOn": ["B1"],
  "userStory": "As a player, I want spawned GameProp entities to follow their parent geometry entity's position and rotation, so that props on a moving vehicle or relocated building stay correctly placed.",
  "background": "Doc 02: WEGamePropTransformSystem runs at PreRendering phase every frame. For each spawned prop entity with WEOwner: look up parent geometry entity transform, compose with WE node transform, write to prop's LocalTransform.",
  "implementationNotes": "Reads WEOwner.m_weOwnerEntity (GameProp text node) → get WETextDataMain.targetEntity (the geometry entity) → get LocalTransform → compose with WETextDataTransform on the GameProp text node → write to spawned prop's LocalTransform.",
  "dod": [
    "Spawned prop transform updates every frame to match parent geometry entity + WE node offset",
    "Transform sync works for static buildings, road nodes, and vehicles",
    "Performance: system uses IJobChunk and is Burst-compiled",
    "No transform update when parent geometry entity does not exist (graceful skip)"
  ]
}
```

---

## EPIC C — Variable Inheritance

### C1 — `Implement Game Prop Variable Inheritance`

```json
{
  "title": "Implement Game Prop Variable Inheritance",
  "priority": 2,
  "tags": ["epic-gameprop-vars"],
  "dependsOn": ["B1"],
  "userStory": "As a WE layout author, I want to define variables on a parent WE tree and have them automatically available in the default prefab layout of any GameProp spawned from that tree, so that I can write dynamic templated prop layouts.",
  "background": "Doc 04: WEInheritedVarsCache on spawned prop entity stores the parent's inheritableVars (excludes !-local vars). Written by DrawTree GameProp case via ECB. Read by PassedCulling() before PopulateVars() call. WETextDataMesh.UpdateFormulaes() also needs GameProp case for prefab name formula eval.",
  "implementationNotes": "1) WEPreCullingSystem — add m_weSubObjectLookup + m_weInheritedVarsCacheLookup to job struct. 2) In PassedCulling(): seed variables from WEInheritedVarsCache.vars if present (read even if disabled). 3) In DrawTree() add case WESimulationTextType.GameProp: call CheckForUpdates then write inheritableVars to WEInheritedVarsCache on each WESubObject entity. 4) WETextDataMesh.UpdateFormulaes(): add case GameProp calling valueData.UpdateEffectiveValue(em, geometryEntity, vars).",
  "dod": [
    "WEPreCullingSystem.PassedCulling() seeds variables from WEInheritedVarsCache when present",
    "DrawTree() GameProp case writes inheritableVars (no !-local vars) to each WESubObject's WEInheritedVarsCache",
    "WETextDataMesh.UpdateFormulaes() evaluates GameProp prefab name formula correctly",
    "Parent variables (non-!) are visible to formulae in the spawned prop's default prefab layout",
    "Parent !-local variables are NOT visible in spawned prop's layout",
    "Unit tests verify variable inheritance and local-var exclusion"
  ]
}
```

---

## EPIC D — UI & Frontend

### D1 — `Add GameProp Frontend UI`

```json
{
  "title": "Add GameProp Frontend UI",
  "priority": 2,
  "tags": ["epic-gameprop-ui"],
  "dependsOn": ["C1"],
  "userStory": "As a player using the WE editor, I want to select GameProp (7) as a content type, enter a prefab name with optional formula support, and see the correct icon, without accidentally seeing irrelevant panels like Appearance or Shader.",
  "background": "Doc 03: All TSX changes + i18n. WEFormulaeElement.ts enum update. WETextValueSettings.tsx: add 7 to dropdown, add GameProp content block modelled on Placeholder (with FormulaeEditRow for formula support), gate Flip-Z. WriteEverywhereToolOptions.tsx: add isGameProp flag, gate Shader+Appearance buttons and windows. WETextHierarchyView.tsx: add i_typeGameProp = BenchAndParkProps.svg + case. i18n: 3 new keys.",
  "implementationNotes": "For WETextValueSettings GameProp block, mirror the Placeholder block but use FormulaeEditRow (like Text type) since formula support is needed. Label: textValueSettings.gamePropName. i18n keys: contentType.7, gamePropName, formulaeTitleName.mesh.7.ValueText.",
  "dod": [
    "WESimulationTextType.GameProp=7 added to TS enum in WEFormulaeElement.ts",
    "Content type dropdown includes GameProp(7), excludes Archetype(3)",
    "GameProp content block shows prefab name field with formula support (FormulaeEditRow)",
    "Appearance and Shader buttons/windows hidden when isGameProp=true",
    "Flip-Z toggle hidden for GameProp nodes",
    "BenchAndParkProps.svg icon shown for GameProp nodes in hierarchy tree",
    "All 3 i18n keys added to i18n.csv (English + pt-BR) per project convention"
  ]
}
```

### D2 — `Gate WEOwner Entities from WE Selection`

```json
{
  "title": "Gate WEOwner Entities from WE Selection",
  "priority": 3,
  "tags": ["epic-gameprop-ui"],
  "dependsOn": ["B1"],
  "userStory": "As a player using the WE editor, I want spawned GameProp prop entities to be invisible to the WE picker and hierarchy tree, so I cannot accidentally select and edit them as if they were regular WE layout targets.",
  "background": "Doc 04 §9: Spawned prop entities have WEOwner component. WEWorldPickerSystem and WEWorldPickerTooltip must exclude these. The hierarchy tree should not list them.",
  "implementationNotes": "Add .WithNone<WEOwner>() to the entity queries in WEWorldPickerSystem, WEWorldPickerTooltip, and any other query that iterates selectable WE geometry entities.",
  "dod": [
    "WEWorldPickerSystem entity query excludes entities with WEOwner",
    "WEWorldPickerTooltip entity query excludes entities with WEOwner",
    "Spawned GameProp props cannot be selected as WE editing targets in-game",
    "Clicking on a spawned prop in the world (if applicable) falls through to the parent geometry entity"
  ]
}
```

---

## kk Task IDs

| Code | kk ID | Title | Status | Epic |
|------|-------|-------|--------|------|
| A1 | [0116] | Add GameProp Enum and Data Components | N | gameprop-core |
| A2 | [0117] | Populate WE Game Prop Prefab Index | N | gameprop-core |
| B1 | [0118] | Implement Game Prop Spawn System | N | gameprop-life |
| B2 | [0119] | Implement Game Prop Cleanup System | N | gameprop-life |
| B3 | [0120] | Implement Game Prop Transform Sync | N | gameprop-life |
| C1 | [0121] | Implement Game Prop Variable Inheritance | L | gameprop-vars |
| D1 | [0122] | Add GameProp Frontend UI | L | gameprop-ui |
| D2 | [0123] | Gate WEOwner Entities from WE Selection | L | gameprop-ui |

---

## Sprint Split Suggestion

Given scope, this is recommended as **two sprints**:

### Sprint 1 — Backend Foundation
Tasks: **A1 [0116], A2 [0117], B1 [0118], B2 [0119], B3 [0120]**  
Goal: Spawn pipeline works — props appear, move, and clean up correctly. No UI yet, tested via unit tests and in-game observation.

### Sprint 2 — Variable Inheritance + UI
Tasks: **C1 [0121], D1 [0122], D2 [0123]**  
Goal: Variables cascade into spawned prop layouts; full UI including type selection, formula editing, icon, and selection gating.

---

## kk JSON Files To Create

One JSON file per task, in `_kanban/tasks/json-import/`:
- `gameprop-task-A1.json`
- `gameprop-task-A2.json`
- `gameprop-task-B1.json`
- `gameprop-task-B2.json`
- `gameprop-task-B3.json`
- `gameprop-task-C1.json`
- `gameprop-task-D1.json`
- `gameprop-task-D2.json`

After creation, promote relevant Sprint 1 tasks to N status: `kk task status <id> N`

---

## DoD Shared Across All Tasks

- Build passes (no CS errors)
- All existing tests still pass (baseline: 757 passing + 9 ignored)
- New unit tests added where indicated
- i18n entries added where applicable (see belzontwE-i18n memory note)
