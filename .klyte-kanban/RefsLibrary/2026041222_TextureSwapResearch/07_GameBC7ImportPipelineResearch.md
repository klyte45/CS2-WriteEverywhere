# 07 — Game BC7 Import Pipeline: Source Code Research

> **Date**: 2026-04-15  
> **Purpose**: Deep investigation of how the game transforms PNG source files into BC7/VT-compatible textures in the asset editor. Triggered by skepticism about Decision 3 in `06_DecisionLog.md`, which incorrectly concluded that `PipelinePlugin.dll` is not available at runtime.
>
> **Key Finding**: `PipelinePlugin.dll` **IS** shipped with the game runtime. The complete BC7 pipeline is callable from mod code at runtime. Decision 3 is corrected at the end of this document.

---

## 1 — Architecture Overview

The game's texture import pipeline is split across three assemblies:

| Assembly | Location | Purpose |
|---|---|---|
| `Colossal.AssetPipeline.dll` | `Cities2_Data/Managed/` | High-level managed pipeline (importers, settings, mip generation) |
| `Colossal.AssetPipeline.Native.dll` | `Cities2_Data/Managed/` | Thin P/Invoke wrapper around native functions |
| `PipelinePlugin.dll` | `Cities2_Data/Plugins/x86_64/` | Native C++ implementation (BC7 encoder, image I/O, resize) |

**All three ship with the game runtime.** This was verified by searching the actual Steam installation at `Cities2_Data/Plugins/x86_64/PipelinePlugin.dll`.

---

## 2 — Verified Runtime Availability

| Component | Callable at Runtime? | Notes |
|---|---|---|
| `TextureImporter.Texture.CompressBC()` | **Yes** | In `Colossal.AssetPipeline.dll` (Managed/) |
| `NativeTextures.BlockCompress()` | **Yes** | In `Colossal.AssetPipeline.Native.dll` (P/Invoke) |
| `PipelinePlugin.dll` native library | **Yes** | In `Cities2_Data/Plugins/x86_64/` — Unity loads all `Plugins/` DLLs at startup |
| `AtlassingUtils.PreProcessData` | **Yes** | In `Colossal.IO.AssetDatabase.dll` (managed/Burst) |
| `VTTextureAsset.PreProcessData` | **Yes** | In `Colossal.IO.AssetDatabase.dll` (delegates to above) |

---

## 3 — Full PNG → BC7 Pipeline (Step by Step)

### Step 1: Entry Point — `DefaultTextureImporter`

**Source**: `Colossal.AssetPipeline/Importers/DefaultTextureImporter.cs`

The `DefaultTextureImporter` is registered for `.png`, `.tif`, `.tiff` extensions. Its `Import()` method drives the full pipeline:

```csharp
// Extension: [Extension(".png", ".tif", ".tiff")]
// Entry method:
public TextureImporter.Texture Import(ImportSettings importSettings, string filePath, ...)
```

`ImportSettings` defines the processing options. Three canonical preset factories exist:

| Preset | normalMap | linearTexture | compressBC | overrideCompressionFormat |
|--------|-----------|---------------|------------|---------------------------|
| `GetDefault()` | false | false | true | None (auto) |
| `GetNormal()` | true | false | true | **BC7** (override) |
| `GetLinear()` | false | true | true | None (auto) |

Default compression effort for all presets: **3**.

---

### Step 2: File Reading via `PipelinePlugin.dll`

**Source**: `Colossal.AssetPipeline.Native/NativeTextures.cs`

The PNG bytes are read using P/Invoke into `PipelinePlugin.dll`, bypassing Unity's own PNG loader:

```csharp
// Get metadata (width, height, channels, bpp, containsAlpha, fileFormat)
[DllImport("PipelinePlugin", EntryPoint = "tex_file_get_info")]
public static extern int FileGetInfo(IntPtr data, long dataSize, out ImageFileInfo info);

// Load raw pixel data (Force8Bpp flag to ensure RGBA8 output)
[DllImport("PipelinePlugin", EntryPoint = "tex_file_load")]
public static extern long FileLoad(IntPtr data, long dataSize, LoadFlags flags, IntPtr dst, long dstSize);
```

The output is a `NativeArray<byte>` containing raw RGBA8 pixels (width × height × 4 bytes). 16-bit is supported but rarely used.

---

### Step 3: Mip Chain Generation

**Source**: `TextureImporter.Texture.ComputeMips()` in `Colossal.AssetPipeline/Importers/TextureImporter.cs`

