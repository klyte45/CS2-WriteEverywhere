# Epic: `io-xml` — IO/XML Round-Trip Tests

## Objective

Test the serialization and deserialization layer in `IO/`. This is the code that saves and loads user-created text layouts to/from disk. Regressions here silently corrupt user data. The good news: the pure XML object-graph operations (serialization, deserialization, property access) are fully testable without `EntityManager` — only the `FromEntity()` / `ToEntity()` integration entry points are blocked by ECS.

This epic focuses on the "what goes in must come out identical" guarantee — round-trip fidelity, null-safety, and descriptor factory correctness.

---

## Target Files

| File | Tier | Est. Tests | Notes |
|---|---|---|---|
| `IO/WETextDataXml.cs` | B | 20–30 | XML serialization graph; round-trips; null safety |
| `IO/WETextDataXmlTree.cs` | B | 15–20 | Tree round-trip; `ShouldSerializechildren`; auto-Guid |
| `IO/WESelflessTextDataTree.cs` | B | 6–10 | Same XML pattern |
| `IO/WEComponentTypeDesc.cs` | B | 5–8 | `From(Type)` factory; buffer vs non-buffer |
| `IO/ObjFileHandler.cs` | B | 10–15 | `.obj` parsing; missing file; invalid float |
| `IO/WETextItemResume.cs` | S | 2 | DTO |
| `Font/Sprites/WEImageInfoXml.cs` | B | 5 | XML DTO; float2 fields |

Total: **~63–90 tests**.

---

## Task Drafts (7 tasks)

### IX-01 — Tests for `WETextDataXmlTree.cs` — round-trip fidelity
**Story:** As a developer, I want tests that verify the XML serialization of `WETextDataXmlTree` produces the exact same object graph when deserialized so that saved layouts are never silently corrupted.

**DoD checklist:**
- [ ] `IO/WETextDataXmlTreeTests.cs` exists with ≥15 test methods
- [ ] Tests cover: `ToXML()` → `FromXML()` produces equal object (deep comparison), serialized XML contains `<WELayout>`, serialized XML has `<self>` and `<children>` structure, `FromXML(null)` returns `null`, `FromXML("")` returns `null`, `children` is empty by default, `ShouldSerializechildren()` returns false when `self.layoutMesh != null`, `Guid` is auto-generated and non-empty, variables array round-trips correctly, `WETemplateVariable` key/value survives serialization

---

### IX-02 — Tests for `WETextDataXml.cs` — object graph and XML structure
**Story:** As a developer, I want tests that cover the full `WETextDataXml` XML serialization model (which is the actual data inside the WETextDataXmlTree) to ensure property bindings and XML attributes are correct.

**DoD checklist:**
- [ ] `IO/WETextDataXmlTests.cs` exists with ≥20 test methods
- [ ] Tests cover: property round-trips for all primitive fields, nested sub-tree serialization, `ToXML()` output is valid XML (`XDocument.Parse` does not throw), version field is present in XML, `FromXML()` on garbage XML returns null or throws `InvalidOperationException` (not `NullReferenceException`)

---

### IX-03 — Tests for `WESelflessTextDataTree.cs`
**Story:** As a developer, I want tests for the alternative XML tree format so that the secondary serialization path is equally regression-proof.

**DoD checklist:**
- [ ] `IO/WESelflessTextDataTreeTests.cs` exists with ≥6 tests
- [ ] Tests cover: round-trip fidelity, null safety on `FromXML`, structural XML element names

---

### IX-04 — Tests for `IO/WEComponentTypeDesc.cs`
**Story:** As a developer, I want tests for the `From(Type)` factory that classifies types as buffer vs non-buffer in the component descriptor system so that the formulae UI doesn't mis-classify component types.

**DoD checklist:**
- [ ] `IO/WEComponentTypeDescTests.cs` exists with ≥5 tests
- [ ] Tests cover: `From(typeof(int))` → `isBuffer == false`, `returnClassName == "System.Int32"`, `From(typeof(SomeIBufferElementData))` → `isBuffer == true`, `WEDescType == "COMPONENT"`
- [ ] Requires `Unity.Entities.dll` metadata reference for `IBufferElementData`; tests guarded with `#if GAME_DLLS_AVAILABLE` if needed

---

### IX-05 — Tests for `IO/ObjFileHandler.cs` — `.obj` file parsing
**Story:** As a developer, I want tests for the `.obj` mesh file parser so that user-created custom mesh imports are verified for correctness and null-safety.

**DoD checklist:**
- [ ] `IO/ObjFileHandlerTests.cs` exists with ≥10 tests
- [ ] Test fixtures: minimal valid `.obj` with 3 vertices and 1 triangle, cube `.obj` with 8 vertices 12 triangles
- [ ] Tests cover: vertex count, normal count, UV coordinate parsing, face index correctness, `ImportFromObj("nonexistent.obj")` returns null without exception, invalid float in `v` line → `FormatException` or graceful null
- [ ] Requires `UnityEngine.dll` metadata reference for `Vector3`/`Vector2` (or tests guarded)

---

### IX-06 — Tests for `Font/Sprites/WEImageInfoXml.cs` and `IO/WETextItemResume.cs`
**Story:** As a developer, I want tests for the remaining XML/DTO types to complete IO layer coverage.

**DoD checklist:**
- [ ] `Font/Sprites/WEImageInfoXmlTests.cs` exists: XML serialization round-trip for sprite info (name, rect, float2 values)
- [ ] `IO/WETextItemResumeTests.cs` exists: field assignment verification

---

### IX-07 — Tests for `MaxRectsBinPack.cs` (Font/Sprites)
**Story:** As a developer, I want tests for the MaxRects bin packing algorithm so that atlas allocation logic is verified for correctness (critical for font atlas slots being correctly assigned without overlap).

**DoD checklist:**
- [ ] `Font/Sprites/MaxRectsBinPackTests.cs` exists with ≥15 tests
- [ ] Tests cover: single rect insertion, multiple rects without overlap, bin overflow (no room), greedy heuristics, reset and re-use
- [ ] Requires `UnityEngine.dll` metadata reference for `Rect` struct (or tests guarded)

---

## Epic Acceptance Criteria

- [ ] `WETextDataXmlTree` and `WETextDataXml` round-trip tests pass
- [ ] `ObjFileHandler` parsing tests pass with embedded test `.obj` fixture
- [ ] All `FromXML(null)` and `FromXML(garbage)` cases handled (no NullReferenceException propagation)
- [ ] `dotnet test` passes with ≥60 more tests after this epic
