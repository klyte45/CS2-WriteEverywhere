**End time:** 2026-04-02 05:03 -0300
**Start time:** 2026-04-02 04:57 -0300
# [0039] Tests for FontAtlas.cs and FontGlyph.cs (atlas node management)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the skyline bin-packer node management in FontAtlas and the pure accessors in FontGlyph**, so that I **atlas layout correctness and glyph property calculations are regression-proof**.

---

## Background

[See epic: tst-font-reader](..\RefsLibrary\2026040123_testing-action-plan\epics\03_Epic_font-reader.md)

Task FR-08. Test FontAtlas skyline bin-packer (InsertNode, RemoveNode, Expand, AddSkylineLevel, AddRect, Version) and FontGlyph pure accessors.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Font/System/FontAtlasTests.cs exists with >=20 tests: InsertNode with array growth, RemoveNode with element shift, Expand (width-only, height-only), AddSkylineLevel node merge, AddRect success and overflow, Version increment on Reset()
- [x] Font/System/FontGlyphTests.cs exists with >=8 tests: PadFromBlur(0)==2, PadFromBlur(3)==5, xMax/yMax calculation, Null.IsValid==false, Font property setter/getter via GCHandle

---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