Mip chain is generated via `ImageResize` in `PipelinePlugin.dll`:

```csharp
[DllImport("PipelinePlugin", EntryPoint = "tex_image_resize")]
public static extern void ImageResize(IntPtr src, int srcWidth, int srcHeight, IntPtr dst,
    int dstWidth, int dstHeight, int channels, int bitsPerChannel,
    int srgb, int clamp, int alphaIsTransparency);
```

Each mip is half the width and height of the previous. The loop terminates when both dimensions reach 1. Result is a `List<NativeArray<byte>> rawMips` — one entry per mip level, all in RGBA8.

---

### Step 4: BC7 Compression — The Core Step

**Source**: `TextureImporter.Texture.CompressBC()` in `TextureImporter.cs`, lines 855–960

This is the critical method. Here is its full logic:

#### 4a. Format selection

```csharp
NativeTextures.BlockCompressionFormat blockCompressionFormat;
if (overrideCompressedFormat != BlockCompressionFormat.None)
    blockCompressionFormat = overrideCompressedFormat;
else
    blockCompressionFormat = normalMap ? BlockCompressionFormat.BC5 : BlockCompressionFormat.BC7;
```

- Default non-normal textures: **BC7**
- Normal maps without override: **BC5**
- Normal maps with `GetNormal()` preset (override = BC7): **BC7** (with channel remapping)

#### 4b. GraphicsFormat assignment

```csharp
// BC7:
compressedFormat = sRGB ? GraphicsFormat.RGBA_BC7_SRGB : GraphicsFormat.RGBA_BC7_UNorm;
// BC5:
compressedFormat = GraphicsFormat.RG_BC5_UNorm;
// BC1 (DXT1):
compressedFormat = sRGB ? GraphicsFormat.RGB_DXT1_SRGB : GraphicsFormat.RGB_DXT1_UNorm;
```

#### 4c. Normal map channel remapping (BC7 override path)

When `normalMap == true` AND `blockCompressionFormat == BC7`, the game first remaps channels before encoding:

```csharp
// Remap: pack X→Alpha, Y→Green, discard Z (reconstructed by GPU)
nativeArray3[i]     = 255;           // R = 255
nativeArray3[i + 1] = original.G;   // G = Y component
nativeArray3[i + 2] = original.G;   // B = Y (duplicated for perceptual quality)
nativeArray3[i + 3] = original.R;   // A = X component
```

This packs the tangent-space normal (X, Y) into (Alpha, Green) — the same layout HDRP uses for reconstructing Z = sqrt(1 - X² - Y²) in the shader.

#### 4d. Block allocation and native encode call

For each mip:
```csharp
int outputWidth  = (mipW + 3) / 4;  // round up to 4x4 BC blocks
int outputHeight = (mipH + 3) / 4;
int outputBytes  = outputWidth * outputHeight * 16; // 16 bytes per BC7 block

NativeTextures.BlockCompress(
    src:    rawPixelPtr,
    width:  mipW,
    height: mipH,
    dst:    outputPtr,
    format: blockCompressionFormat,
    flags:  sRGB ? BlockCompressionFlags.Perceptual : BlockCompressionFlags.None,
    effort: 3
);
```

The P/Invoke binds to `tex_image_block_compress` in `PipelinePlugin.dll`. This is a CPU-side BC7 encoder (not GPU — the same encoder runs on your machine during asset builds and during runtime loads of new textures).

Result: `List<NativeArray<byte>> compressedMips` — one entry per mip, each in raw BC7 block format.

---

### Step 5: Serialization into TextureAsset

**Source**: `Colossal.IO.AssetDatabase/TextureAsset.cs`

The `TextureAsset.SetData(TextureImporter.ITexture)` method extracts the compressed mip chain:

```csharp
// Copies compressedMips[i] per-mip into a single contiguous NativeArray<byte>
// m_Format = RGBA_BC7_SRGB (or variant)
// m_Width, m_Height, m_MipsCount stored as metadata
// Written to disk as binary .Texture asset
```

The raw BC7 bytes are the **only data stored** — the RGBA8 source is discarded. The disk format is: `[metadata header] [mip0 BC7 bytes] [mip1 BC7 bytes] ... [mipN BC7 bytes]`.

---

### Step 6: VT Preprocessing — `AtlassingUtils.PreProcessData`

**Source**: `Colossal.IO.AssetDatabase/VirtualTexturing/AtlassingUtils.cs`

