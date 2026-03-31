# Font Processing System: Architecture & Pipeline

> **Purpose**: Documents the complete font processing pipeline from font file loading to rendered mesh output, including all data structures, job chains, and the atlas management system.

## System Overview

The font system converts TTF/OTF font files into per-string 3D meshes (vertices, triangles, UVs) that can be rendered via `Graphics.DrawMesh`. It is built on a custom stbtt (STB TrueType) port for glyph rasterization and uses a skyline-packing atlas for texture storage.

## Class Hierarchy

```mermaid
classDiagram
    class FontServer {
        +static Instance : FontServer
        +DefaultFont : FontSystemData
        +QualitySize : int
        +ScaleEffective : Vector2
        +RegisterFont(name, fontData) : bool
        +DestroyFont(name)
        +TryGetFont(name, out data) : bool
        +DictPtr : GCHandle
        -LoadedFonts : Dictionary~string, FontSystemData~
        -OnUpdate()
    }

    class FontSystemData {
        +Font : Font
        +FontSystem : FontSystem
        +Name : string
        +IsWeak : bool
        +Guid : Hash128
        +static From(fontData, name) : FontSystemData
    }

    class FontSystem {
        +Data : FontSystemData
        +FontHeight : int
        +CurrentAtlas : FontAtlas
        +DrawText(str) : IBasicRenderInformation
        +EnsureText(str, scale)
        +RunJobs(dependency) : JobHandle
        +ResetCache()
        -m_textCache : Dictionary~string, PrimitiveRenderInformation~
        -m_itemsQueue : Queue~StringRenderingQueueItem~
        -m_jobOutputQueue : NativeQueue~BasicRenderInformationJob~
    }

    class Font {
        +Ascent : float
        +Descent : float
        +LineHeight : float
        +Capital : float
        +Scale : float
        +RecalculateBasedOnHeight(height)
        +GetGlyphIndex(codepoint) : int
        +BuildGlyphBitmap(glyph, scale, ...)
        +RenderGlyphBitmap(output, ...)
        +static FromMemory(data) : Font
        -_font : FontInfo
    }

    class FontAtlas {
        +Width : int
        +Height : int
        +Texture : Texture2D
        +Version : uint
        +IsPendingApply : bool
        +AddRect(rw, rh, ref rx, ref ry) : bool
        +RenderGlyph(glyph) : bool
        +Apply()
        +Reset(w, h)
        +Expand(w, h)
    }

    class FontGlyph {
        +Codepoint : int
        +Index : int
        +Font : Font
        +x, y, width, height : float
        +XAdvance, XOffset, YOffset : int
        +AtlasGenerated : bool
        +GetKerningCached(nextGlyph) : int
        -kernings : NativeHashMap~int, int~
    }

    class StringRenderingJob {
        <<IJobParallelForBatch>>
        +output : NativeQueue~BasicRenderInformationJob~.ParallelWriter
        +inputArray : NativeArray~StringRenderingQueueItem~
        +glyphs : NativeHashMap~int, FontGlyph~
        +Execute(idx, count)
    }

    FontServer "1" --> "*" FontSystemData
    FontSystemData "1" --> "1" FontSystem
    FontSystemData "1" --> "1" Font
    FontSystem "1" --> "1" FontAtlas
    FontSystem "1" --> "*" FontGlyph
    FontSystem ..> StringRenderingJob : schedules
```

## Processing Pipeline

### Phase 1: Font Registration

```mermaid
flowchart TD
    A["byte[] font data"] --> B[Font.FromMemory]
    B --> C[FontInfo.stbtt_InitFont]
    C --> D[Parse TT/OTF tables:<br/>head, hhea, maxp, cmap, glyf, loca]
    D --> E[Font metrics:<br/>Ascent, Descent, LineHeight, Capital]
    E --> F[FontSystemData.From]
    F --> G[FontServer.LoadedFonts dictionary]
    G --> H[Event: OnFontsLoadedChanged]
```

### Phase 2: Text Request

When `DrawText(string)` is called:

```mermaid
flowchart TD
    A[DrawText string] --> B{In m_textCache?}
    B -->|Yes, valid| C[Return cached BRI]
    B -->|Yes, atlas outdated| D[Invalidate, re-queue]
    B -->|No| E[Queue StringRenderingQueueItem]
    D --> E
    E --> F[Return LOADING_PLACEHOLDER]
```

The `LOADING_PLACEHOLDER` is a sentinel BRI that the renderer checks — if received, it skips rendering that text this frame.

### Phase 3: Job Pipeline (FontServer.OnUpdate → FontSystem.RunJobs)

