# [0037] Tests for FontInfo.cs (full stbtt port, requires TTF fixture, >=40 tests)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the full stbtt port in FontInfo.cs**, so that I **critical font-decoding operations (glyph index lookup, vertical metrics, kerning, bounding box) are verified against a known font file**.

---

## Background

[See epic: tst-font-reader](..\RefsLibrary\2026040123_testing-action-plan\epics\03_Epic_font-reader.md)

Task FR-06. Test the full stbtt port in FontInfo.cs against the embedded TTF fixture: InitFont, VMetrics, FindGlyphIndex, ScaleForPixelHeight, GetGlyphHMetrics, GetGlyphBitmapBox, kerning pairs.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Font/FileReader/FontInfoTests.cs exists with >=40 test methods
- [ ] Tests cover: stbtt_InitFont success/failure, stbtt_GetFontVMetrics (ascent > 0, descent < 0), stbtt_FindGlyphIndex('A') non-zero, stbtt_FindGlyphIndex for non-existent codepoint returns 0, stbtt_ScaleForPixelHeight(16), stbtt_GetGlyphHMetrics advance > 0, stbtt_GetGlyphBitmapBox, kerning pairs
- [ ] TTF fixture from TI-05 is used (no internet download at test time)

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


