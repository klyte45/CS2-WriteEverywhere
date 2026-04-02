# Epic: `font-reader` — Font FileReader Test Suite

## Objective

Build a comprehensive test suite for the `Font/FileReader/` subsystem — a port of the stbtt (stb_truetype) C library to C#. This code is a pure-C# parsing engine with **no Unity or game dependencies**, making it one of the highest-value test targets in the repo. Bugs here cause silent font rendering corruption or crashes; tests here run in milliseconds and catch port-correctness issues.

The epic also covers the `Font/System/` classes that do not touch the GPU (`FontAtlas`, `FontGlyph`, `Font`, `FontAtlasNode`, `FontGlyphBounds`, `FontCreationException`, `Bounds`).

---

## Target Files

| File | Tier | Est. Tests | Notes |
|---|---|---|---|
| `Font/FileReader/FakePtr.cs` | S | 20–30 | Pointer abstraction — flagship target |
| `Font/FileReader/Buf.cs` | S | 25–40 | Buffer cursor + big-endian reads |
| `Font/FileReader/Common.cs` | S | 25–40 | Platform/encoding constants + codepoint utilities |
| `Font/FileReader/CharStringContext.cs` | S | 8–12 | CFF charstring state machine |
| `Font/FileReader/RectPackContext.cs` | S | 20–30 | Rect-packing algorithm |
| `Font/FileReader/Bmp.cs` | S | 15–25 | BMP rasterizer output |
| `Font/FileReader/FontInfo.cs` | S | 40–60 | Full stbtt port (high-value, needs TTF fixture) |
| `Font/System/FontAtlasNode.cs` | S | 2 | Plain data struct |
| `Font/System/FontCreationException.cs` | S | 2 | Standard exception |
| `Font/System/FontGlyphBounds.cs` | S | 5 | Pure struct with `ToString()` |
| `Font/System/Bounds.cs` | S → / | 1 | Near-trivial |
| `Font/System/Font.cs` | B | 10–15 | Pure stbtt wrapper; needs TTF fixture |
| `Font/System/FontAtlas.cs` | B | 20–30 | Node management pure; GPU methods excluded |
| `Font/System/FontGlyph.cs` | B | 8–12 | Pure accessors; `GetKerning` blocked |

Total: **~201–334 test cases** — the largest single epic by test count.

---

## Dependencies

- TTF fixture file (from `testing-infra` epic, task TI-05)
- `Unity.Mathematics` NuGet (for any `float3` types in `FontGlyph`)
- Game DLL reference strategy (TI-02) not required for most of these — they are truly native C#

---

## Task Drafts (8 tasks)

### FR-01 — Tests for `FakePtr<T>`
**Story:** As a developer, I want exhaustive tests for the `FakePtr<T>` pointer abstraction so that the foundation of the entire stbtt port is verified correct.

**DoD checklist:**
- [ ] `Font/FileReader/FakePtrTests.cs` exists with ≥20 test methods
- [ ] Tests cover: construct from array, construct with offset, `Value` getter/setter, `GetAndIncrease`/`SetAndIncrease`, `Clear(n)`, indexer read/write with int and long, null pointer (`FakePtr<T>.Null.IsNull == true`), shared array mutation, arithmetic `ptr + n`, and copy constructor offset propagation
- [ ] At least one test verifies that accessing `FakePtr<T>.Null` throws `NullReferenceException` or `IndexOutOfRangeException`

---

### FR-02 — Tests for `Buf.cs`
**Story:** As a developer, I want tests for every buffer navigation and parsing method in `Buf` so that the binary parsing layer that drives TTF/CFF decoding is regression-proof.

**DoD checklist:**
- [ ] `Font/FileReader/BufTests.cs` exists with ≥25 test methods
- [ ] Tests cover: construction (size, cursor=0), `stbtt__buf_get8()` normal read and past-end return, `stbtt__buf_peek8()` without cursor advance, `stbtt__buf_seek()` exact/negative/past-end clamping, `stbtt__buf_skip()`, `stbtt__buf_get(1/2/4)` big-endian reads, `stbtt__buf_range()` valid and invalid, `stbtt__cff_int()` for all op-code ranges (32–246, 247–250, 251–254, 255, 28, 29)

---

### FR-03 — Tests for `CharStringContext.cs`
**Story:** As a developer, I want tests for the CFF charstring execution context so that glyph outline decoding state machine behavior is anchored.

