# 01 — VT Architecture and Rendering Pipeline

> **Purpose**: End-to-end documentation of how CS2 renders asset textures, from file-on-disk to rendered pixel, with emphasis on the Virtual Texturing (VT) system.

## High-Level Architecture

CS2 uses Unity's **Procedural Virtual Texturing (PVT)** system in CPU mode. This means textures are NOT loaded in full to VRAM. Instead:

1. Textures are stored as **tiled, BC7-compressed** data on disk (`.VTSurface` files)
2. At load time, each material **reserves a rectangular region** in a virtual atlas (2048×2048 tiles)
3. At runtime, the GPU **requests tiles** as-needed based on what's visible on screen
4. The CPU **decompresses and uploads** only the requested tiles to a GPU tile cache

## System Component Overview

```mermaid
flowchart TB
    subgraph DiskData["Disk / Asset Database"]
        SA["SurfaceAsset<br/>(.Surface files)"]
        VTS["VTSurfaceAsset<br/>(.VTSurface files)"]
        VTT["VTTextureAsset<br/>(.VTTexture files)"]
        MMC["MidMipCacheAsset<br/>(pre-packed low mips)"]
    end

    subgraph VTCore["VT Core System (CPU)"]
        TSS["TextureStreamingSystem<br/>(COSystemBase)"]
        VTDb["VTDatabase<br/>(atlas bookkeeping)"]
        PVTCPU["VTProceduralCPU<br/>(tile request handler)"]
        Atlas["Atlas<br/>(Z-ordered tile allocator)"]
        AMD["AtlasMaterialsDatabase<br/>(mid-mip pre-registration)"]
    end

    subgraph UnityVT["Unity PVT Layer"]
        CPUStack["CPUTextureStack<br/>(Procedural.CPUTextureStack)"]
        GPUCache["GPU Tile Cache<br/>(BC7_SRGB + BC7_UNorm)"]
    end

    subgraph GameRendering["Game Rendering Pipeline"]
        MBS["ManagedBatchSystem"]
        BMS["BatchMeshSystem"]
        BDS["BatchDataSystem"]
        VTR["VTTextureRequester"]
    end

    subgraph Shader["Shader"]
        SHDR["sg_defaultshader<br/>(DefaultPVTStack / ExtendedPVTStack)"]
    end

    SA -->|"LoadHeader + LoadProperties"| TSS
    VTS -->|"per-material tile data"| TSS
    VTT -->|"per-texture tile data"| TSS
    MMC -->|"pre-packed mid mips"| AMD

    TSS --> VTDb
    TSS --> PVTCPU
    TSS --> AMD
    VTDb --> Atlas

    PVTCPU -->|"PopRequests / UpdateStacks"| CPUStack
    CPUStack -->|"tile upload"| GPUCache

    MBS -->|"CreateBatch"| BMS
    MBS -->|"VTAtlassingInfo[]"| VTR
    VTR -->|"RequestRegion per visible texture"| TSS

    GPUCache --> SHDR
    MBS -->|"Material + VTTextureParamBlock"| SHDR
```

## Data Flow: Asset Load to Rendered Pixel

### Phase 1 — System Initialization

```mermaid
sequenceDiagram
    participant TSS as TextureStreamingSystem
    participant VTDb as VTDatabase
    participant PVT as VTProceduralCPU
    participant Atlas as Atlas
    participant AMD as AtlasMaterialsDatabase

    Note over TSS: OnCreate()
    TSS->>TSS: Load VirtualTexturingConfig from Resources
    TSS->>AMD: Load MidMipCacheAssets
    
    Note over TSS: Initialize()
    TSS->>PVT: new VTProceduralCPU(config)
    PVT->>PVT: SetCPUCacheSize, SetGPUCacheSettings
    TSS->>VTDb: new VTDatabase(config, midMipCount, pvt)
    
    loop For each StackData in config
        VTDb->>Atlas: new Atlas(totalW, totalH, tileSize, layers)
        VTDb->>PVT: CreateStack(stackConfigIndex)
        PVT->>PVT: new CPUTextureStack(name, creationParams)
    end

    PVT->>PVT: BindStacksGlobally()
    
    Note over TSS: Pre-register from MidMipCache
    TSS->>AMD: PreRegisterToVT(this)
    AMD->>VTDb: ReserveMultipleTextureRect(...)
```