```mermaid
flowchart TD
    subgraph "Main Thread - Pre-Job"
        A[Dequeue up to 256 items from m_itemsQueue] --> B[Extract unique codepoints]
        B --> C{For each codepoint}
        C --> D[GetGlyph: lookup or create FontGlyph]
        D --> E{Glyph in atlas?}
        E -->|No| F[FontAtlas.AddRect → allocate space]
        F --> G[FontAtlas.RenderGlyph → rasterize bitmap]
        G --> H{Atlas full?}
        H -->|Yes| I[Expand atlas, clear cache, retry]
        H -->|No| J[Glyph ready in atlas]
        E -->|Yes| J
    end

    subgraph "Worker Threads - Job"
        K[StringRenderingJob<br/>IJobParallelForBatch<br/>batchSize=32] --> L[Per string: iterate Runes]
        L --> M[Per Rune: lookup glyph]
        M --> N[GetQuad: apply kerning, positioning]
        N --> O[DrawChar: emit 2D quad<br/>4 vertices, 2 triangles, 4 UVs]
        N --> P[DrawCharCube: emit 3D cube<br/>24 vertices, 12 triangles]
        O --> Q[AlignVertices: center horizontally]
        P --> Q
        Q --> R[BasicRenderInformationJob output]
        R --> S[NativeQueue.ParallelWriter enqueue]
    end

    subgraph "Main Thread - Post-Job"
        T[Dependency.Complete - BLOCKING] --> U[Dequeue up to 256 results]
        U --> V{AtlasVersion match?}
        V -->|Yes| W[PrimitiveRenderInformation.Fill]
        V -->|No| X[Discard, will re-queue]
        W --> Y[Update m_textCache]
    end

    J --> K
    S --> T
```

### Phase 4: Atlas GPU Upload

Every 60 frames (if pending):
```
FontAtlas.IsPendingApply → Texture2D.Apply(false, false) → GPU upload
```

`Texture2D.Apply()` is a synchronous GPU operation that blocks the main thread.

## Memory Layout

### Atlas Packing (Skyline Algorithm)

```
┌────────────────────────────────────────┐
│  ┌──┐ ┌─────┐ ┌──┐                    │
│  │ A│ │  B  │ │C │                     │  ← Skyline level 1
│  │  │ │     │ │  │ ┌───┐ ┌──┐         │
│  │  │ │     │ │  │ │ D │ │E │         │  ← Skyline level 2  
│  │  │ │     │ │  │ │   │ │  │         │
│  └──┘ └─────┘ └──┘ └───┘ └──┘         │
│                                        │
│              Free Space                │
│                                        │
└────────────────────────────────────────┘
   Width: 512-16384 (power of 2)
   Height: same as width
   Format: ARGB32
```

### Glyph Storage

```
Two-level NativeHashMap:
  Level 1: FontHeight (int) → Level 2
  Level 2: Codepoint (int) → FontGlyph struct

Per-glyph kerning: 
  NativeHashMap<int, int> (nextGlyphIndex → kerning value)
```

### Mesh Output per String

Each rendered string produces TWO mesh representations:

| Mesh Type | Vertices | Triangles | Purpose |
|-----------|----------|-----------|---------|
| 2D Quad | 4 per char | 2 per char | Flat decal/billboard rendering |
| 3D Cube | 24 per char | 12 per char | Volumetric text with depth |

For a 10-character string:
- 2D: 40 vertices, 20 triangles, 40 UVs
- 3D: 240 vertices, 120 triangles, 240 UVs
- Colors: per-vertex (top/bottom gradient support)

## Integration with Rendering Pipeline

```mermaid
sequenceDiagram
    participant TUS as WETemplateUpdateSystem
    participant PRS as WEPostRendererSystem
    participant FS as FontServer / FontSystem
    participant RS as WERendererSystem

    TUS->>TUS: Set WEWaitingRendering on changed entities
    
    PRS->>PRS: Query entities with WEWaitingRendering
    PRS->>FS: FontSystem.DrawText(textValue)
    
    alt Text cached
        FS-->>PRS: IBasicRenderInformation (cached)
    else Text not cached
        FS-->>PRS: LOADING_PLACEHOLDER
        FS->>FS: Queue for next-frame job processing
    end
    
    PRS->>PRS: Store BRI reference in component
    PRS->>PRS: Disable WEWaitingRendering
    
    Note over RS: Next render callback
    RS->>RS: For each m_availToDraw entry
    RS->>RS: Get BRI from component
    RS->>RS: Mesh = BRI.GetMesh(shader, backface)
    RS->>RS: Graphics.DrawMesh(mesh, matrix, material)
```

## Configuration Parameters

| Setting | Default | Range | Effect |
|---------|---------|-------|--------|
| `StartTextureSizeFont` | 1 (1024×1024) | 0-4 (512 to 8192) | Initial atlas size |
| `FontQuality` | 2 (150%) | 0-7 (50%-800%) | Glyph rasterization resolution |

Quality size mapping: `QualitySize = [50, 100, 150, 200, 300, 400, 600, 800][FontQuality]`

Atlas size mapping: `AtlasSize = [512, 1024, 2048, 4096, 8192][StartTextureSizeFont]`