**DoD checklist:**
- [ ] `Font/FileReader/CharStringContextTests.cs` exists with ≥8 test methods
- [ ] Tests cover: initial state (stack empty, depth=0), stack push/pop operations, limit enforcement, context reset

---

### FR-04 — Tests for `Common.cs` and `RectPackContext.cs`
**Story:** As a developer, I want tests for the platform/encoding constants in `Common.cs` and the rect-packing geometry in `RectPackContext.cs` so that the lower-level font metadata parsing has anchored contracts.

**DoD checklist:**
- [ ] `Font/FileReader/CommonTests.cs` exists with ≥20 tests verifying platform ID constants, encoding IDs, language IDs, and any codepoint utility functions
- [ ] `Font/FileReader/RectPackContextTests.cs` exists with ≥20 tests covering rect insertion, packing correctness, and overflow behavior

---

### FR-05 — Tests for `Bmp.cs`
**Story:** As a developer, I want tests for the BMP rasterizer's pixel-output math in `Bmp.cs` so that glyph bitmap rendering correctness is verifiable without GPU context.

**DoD checklist:**
- [ ] `Font/FileReader/BmpTests.cs` exists with ≥15 test methods
- [ ] Tests cover: known pixel coordinates produce expected output values, edge-case coordinates (0,0), SDF calculation for expected shapes

---

### FR-06 — Tests for `FontInfo.cs` (requires TTF fixture)
**Story:** As a developer, I want tests for the full stbtt port in `FontInfo.cs` so that critical font-decoding operations (glyph index lookup, vertical metrics, kerning, bounding box) are verified against a known font file.

**DoD checklist:**
- [ ] `Font/FileReader/FontInfoTests.cs` exists with ≥40 test methods
- [ ] Tests cover: `stbtt_InitFont` success/failure, `stbtt_GetFontVMetrics` (ascent > 0, descent < 0), `stbtt_FindGlyphIndex('A')` non-zero, `stbtt_FindGlyphIndex` for non-existent codepoint returns 0, `stbtt_ScaleForPixelHeight(16)` in plausible range, `stbtt_GetGlyphHMetrics` advance > 0, `stbtt_GetGlyphBitmapBox` non-zero for visible glyph, `stbtt_GetGlyphKernAdvance` for known kerning pair
- [ ] TTF fixture from TI-05 is used (no internet download at test time)

---

### FR-07 — Tests for `Font/System/Font.cs`, `FontGlyphBounds.cs`, `FontAtlasNode.cs`, `FontCreationException.cs`
**Story:** As a developer, I want tests for the high-level Font wrapper and its trivial supporting types so that calling code has a type-safe contract.

**DoD checklist:**
- [ ] `Font/System/FontTests.cs` exists with ≥10 tests: `FromMemory` with valid TTF, with garbage bytes (throws `FontCreationException`), `GetGlyphIndex('A')` non-zero, `Recalculate(48)` sets positive `Ascent`
- [ ] `Font/System/FontGlyphBoundsTests.cs` covers field assignment and `ToString()` format
- [ ] `Font/System/FontCreationExceptionTests.cs` verifies message propagation

---

### FR-08 — Tests for `Font/System/FontAtlas.cs` and `FontGlyph.cs`
**Story:** As a developer, I want tests for the skyline bin-packer node management in `FontAtlas` and the pure accessors in `FontGlyph` so that atlas layout correctness and glyph property calculations are regression-proof.

**DoD checklist:**
- [ ] `Font/System/FontAtlasTests.cs` exists with ≥20 tests: `InsertNode` with array growth, `RemoveNode` with element shift, `Expand` (width-only, height-only), `AddSkylineLevel` node merge, `AddRect` success and overflow, `Version` increment on `Reset()`
- [ ] `Font/System/FontGlyphTests.cs` exists with ≥8 tests: `PadFromBlur(0) == 2`, `PadFromBlur(3) == 5`, `xMax/yMax` calculation, `Null.IsValid == false`, `Font` property setter/getter via `GCHandle`

---

## Epic Acceptance Criteria

- [ ] All 14 target files have at least one test file
- [ ] `FontInfo` tests pass with the embedded TTF fixture
- [ ] `FontAtlas` node-management suite passes (≥20 tests)
- [ ] `FakePtr` and `Buf` suites each pass with ≥20 and ≥25 tests respectively
- [ ] No production code changes (other than `InternalsVisibleTo` from `testing-infra`)