This step only applies to `VTTextureAsset` (VT-registered textures). It converts the flat BC7 mipchain into the VT page-streaming layout.

Input: raw BC7 byte buffer (from `TextureAsset.rawData`)  
Output: VT tile buffer, each tile padded with 8px overlap borders

```csharp
// The input must already be BC7 compressed — this method just reorganizes blocks
AtlassingUtils.PreProcessData(
    data:                 bc7Bytes,     // NativeSlice<byte>
    out processedData,                  // NativeArray<byte>
    textureWidth:         W,
    textureHeight:        H,
    preprocessedTileSize: tileSize,     // typically 512
    maxLevel:             mipCount-1,
    paddingSize:          8,            // 8px border per tile
    layerInfo:            new LayerInfo(tileSize, GraphicsFormat.RGBA_BC7_SRGB)
);
```

`LayerInfo` is constructed from the GraphicsFormat. For BC7: `blockWidthInPixels = 4`, `blockHeightInPixels = 4`, `blockSizeInBytes = 16`. It computes tile geometry from these values. The `LayerInfo` constructor does not perform any compression — it simply computes geometric constants.

The final tiles are zstd-compressed individually and written to the `.VTTexture` asset file.

---

## 4 — Per-Channel Format Reference for WE Atlases

| WE Layer | Usage | sRGB | normalMap | Expected Output Format |
|---|---|---|---|---|
| `_BaseColorMap` | Color + Alpha | true | false | `RGBA_BC7_SRGB` |
| `_EmissiveColorMap` | Emissive color | true | false | `RGBA_BC7_SRGB` |
| `_MaskMap` | Metallic/AO/Detail | false | false | `RGBA_BC7_UNorm` |
| `_ControlMask` | Custom control | false | false | `RGBA_BC7_UNorm` |
| `_NormalMap` | Tangent-space normal | false | false* | `RGBA_BC7_UNorm` |

> *For the `_NormalMap` layer in Phase 1, use `normalMap=false` (no channel swap) with `RGBA_BC7_UNorm`. The existing WE normal data is stored as standard RGBA — applying the game's BC7-normal channel remapping would corrupt it unless WE is also updated to pack/unpack X→Alpha. Leave normal channel remapping for Phase 2 investigation.

---

## 5 — How to Use the Pipeline from Mod Code

Given the confirmed runtime availability, the correct BC7 compression path for WETextureAtlas is:

```csharp
// Requires: atlas Texture2D is readable (RGBA32, Apply(false, false))
// Adding reference: Colossal.AssetPipeline.dll + Colossal.AssetPipeline.Native.dll

using Colossal.AssetPipeline.Importers;
using UnityEngine.Experimental.Rendering;

// Step 1: Wrap Unity Texture2D into the game's pipeline Texture
var tex = new TextureImporter.Texture("atlasName", "dummy_path", sourceTexture2D);

// Step 2: Generate mips (optional — skip if VT Phase 2 not needed, the atlas is binpacked)
// tex.ComputeMips(wrapClamp: true, alphaIsTransparency: true);

// Step 3: Compress to BC7 using the same encoder as the game
bool isLinear = !isSRGB;
tex.normalMap = false;
tex.sRGB = !isLinear;   // Note: sRGB is set by the Texture() constructor
                         //       (true if sRGB was passed to constructor)
tex.CompressBC(effort: 3);
// tex.compressedMips[0] => NativeArray<byte> with raw BC7 blocks (RGBA_BC7_SRGB or UNorm)
// tex.compressedFormat  => GraphicsFormat.RGBA_BC7_SRGB (or _UNorm)

// Step 4: Upload to a new Texture2D
var bc7Tex = new Texture2D(w, h, tex.compressedFormat,
    TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
bc7Tex.SetPixelData(tex.compressedMips[0], mipLevel: 0);
bc7Tex.Apply(false);  // No makeNoLongerReadable — keep GPU state correct

// Step 5: Cleanup
tex.Dispose();        // Frees NativeArrays (rawMips + compressedMips)
UnityEngine.Object.Destroy(sourceTexture2D);
```

> **Constructor note**: The `TextureImporter.Texture(string name, string path, Texture2D unityTexture)` constructor  
> (a) copies `GetPixelData<byte>(0)` into `rawMips[0]`  
> (b) infers `sRGB` from `unityTexture.graphicsFormat`  
> (c) requires the source Texture2D to be **readable** (non-compressed, CPU-accessible)