### Phase 2 — Material Registration (per SurfaceAsset)

```mermaid
sequenceDiagram
    participant SA as SurfaceAsset
    participant TSS as TextureStreamingSystem
    participant VTDb as VTDatabase
    participant Atlas as Atlas

    Note over SA: RegisterToVT(textureStreamingSystem)
    
    loop For each VT stack in material
        SA->>TSS: ReserveTextureRect(stackConfigIndex, w, h)
        TSS->>VTDb: ReserveTextureRect(stackConfigIndex, w, h)
        VTDb->>Atlas: ReserveTextureRect(w, h)
        Atlas-->>VTDb: atlasIndex
        VTDb-->>TSS: VTAtlassingInfo(stackGlobalIndex, indexInStack)
        TSS-->>SA: VTAtlassingInfo
    end

    SA->>SA: Store m_VTAtlassingInfos[]
    
    Note over SA: LoadVTAsync(...)
    SA->>SA: Compute VTTextureParamBlock from atlas position
    SA->>TSS: BindMaterial(material, stackGlobalIndex, ...)
    TSS->>PVT: BindMaterial → CPUTextureStack.BindToMaterial
    SA->>SA: material.SetTextureParamBlock(stackConfigIndex, block)
    SA->>SA: material.EnableKeyword("ENABLE_VT")
```

### Phase 3 — Runtime Tile Streaming

```mermaid
sequenceDiagram
    participant GPU as GPU
    participant PVT as VTProceduralCPU
    participant VTDb as VTDatabase
    participant TSS as TextureStreamingSystem
    participant Disk as AsyncReadManager

    Note over TSS: OnUpdate() — every frame
    
    loop For each active CPUTextureStack
        PVT->>PVT: PopRequests(fetchedRequests)
        
        loop For each tile request
            PVT->>TSS: GetUniversalTileIndex(req)
            
            alt Mid-mip mask hit
                TSS->>TSS: FillRequest from AtlasMaterialsDatabase
            else Normal tile request
                PVT->>VTDb: GetTextureIndex(stack, x, y, w, h)
                
                alt Per-surface tile data exists
                    VTDb->>VTDb: CopyTextureDataFromRequest(surface)
                    VTDb->>Disk: AsyncReadManager.Read(path, cmds)
                    Disk-->>PVT: tile data → CPUTextureStack
                else Per-texture tile data
                    VTDb->>VTDb: CopyTextureDataFromRequest(texture)
                    VTDb->>Disk: CustomAsyncReadManager.ReadAndDecompress
                    Disk-->>PVT: decompressed tile → CPUTextureStack
                end
            end
        end
    end
    
    Note over PVT: Unity internally uploads tiles to GPUCache
    PVT-->>GPU: Tile data transferred
```

### Phase 4 — Batch Creation and Rendering

```mermaid
sequenceDiagram
    participant MBS as ManagedBatchSystem
    participant RP as RenderPrefab
    participant SA as SurfaceAsset
    participant VTR as VTTextureRequester
    participant TSS as TextureStreamingSystem

    Note over MBS: OnUpdate() — process new batch groups
    
    MBS->>RP: GetSurfaceAsset(subMeshIndex)
    RP-->>MBS: surfaceAsset
    MBS->>SA: LoadProperties(useVT=true)
    
    SA-->>MBS: VTAtlassingInfos[]
    
    Note over MBS: CreateBatch()
    MBS->>MBS: Create Material from template + properties
    MBS->>MBS: Set ENABLE_VT keyword, UseStack0/1
    MBS->>MBS: Set VTTextureParamBlock (atlas coords)
    
    MBS->>VTR: RegisterTexture(stackConfig, stackGlobal, vtIndex, bounds)
    VTR-->>MBS: textureRequestIndex
    
    Note over MBS: Per-frame visibility update
    MBS->>VTR: UpdateMaxPixel(stackIndex, requestIndex, maxPixel)
    VTR->>TSS: RequestRegion(stackGlobal, textureIndex, maxPixel, bounds)
```

