# Overall Mod Structure: Rendering Pipeline End-to-End

> **Purpose**: Documents the complete rendering pipeline from entity creation to pixels on screen, crossing all system boundaries. Serves as a reference for understanding how all the pieces connect.

## End-to-End Flow

```mermaid
flowchart TD
    subgraph "Template Definition"
        A1[XML Template File<br/>or Mod SubTemplate] --> A2[WETemplateManager<br/>loads & registers]
        A2 --> A3[WETextDataXmlTree<br/>in-memory template]
    end

    subgraph "Entity Instantiation"
        A3 --> B1[WETemplateUpdateSystem<br/>detects prefab match]
        B1 --> B2[Create Entity with:<br/>WETextDataMain<br/>WETextDataTransform<br/>WETextDataMaterial<br/>WETextDataMesh]
        B2 --> B3[Attach WETemplateUpdater<br/>buffer to geometry entity]
        B3 --> B4[Set WEWaitingRendering<br/>enabled]
    end

    subgraph "Mesh Generation"
        B4 --> C1{WEPostRendererSystem<br/>query WEWaitingRendering}
        C1 -->|Text type| C2[FontSystem.DrawText<br/>→ IBasicRenderInformation]
        C1 -->|Image type| C3[WEAtlasesLibrary.GetBRI<br/>or WECustomMeshLibrary]
        C1 -->|Placeholder| C4[Template validation<br/>→ mark for main thread]
        C2 --> C5[Cache BRI reference]
        C3 --> C5
        C5 --> C6[Disable WEWaitingRendering]
    end

    subgraph "Formula Evaluation"
        D1[WEPreCullingSystem<br/>checks nextUpdateFrame] --> D2[Enable WETextDataDirtyFormulae]
        D2 --> D3[WETemplateUpdateSystem<br/>WEUpdateFormulaesJob]
        D3 --> D4[Evaluate up to 19 fields<br/>per entity]
        D4 --> D5[Update EffectiveValues<br/>in Main, Material, Mesh, Transform]
        D5 -->|if changed| D6[Track lastChangeFrame]
    end

    subgraph "Culling"
        E1[Game PreCullingSystem<br/>runs frustum culling] --> E2[NativeList of PreCullingData]
        E2 --> E3[WEPreCullingSystem<br/>WERenderingJob]
        E3 --> E4{Entity passed culling?}
        E4 -->|Yes| E5[Calculate transform matrix<br/>recursive via DrawTree]
        E4 -->|No| E6[Disable WEDrawing]
        E5 --> E7[Enqueue WERenderData<br/>to m_newItemsRender]
        E7 --> E8[m_availToDraw NativeArray]
    end

    subgraph "Rendering"
        E8 --> F1[WERendererSystem<br/>beginContextRendering callback]
        F1 --> F2[For each WERenderData]
        F2 --> F3[Get BRI from component]
        F3 --> F4[mesh = BRI.GetMesh<br/>shader, backface, lod]
        F4 --> F5[matrix = baseMatrix ×<br/>meshTranslation]
        F5 --> F6[Graphics.DrawMesh<br/>mesh, matrix, material<br/>ShadowCastingMode.TwoSided]
    end

    subgraph "Emissive Lighting"
        E8 --> G1[WEEmissiveLightSystem]
        G1 --> G2[For visible entities with<br/>emissive + global light]
        G2 --> G3[Create/Update HDRP PointLight<br/>position, color, intensity, range]
    end

    C6 -.-> E3
    D6 -.-> B4
```

## Frame Execution Order (Summary)

```
1. MainLoop          → WEPostRendererSystem (mesh cache), WEEmissiveLightSystem
2. Modification1     → WENodeExtraDataUpdater (node cache)
3. Modification2B    → WENodeExtraDataUpdater2B (cache invalidation)
4. ModificationEnd   → WEWorldPickerController (selection state)
5. Rendering         → FontServer → Atlases → Meshes → Templates chain
6. UIUpdate          → UI systems
7. UITooltip         → Tooltip system
8. PreCulling        → WEPreCullingSystem (visibility + render list)
9. Unity Callback    → WERendererSystem (actual Graphics.DrawMesh calls)
```

## Entity Component Architecture

```mermaid
classDiagram
    class TextEntity {
        WETextDataMain
        WETextDataTransform
        WETextDataMaterial
        WETextDataMesh
        WETextComponentValid
        WETextDataDirtyFormulae
        WEWaitingRendering
    }

    class GeometryEntity {
        Game.Objects.Transform
        InterpolatedTransform
        CullingInfo
        WEDrawing
        WESubTextRef buffer
        WETemplateUpdater buffer
        WEDependantRendering buffer
    }

    class PrefabEntity {
        PrefabRef
        WETemplateForPrefab
        WETemplateForPrefabDirty
        WEPlacementPivot
        WEPlacementAlignment
    }

    GeometryEntity "1" --> "*" TextEntity : owns via WESubTextRef
    PrefabEntity "1" --> "*" GeometryEntity : templates via WETemplateForPrefab
    TextEntity "*" --> "0..*" WETextDataVariable : buffer elements
```

## Cross-System Data Dependencies

| Producer System | Data Produced | Consumer System | Phase Gap |
|----------------|---------------|-----------------|-----------|
| WENodeExtraDataUpdater | WENetNodeInformation | WERoadFn (via formulas) | Mod1 → Rendering |
| WEWorldPickerController | Selected entity | WEPreCullingSystem | ModEnd → PreCulling |
| FontServer | IBasicRenderInformation | WEPostRendererSystem | Rendering → MainLoop(next) |
| WETemplateManager | Template data | WETemplateUpdateSystem | Rendering (ordered) |
| WETemplateUpdateSystem | Updated components | WEPostRendererSystem | Rendering → MainLoop(next) |
| WEPreCullingSystem | m_availToDraw | WERendererSystem | PreCulling → Unity callback |
| WEPreCullingSystem | m_availToDraw | WEEmissiveLightSystem | PreCulling → MainLoop(next) |
| Game PreCullingSystem | PreCullingData | WEPreCullingSystem | Same phase |

Note: "MainLoop(next)" means the data is consumed in the following frame's MainLoop phase, introducing one frame of latency.