---

## 6 — Correction to Decision 3 in `06_DecisionLog.md`

Decision 3 stated:

> *"Research finding: The game's offline compression pipeline uses `NativeTextures.BlockCompress` → `PipelinePlugin.dll`, which **is not shipped with the game runtime** — cannot be called from a mod at runtime."*

**This is incorrect.** A direct filesystem search of the game's Steam installation confirmed:

```
C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\
    Cities2_Data\
        Managed\
            Colossal.AssetPipeline.dll         ✓ present
            Colossal.AssetPipeline.Native.dll  ✓ present
        Plugins\
            x86_64\
                PipelinePlugin.dll             ✓ present  ← was believed absent
```

Unity loads all DLLs in `Cities2_Data/Plugins/x86_64/` automatically at startup. `PipelinePlugin.dll` is therefore available to any mod that calls `NativeTextures.BlockCompress()` via `Colossal.AssetPipeline.Native`.

**Corrected decision**:

> Phase 1 BC7 compression uses **`TextureImporter.Texture.CompressBC(effort: 3)`** — the same CPU-side BC7 encoder (`PipelinePlugin.dll`) the game uses for its own asset editor. This guarantees format-identical output at identical compression quality. `Texture2D.Compress()` (GPU/driver-based) is no longer the proposed path.

**Impact on T2 in `05_SprintRoadmap.md`**: See Section 7 below.

---

## 7 — Required T2 Spec Update

Old spec excerpt (to be replaced in `05_SprintRoadmap.md`):

> - Research confirmed: the game's offline asset pipeline uses `NativeTextures.BlockCompress` → `PipelinePlugin.dll`, which is **not shipped with the game runtime** — cannot be called from a mod at runtime.  
> - Runtime BC7 path: Unity's built-in `Texture2D.Compress(highQuality: true)` — calls the GPU driver's block encoder; produces BC7 on PC where supported.

New spec (corrected):

> - Research confirmed: **`PipelinePlugin.dll` is shipped** in `Cities2_Data/Plugins/x86_64/`. Both `Colossal.AssetPipeline.dll` and `Colossal.AssetPipeline.Native.dll` are in `Cities2_Data/Managed/`. The full BC7 pipeline is callable from mod code at runtime.
> - Runtime BC7 path: **`TextureImporter.Texture.CompressBC(effort: 3)`** — wraps `NativeTextures.BlockCompress()` (P/Invoke into `PipelinePlugin.dll`). This is the exact same CPU-side encoder the game uses for its own assets. `Texture2D.Compress()` (Unity built-in, GPU/driver-based) is **not** used — it produces implementation-defined quality that may differ across GPU vendors and driver versions.
> - **How to use**: Construct a `TextureImporter.Texture` from the readable RGBA32 atlas `Texture2D`, call `CompressBC(effort: 3)`, then upload the `compressedMips[0]` bytes into a new `Texture2D` with `GraphicsFormat.RGBA_BC7_SRGB` (or `_UNorm` for linear layers). Dispose the pipeline Texture when done.

Also update T2 DoD:

Old: `CompressToBC7(Texture2D source, bool linear)` returns `byte[]` (via `Texture2D.Compress` + `GetRawTextureData`)

New: `CompressToBC7(Texture2D source, bool linear)` returns `byte[]` (via `TextureImporter.Texture.CompressBC(effort: 3)` — game's own BC7 encoder)

---

## 8 — Source File Reference

| File | Relevance |
|---|---|
| `Colossal.AssetPipeline/Importers/DefaultTextureImporter.cs` | Entry point; orchestrates Steps 1-4 |
| `Colossal.AssetPipeline/Importers/TextureImporter.cs` | `CompressBC()` implementation; `TextureImporter.Texture` class |
| `Colossal.AssetPipeline.Native/NativeTextures.cs` | P/Invoke declarations for `PipelinePlugin.dll` |
| `Colossal.AssetPipeline/Settings.cs` | Default import settings presets (Default/Normal/Linear) |
| `Colossal.IO.AssetDatabase/TextureAsset.cs` | Asset serialization; `SetData(ITexture)` |
| `Colossal.IO.AssetDatabase/VTTextureAsset.cs` | VT preprocessing; calls `AtlassingUtils.PreProcessData` |
| `Colossal.IO.AssetDatabase/VirtualTexturing/AtlassingUtils.cs` | VT tile layout; runtime-callable (managed/Burst) |