## Key Data Structures

### VirtualTexturingConfig (ScriptableObject — loaded from Resources)

| Field | Type | Description |
|-------|------|-------------|
| `tileSize` | `int` | Tile dimension in pixels (typically 128) |
| `stackDatas` | `StackData[]` | Per-stack name + layer formats |
| `cpuCacheSize` | `int` | CPU-side tile cache in MB |
| `bc7GPUCacheSize` | `uint` | GPU cache for BC7_SRGB in MB |
| `bc7UNormGPUCacheSize` | `uint` | GPU cache for BC7_UNorm in MB |
| `filterMode` | `FilterMode` | Bilinear or Trilinear |
| `maxTextureSize` | `int` | Max individual texture size in atlas |

Total virtual atlas size: `2048 × tileSize` per dimension (e.g., 262144 px if tileSize=128).

### StackData

| Field | Type | Description |
|-------|------|-------------|
| `stackName` | `string` | E.g. `"DefaultPVTStack"`, `"ExtendedPVTStack"` |
| `layerFormats` | `GraphicsFormat[]` | E.g. `[BC7_SRGB, BC7_SRGB, BC7_UNorm, BC7_SRGB]` |

The default shader (`sg_defaultshader`) uses **two stacks**:
- **Stack 0 — DefaultPVTStack (4 layers)**: `_BaseColorMap`, `_MaskMap`, `_NormalMap`, + 1 more (likely snow)
- **Stack 1 — ExtendedPVTStack (1 layer)**: Additional texture layer

### VTAtlassingInfo

| Field | Type | Description |
|-------|------|-------------|
| `stackGlobalIndex` | `int` | Which atlas instance this texture lives in |
| `indexInStack` | `int` | Z-ordered index within the atlas |
| `addressMode` | `VTAddressMode` | Clamp (most cases) |

### VTTextureParamBlock

| Field | Type | Description |
|-------|------|-------------|
| `transform` | `float4` | `(offsetX, offsetY, scaleX, scaleY)` — atlas UV transform |
| `textureInfo` | `float4` | `(maxMipLevel, 1.0, 1.0, 2.0)` typically |

This struct is set as shader properties `DefaultPVTStack_atlasParams0/1` (or `ExtendedPVTStack_atlasParams0/1`).

### Shader Properties for VT

| Property | Meaning |
|----------|---------|
| `_UseStack0` | Float toggle: 1 = sample from VT stack 0 |
| `_UseStack1` | Float toggle: 1 = sample from VT stack 1 |
| `ENABLE_VT` | Shader keyword: enables VT sampling path |
| `DefaultPVTStack_atlasParams0` | VTTextureParamBlock.transform |
| `DefaultPVTStack_atlasParams1` | VTTextureParamBlock.textureInfo |
| `_BaseColorMap` | Fallback albedo texture (used when VT disabled) |
| `_NormalMap` | Fallback normal map |
| `_MaskMap` | Fallback mask map |

## Summary: Key Insight for Modding

The entire VT system operates at the **SurfaceAsset / Material level**. Each material has:

1. A reference to a template shader (with VT support baked in)
2. VT atlas coordinates (VTTextureParamBlock) set as material properties
3. An `ENABLE_VT` keyword that toggles between VT sampling and direct Texture2D sampling
4. Fallback `_BaseColorMap`, `_NormalMap`, `_MaskMap` texture properties that are **only used when VT is disabled**

This dual-path architecture in the shader is the primary modding hook — disabling VT on a material re-enables direct texture binding.
