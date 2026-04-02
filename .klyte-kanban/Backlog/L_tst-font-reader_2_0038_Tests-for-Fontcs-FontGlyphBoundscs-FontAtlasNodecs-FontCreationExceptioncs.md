# [0038] Tests for Font.cs, FontGlyphBounds.cs, FontAtlasNode.cs, FontCreationException.cs

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests for the high-level Font wrapper and its trivial supporting types**, so that I **calling code has a type-safe contract**.

---

## Background

[See epic: tst-font-reader](..\RefsLibrary\2026040123_testing-action-plan\epics\03_Epic_font-reader.md)

Task FR-07. Test Font.FromMemory, FontCreationException propagation, FontGlyphBounds field + ToString(), FontAtlasNode plain data struct.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Font/System/FontTests.cs exists with >=10 tests: FromMemory valid TTF, FromMemory garbage bytes throws FontCreationException, GetGlyphIndex('A') non-zero, Recalculate(48) sets positive Ascent
- [ ] Font/System/FontGlyphBoundsTests.cs covers field assignment and ToString() format
- [ ] Font/System/FontCreationExceptionTests.cs verifies message propagation

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


