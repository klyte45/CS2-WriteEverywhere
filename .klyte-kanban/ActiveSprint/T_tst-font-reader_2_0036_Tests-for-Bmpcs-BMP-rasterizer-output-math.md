**End time:** 2026-04-02 04:34 -0300
**Start time:** 2026-04-02 04:31 -0300
# [0036] Tests for Bmp.cs (BMP rasterizer output math)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the BMP rasterizer's pixel-output math**, so that I **glyph bitmap rendering correctness is verifiable without GPU context**.

---

## Background

[See epic: tst-font-reader](..\RefsLibrary\2026040123_testing-action-plan\epics\03_Epic_font-reader.md)

Task FR-05. Test the BMP rasterizer's pixel-output math: known coordinates produce expected values, edge-case (0,0), SDF calculation.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Font/FileReader/BmpTests.cs exists with >=15 test methods
- [x] Tests cover: known pixel coordinates produce expected output values, edge-case coordinates (0,0), SDF calculation for expected shapes

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


