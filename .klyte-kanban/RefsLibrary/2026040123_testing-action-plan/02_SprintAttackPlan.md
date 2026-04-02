# Sprint Attack Plan — BelzontWE Testing Implementation

> **Strategy:** Sprints are measured in task count, not time. Target: ~8 tasks per sprint.  
> A sprint may drop below 8 if remaining tasks in an epic would be fewer than 4 (merge into previous sprint) or form a coherent standalone group (short sprint allowed at ≥5).  
> Different epics can share a sprint only when the smaller subject contributes ≤3 tasks and they don't require conflicting code changes.

## Sprint Overview

| Sprint | Tasks | Epics | Status |
|---|---|---|---|
| [Sprint 1](#sprint-1) | 8 | `testing-infra` | First — no dependencies |
| [Sprint 2](#sprint-2) | 8 | `pure-logic` (7) + `font-reader` start (1) | After Sprint 1 |
| [Sprint 3](#sprint-3) | 8 | `font-reader` (7) + `component-data` start (1) | After Sprint 2 |
| [Sprint 4](#sprint-4) | 8 | `component-data` (6) + `io-xml` start (2) | After Sprint 3 |
| [Sprint 5](#sprint-5) | 7 | `io-xml` (5) + `builtin-fn` start (2) | After Sprint 4 |
| [Sprint 6](#sprint-6) | 8 | `builtin-fn` (5) + `seam-refactor` start (3) | After Sprint 5 |
| [Sprint 7](#sprint-7) | 7 | `seam-refactor` (4) + `formulae-engine` start (3) | After Sprint 6 |
| [Sprint 8](#sprint-8) | 7 | `formulae-engine` (4) + `seam-refactor` close (SR-07) (3) | After Sprint 7 |

**Total: 8 sprints, 61 tasks**

> **Note on Sprint 8:** SR-07 is placed at end of `seam-refactor` because it depends on FE work for `formulae-engine` tests. The feedback loop between `seam-refactor` and `formulae-engine` creates a natural dependency that extends these two epics across 3 sprints (6, 7, 8).

---

## Tier Legend (for reference in task rows)

| Tier | Meaning |
|---|---|
| **S** | Fully testable without mocking |
| **A** | Fully testable with mocking in some/all methods |
| **B** | Partially testable now, but can be fully testable after some refactor |
| **C** | Partially testable now and can raise coverage after some refactor |
| **D** | Not testable, but fully testable after some refactor |
| **E** | Not testable, but can be partially tested after some refactor |
| **F** | Impossible to test |
| **/** | Not Applicable (no methods to test) |

---

## Sprint 1

**Epic:** `testing-infra` — Test infrastructure setup  
**Goal:** A working `dotnet test` pipeline with zero test content.  
**Tasks: 8**

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | TI-01 | Create `BelzontWE.Tests` project file (`.csproj`, solution reference) | infra | `testing-infra` |
| 2 | TI-02 | Configure game DLL metadata references (`GameDllRefs.targets`) | infra | `testing-infra` |
| 3 | TI-03 | Add `InternalsVisibleTo` to `BelzontWE` | infra | `testing-infra` |
| 4 | TI-04 | Create test folder structure and naming conventions | infra | `testing-infra` |
| 5 | TI-05 | Add TTF fixture file (embedded resource, `TestFontFixture` helper) | infra | `testing-infra` |
| 6 | TI-06 | Integrate `dotnet test` into MSBuild workflow | infra | `testing-infra` |
| 7 | TI-07 | Create CI pipeline file (GitHub Actions / equivalent) | infra | `testing-infra` |
| 8 | TI-08 | Smoke test: validate pipeline end-to-end | infra | `testing-infra` |

**Sprint Definition of Done:**
- `dotnet test` runs from `MSBuild.exe BelzontWE.sln /p:Configuration=Debug`
- At least 1 test discovered and passes
- CI pipeline runs green on a test branch

---

## Sprint 2

**Epic:** `pure-logic` (7 tasks) + `font-reader` TI bridging task (1)  
**Goal:** Anchor all enum/constant/utility S-tier coverage; plant first font-reader file.  
**Tasks: 8**

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | PL-01 | Tests for all enum files (WEShader, WEMemberSource, WESimulationTextType, placement enums) | S | `pure-logic` |
| 2 | PL-02 | Tests for `WEBuiltinAttributes.cs` | S | `pure-logic` |
| 3 | PL-03 | Tests for `WEStringsBank.cs` (12–15 tests) | S | `pure-logic` |
| 4 | PL-04 | Tests for `WEVarsCacheBank.cs` | S | `pure-logic` |
| 5 | PL-05 | Tests for `WEConstants.cs` | S | `pure-logic` |
| 6 | PL-06 | Tests for `IO/WEStaticMethodDesc.cs` and `IO/WETypeMemberDesc.cs` | S | `pure-logic` |
| 7 | PL-07 | Tests for remaining IO DTOs (WETypeMathOperationDesc, WEXmlMetadata, WETextItemResume) | S | `pure-logic` |
| 8 | FR-01 | Tests for `FakePtr<T>` (20–30 tests) — font-reader epic first task | S | `font-reader` |

**Sprint Definition of Done:**
- All 7 S-tier pure-logic groups covered
- `WEStringsBank` has ≥12 passing tests
- `FakePtr` has ≥20 passing tests
- `dotnet test` passes with ≥100 tests total accumulated

---

## Sprint 3

**Epic:** `font-reader` (7 tasks) + `component-data` seed (1)  
**Goal:** Complete the stbtt port test suite; start component data.  
**Tasks: 8**

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | FR-02 | Tests for `Buf.cs` (25–40 tests) | S | `font-reader` |
| 2 | FR-03 | Tests for `CharStringContext.cs` | S | `font-reader` |
| 3 | FR-04 | Tests for `Common.cs` and `RectPackContext.cs` | S | `font-reader` |
| 4 | FR-05 | Tests for `Bmp.cs` | S | `font-reader` |
| 5 | FR-06 | Tests for `FontInfo.cs` (requires TTF fixture) | S | `font-reader` |
| 6 | FR-07 | Tests for `Font.cs`, `FontGlyphBounds.cs`, `FontAtlasNode.cs`, `FontCreationException.cs` | S/B | `font-reader` |
| 7 | FR-08 | Tests for `FontAtlas.cs` and `FontGlyph.cs` | B | `font-reader` |
| 8 | CD-01 | Tests for `WETextDataMaterial.cs` — float clamping (40–60 tests) | A | `component-data` |

**Sprint Definition of Done:**
- All 7 Font FileReader + System files have test coverage
- `FontInfo` tests pass with embedded TTF
- `FontAtlas` node-management suite has ≥20 tests
- `WETextDataMaterial` float-clamp suite has ≥40 tests
- `dotnet test` passes with ≥350 tests total accumulated

---

## Sprint 4

**Epic:** `component-data` (6 tasks) + `io-xml` seed (2)  
**Goal:** Complete WETextData component tests; start IO/XML.  
**Tasks: 8**

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | CD-02 | Tests for `WETextDataMaterial.cs` — Color properties + dirty flag | A | `component-data` |
| 2 | CD-03 | Tests for `WETextDataTransform.cs` (35–50 tests) | A | `component-data` |
| 3 | CD-04 | Tests for `WETextDataValueFloat/Int/Float3.cs` | B | `component-data` |
| 4 | CD-05 | Tests for `WETextDataValueString.cs` and `WETextDataValueColor.cs` | B | `component-data` |
| 5 | CD-06 | Tests for `WETextDataMesh.cs` | B | `component-data` |
| 6 | CD-07 | Tests for `WETextDataMain.cs` and `WETextDataVariable.cs` | C/B | `component-data` |
| 7 | IX-01 | Tests for `WETextDataXmlTree.cs` round-trip fidelity | B | `io-xml` |
| 8 | IX-02 | Tests for `WETextDataXml.cs` object graph | B | `io-xml` |

**Sprint Definition of Done:**
- All WETextData structs have test coverage
- `WETextDataTransform` pivot tests cover all enum values
- `WETextDataXmlTree` round-trip tests pass
- `dotnet test` passes with ≥480 tests total accumulated

---

## Sprint 5

**Epic:** `io-xml` (5 tasks) + `builtin-fn` seed (2)  
**Goal:** Complete IO/XML coverage; start BuiltinFn.  
**Tasks: 7**

> Short sprint (7 tasks): merging remaining `io-xml` (5) with first 2 `builtin-fn` tasks. Both epics are read-only production code — compatible in same sprint.

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | IX-03 | Tests for `WESelflessTextDataTree.cs` | B | `io-xml` |
| 2 | IX-04 | Tests for `IO/WEComponentTypeDesc.cs` | B | `io-xml` |
| 3 | IX-05 | Tests for `IO/ObjFileHandler.cs` (.obj parsing) | B | `io-xml` |
| 4 | IX-06 | Tests for `WEImageInfoXml.cs` and `WETextItemResume.cs` | B/S | `io-xml` |
| 5 | IX-07 | Tests for `MaxRectsBinPack.cs` | B | `io-xml` |
| 6 | BF-01 | Tests for `WEParameterFn.cs` — pure dictionary operations | S | `builtin-fn` |
| 7 | BF-02 | Tests for `WEVehicleFn.cs` — plate/serial binding seam | A | `builtin-fn` |

**Sprint Definition of Done:**
- All IO/XML files have round-trip tests
- `ObjFileHandler` has ≥10 parsing tests
- `WEParameterFn` fully covered (15+ tests)
- `WEVehicleFn` plate split and serial math tests pass
- `dotnet test` passes with ≥545 tests total accumulated

---

## Sprint 6

**Epic:** `builtin-fn` (5 tasks) + `seam-refactor` seed (3)  
**Goal:** Complete BuiltinFn coverage; begin seam infrastructure.  
**Tasks: 8**

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | BF-03 | Tests for `WEBuildingFn.cs` binding null-fallback | A | `builtin-fn` |
| 2 | BF-04 | Tests for `WERouteFn.cs` binding seam | A | `builtin-fn` |
| 3 | BF-05 | `WENumberFormattingFn` locale seam + tests | B→A | `builtin-fn` |
| 4 | BF-06 | Tests for `WECalendarFn.cs` partial binding | C | `builtin-fn` |
| 5 | BF-07 | Tests for `WEColorsFn.cs` review + tests if pure logic exists | E | `builtin-fn` |
| 6 | SR-01 | Extract `IECSReader` interface from EntityManager usage | infra | `seam-refactor` |
| 7 | SR-02 | Inject `IECSReader` into `WELayoutUtility.cs` + tests | D→B | `seam-refactor` |
| 8 | SR-03 | Separate pure XML logic from ECS in `WEXmlExtensions.cs` + tests | C→B | `seam-refactor` |

**Sprint Definition of Done:**
- All BuiltinFn binding-seam files have null-fallback tests
- `WENumberFormattingFn` formatting tests with locale seam pass
- `IECSReader` interface exists and `WELayoutUtility` uses it
- `WEXmlExtensions` pure logic is tested
- `dotnet test` passes with ≥620 tests total accumulated

---

## Sprint 7

**Epic:** `seam-refactor` (4 tasks) + `formulae-engine` seed (3)  
**Goal:** Complete main seam refactors; start formulae engine testing.  
**Tasks: 7**

> Short sprint (7 tasks): `seam-refactor` finishes with 4 tasks; `formulae-engine` starts with 3.

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | SR-04 | Inject `IECSReader` into `WETextDataValue*.UpdateEffectiveValue` | B→A | `seam-refactor` |
| 2 | SR-05 | Introduce binding delegates in `WECalendarFn.cs` | C→B | `seam-refactor` |
| 3 | SR-06 | Extract pure formula tokenizer from `WEFormulaeEvalCore.cs` | B→A | `seam-refactor` |
| 4 | FE-01 | Tests for `WEFormulaeEvalCore.cs` — tokenizer correctness | A | `formulae-engine` |
| 5 | FE-02 | Tests for `WEFormulaeEvalCore.cs` — evaluation dispatch | A | `formulae-engine` |
| 6 | FE-03 | Integration tests: formula registration → evaluation round-trip | A | `formulae-engine` |
| 7 | FE-04 | Extended tests for `WEFormulaeHelper.cs` — Emit binding | A | `formulae-engine` |

**Sprint Definition of Done:**
- `IECSReader` injected into `UpdateEffectiveValue` paths
- Formula tokenizer extractedfrom `WEFormulaeEvalCore`
- Tokenizer has ≥12 tests
- End-to-end formula round-trip test passes
- `dotnet test` passes with ≥680 tests total accumulated

---

## Sprint 8

**Epic:** `formulae-engine` (4 tasks) + `seam-refactor` close (SR-07) (3)  
**Goal:** Close both remaining epics; reach total test coverage target.  
**Tasks: 7**

| # | Task ID | Description | Tier Target | Epic |
|---|---|---|---|---|
| 1 | FE-05 | Tests for `WETextDataValue*.UpdateEffectiveValue` via IECSReader mock | A | `formulae-engine` |
| 2 | FE-06 | Error path tests: invalid formula handling | A | `formulae-engine` |
| 3 | FE-07 | Tests for `WEFormulaeController.cs` pure logic (or documented skip) | F/A | `formulae-engine` |
| 4 | SR-07 | Tests for `WEFormulaeHelper.cs` method discovery and Emit binding | A | `seam-refactor` |
| 5 | FE-EXT-01 | Regression test suite: manual game session validation document | manual | `formulae-engine` |
| 6 | CLOSE-01 | Coverage gap review: any B/C-tier files missed; plan next cycle | review | all |
| 7 | CLOSE-02 | Update `01_FileTestabilityMatrix.md` with actual achieved coverage | doc | all |

**Sprint Definition of Done:**
- All 8 epics marked complete
- ≥50 more tests added in this sprint
- `WEFormulaeHelper` method discovery tests pass
- Coverage gap review documented
- `dotnet test` passes with ≥720 tests total accumulated

---

## Task Count Summary

| Epic | Tasks | Sprints | Notes |
|---|---|---|---|
| `testing-infra` | 8 | Sprint 1 | Self-contained |
| `pure-logic` | 7 | Sprint 2 | Self-contained |
| `font-reader` | 8 | Sprints 2–3 | Started S2, completed S3 |
| `component-data` | 7 | Sprints 3–4 | Started S3, completed S4 |
| `io-xml` | 7 | Sprints 4–5 | Started S4, completed S5 |
| `builtin-fn` | 7 | Sprints 5–6 | Started S5, completed S6 |
| `seam-refactor` | 7 | Sprints 6–8 | Spread due to formulae dependency |
| `formulae-engine` | 7 | Sprints 7–8 | Depends on seam-refactor |
| **Totals** | **58** | **8** | |

> **2 closure tasks** (CLOSE-01, CLOSE-02) and 1 regression validation task (FE-EXT-01) bring the raw total to 61 task slots. These overlap Sprint 8.

## Cumulative Test Estimate

| After Sprint | Cumulative Tests |
|---|---|
| Sprint 1 | ~1 (smoke) |
| Sprint 2 | ~100–120 |
| Sprint 3 | ~320–400 |
| Sprint 4 | ~480–570 |
| Sprint 5 | ~545–640 |
| Sprint 6 | ~620–730 |
| Sprint 7 | ~680–810 |
| Sprint 8 | **~720–900** |
