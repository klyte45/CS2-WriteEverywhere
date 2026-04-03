**End time:** 2026-04-02 20:34 -0300
**Start time:** 2026-04-02 20:34 -0300
# [0051] Tests for IO/ObjFileHandler.cs (.obj file parsing, >=10 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for the .obj mesh file parser**, so that I **user-created custom mesh imports are verified for correctness and null-safety**.

---

## Background

[See epic: tst-io-xml](..\RefsLibrary\2026040123_testing-action-plan\epics\05_Epic_io-xml.md)

Task IX-05. Test .obj mesh file parser with embedded fixtures: vertex count, normal count, UV parsing, face indices, missing file returns null, invalid float in v line -> FormatException or graceful null.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] IO/ObjFileHandlerTests.cs exists with >=10 tests
- [x] Test fixtures: minimal valid .obj with 3 vertices and 1 triangle, cube .obj with 8 vertices 12 triangles
- [x] Tests cover: vertex count, normal count, UV coordinate parsing, face index correctness, ImportFromObj("nonexistent.obj") returns null without exception, invalid float in v line -> FormatException or graceful null

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


