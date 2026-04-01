# [0010] Shared Per-Font Kerning Table

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/03_FontProcessing/02_ImprovementAnalysis.md — Area 5

## User Story

> Acting as **a mod developer optimizing Write Everywhere's memory usage**, I want **each font's kerning data stored in a single font-level NativeHashMap instead of per-glyph maps**, so that I **memory allocation is reduced and disposal is simplified**.

---

## Background

Each FontGlyph currently owns a NativeHashMap<int, int> for its kerning pair values, lazily populated via GetKerningCached(). For a font with 600 glyphs, this creates up to 600 NativeHashMap allocations. Most are sparsely populated or entirely empty (kerning is primarily relevant for a subset of Latin glyphs; CJK fonts typically have no kerning).

The proposed improvement replaces N per-glyph maps with a single font-level map using a long key encoding (left glyph index << 32 | right glyph index).

Benefits: One allocation per font instead of one per glyph; Trivially disposed with a single Dispose() call; Better cache locality during rendering (all kerning data in one contiguous structure).

---

## Definition of Ready (DoR)

- [ ] FontGlyph.cs (or the struct/class containing NativeHashMap<int, int> kerning map) is located and read
- [ ] GetKerningCached() call sites are identified — confirm all callers access the map via the glyph instance
- [ ] FontSystem.cs (or FontAtlas.cs) disposal path is read to understand how glyph maps are currently disposed
- [ ] The glyph index type is confirmed (int32 on both sides, so long encoding is safe)

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Per-glyph NativeHashMap<int, int> kerning maps are removed from FontGlyph
- [ ] A single NativeHashMap<long, int> is added to FontSystem (or FontAtlas) as the font-level kerning table
- [ ] The key encoding formula ((long)left << 32) | (uint)right is documented in a code comment
- [ ] GetKerningCached() logic is updated to use the font-level map (lookup and lazy-populate via the long key)
- [ ] All callers of GetKerningCached() are updated to pass the font-level map (or access it via a reference to FontSystem)
- [ ] The font-level kerning map is correctly disposed when the font is unloaded/disposed
- [ ] Kerning rendering output is visually identical to before (glyph spacing unchanged)
- [ ] The mod compiles and loads without errors in CS2 v1.5.6

---

## Implementation Notes

1. Add NativeHashMap<long, int> m_kerningTable to FontSystem, initialized in OnCreate with a reasonable initial capacity (e.g., 512)
2. In GetKerningCached() (wherever it currently lives), replace the per-glyph map lookup with the long key encoding pattern
3. Remove the NativeHashMap<int, int> field and its initialization/disposal from FontGlyph
4. In FontSystem.Dispose(), add m_kerningTable.Dispose()

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Long key encoding collision (two different glyph pairs produce the same key) | None | 32-bit left-shift encoding is collision-free for int32 indices |
| FontGlyph is used as a Burst-compatible struct (field removal changes layout) | Low | FontGlyph is a managed class or its native fields don't affect Burst jobs; verify |
| Initial map capacity causes allocator growth | Very low | Pre-size to 512; typical Latin fonts have <200 kerning pairs |

---

## Related Tasks

### Depends on



### Is dependent for


