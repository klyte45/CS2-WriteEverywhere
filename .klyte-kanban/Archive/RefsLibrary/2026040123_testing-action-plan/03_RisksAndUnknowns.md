# Risks and Unknowns — BelzontWE Testing Implementation

> This document captures the technical risks, open questions, and uncertainties that were identified during the research phase and remain unresolved at the time of planning. Each risk includes a likelihood / impact rating, current blockers, and a suggested mitigation.

---

## Risk Ratings

| Rating | Meaning |
|---|---|
| 🔴 **High** | Likely to block a sprint if unresolved |
| 🟡 **Medium** | May require extra effort or workaround but not a full blocker |
| 🟢 **Low** | Known risk; unlikely to materialize; monitored |

---

## R1 — Game DLL reference strategy may fail on developer machines without game install

**Category:** Infrastructure  
**Likelihood:** 🟡 Medium  
**Impact:** 🔴 High — Any test referencing `UnityEngine.dll`, `Unity.Entities.dll`, or `Unity.Collections.dll` fails to compile on machines without the game installed.

**Details:**  
Many B/A-tier tests (WETextDataMaterial, WETextDataTransform, WEParameterFn, etc.) require the game's managed DLLs as metadata-only references. These DLLs are not available on NuGet and are licensed to be used only by game modders. CI/CD systems (GitHub Actions, etc.) will never have them installed.

**Current State:**  
Unknown — no configuration exists yet. This is the first question Sprint 1 (TI-02) must answer.

**Mitigations:**
1. Use `#if GAME_DLLS_AVAILABLE` preprocessor symbol: define it only when the path resolves; guard affected tests with this symbol. CI skips guarded tests cleanly.
2. Use a `Directory.Build.props` local override (not committed) for the game path; document the setup in README.
3. Evaluate whether `Unity.Mathematics` NuGet package (https://www.nuget.org/packages/Unity.Mathematics) provides enough of the Unity math types to avoid the game DLL ref for math-only tests.
4. For continuous integration: document that game-DLL-dependent tests only run locally, and tag them with `[Category("RequiresGameDLL")]` so they can be filtered with `dotnet test --filter Category!=RequiresGameDLL`.

**Unknown:** Which tests will actually require game DLLs vs which can be isolated. Must be validated empirically in TI-02.

---

## R2 — `System.Reflection.Emit` behavior in .NET Framework 4.7.2 may differ from expected

**Category:** Technical / Formulae Engine  
**Likelihood:** 🟡 Medium  
**Impact:** 🟡 Medium — `WEFormulaeHelper.cs` uses heavy Reflection.Emit for delegate generation. Tests that exercise this path may fail silently or with obscure IL validation errors in the test runner context.

**Details:**  
The game targets `.NET Framework 4.7.2`. `System.Reflection.Emit` has known differences between .NET Framework and .NET 6+. If the test project targets a different runtime (e.g., `net6.0`) due to developer preference, the Emit-generated delegates may behave differently or fail safety checks.

**Mitigations:**
1. Ensure `BelzontWE.Tests.csproj` targets `net472` (same as production) — this is specified in TI-01 but must be enforced.
2. If a cross-platform test runner is preferred, keep `net472` but investigate `Mono` compatibility on non-Windows machines.
3. Add explicit verification in FE-04 (WEFormulaeHelper Emit binding tests) that the generated delegate is not null and is callable — not just that registration didn't throw.

**Unknown:** Whether the Reflection.Emit path in `WEFormulaeHelper` accesses any game types. If it does, the formula registration tests may be partially blocked.

---

## R3 — TTF fixture legal risk (font licensing)

**Category:** Legal / Infrastructure  
**Likelihood:** 🟢 Low  
**Impact:** 🟡 Medium — Using a font file with a restrictive license as a test fixture could create licensing complications.

**Details:**  
The test suite for `FontInfo.cs` and `Font.cs` requires a real TTF file embedded in the test assembly. Using the game's bundled fonts or the mod's bundled `SourceSansPro` without verifying the license is risky.

**Mitigations:**
1. Use a font explicitly released under **SIL Open Font License (OFL)** or **CC0** — e.g., `Noto Sans Regular`, `GNU FreeMono`, or a font from Google Fonts' OFL-licensed set.
2. Place the font in `BelzontWE.Tests/Fixtures/Fonts/` with a `LICENSE.txt` confirming terms.
3. Keep the fixture file as small as possible (Latin character subset fonts are typically 20–60 KB).

**Unknown:** None — this is a well-understood risk with a clear resolution path.

---

## R4 — `Unity.Collections` native allocator blocks `NativeArray<T>` test access

**Category:** Technical  
**Likelihood:** 🔴 High (certain to affect some tests)  
**Impact:** 🟡 Medium — Tests for `FontGlyph.GetKerning`, `StringRenderingJob`, `FontSystemData` are blocked until Unity native allocator is available.

**Details:**  
`NativeArray<T>`, `NativeReference<T>`, `NativeHashMap<K,V>` (from `Unity.Collections`) require Unity's `DisposeSentinel` and `AtomicSafetyHandle` safety system, which initializes at Unity engine startup. In a plain `dotnet test` process, the first allocation call throws `InvalidOperationException`.

This is by design and is a known limitation of Unity's ECS runtime — it cannot be shimmed without rewriting Unity internals.

**Impact scope:**  
- `Font/System/FontGlyph.cs` — `GetKerning(NativeHashMap<long,int>)` → blocked
- `Font/System/StringRenderingJob.cs` — completely blocked (Burst+NativeArray)
- All 10 ECS System files — completely blocked (F-tier)

**Mitigations:**
1. Accept these methods as **out of scope** for Plan 1 (plain `dotnet test`). Tier them as B/F in the matrix (already done).
2. Long-term option: Unity Editor Test Framework (Plan 3 from research). Would require setting up the Unity Editor project — significantly more infrastructure.
3. Do not attempt to shim `NativeArray` — it is fragile and unsupported outside Unity.

**Unknown:** Whether `Unity.Collections` has a "no-safety" mode that works outside the Unity runtime. Some community reports mention `Allocator.None` or `unsafe` allocations — unverified for this codebase.

---

## R5 — `WEModData.InstanceWE` singleton dependency scattered through codebase

**Category:** Technical  
**Likelihood:** 🔴 High (affects multiple B/C-tier files)  
**Impact:** 🟡 Medium — `WEModData.InstanceWE.FormatCulture`, `WEModData.InstanceWE.ModFolder`, etc. appear in formatting and IO helpers. Accessing them in tests throws `NullReferenceException`.

**Details:**  
`WEModData.InstanceWE` is set during mod initialization. In tests, it is `null`. Files that call it directly without a null guard fail at runtime, not compile time.

**Files affected:**
- `WENumberFormattingFn.cs` (FormatCulture)
- `WEAssetsSettingsLoaderUtility.cs` (ModFolder)
- Possibly `WEFormulaeEvalCore.cs` (indirect)

**Mitigations:**
1. **For `WENumberFormattingFn`:** The locale seam (BF-05 in `builtin-fn` epic) directly addresses this — a `static Func<CultureInfo> _cultureProvider` field is introduced that defaults to the production value and is overridden in tests to `CultureInfo.InvariantCulture`.
2. **For `WEAssetsSettingsLoaderUtility`:** Avoid testing file-system-bound methods in this epic cycle. The class is F-tier.
3. **General pattern:** Whenever a new static singleton call is found, apply the same static-Func-override pattern before writing tests.

**Unknown:** Whether `WEModData.InstanceWE` is accessed in `WEFormulaeHelper.cs` during formula registration. Must be verified in FR-07 / SR-07.

---

## R6 — `Entity` struct requires `Unity.Entities.dll` reference even as a value

**Category:** Technical  
**Likelihood:** 🟡 Medium  
**Impact:** 🟡 Medium — Tests using `Entity.Null` or creating `new Entity { Index = N }` need `Unity.Entities.dll` accessible to the test project.

**Details:**  
`Entity` is a struct from `Unity.Entities`. Creating test instances (`Entity.Null`, `new Entity { Index = 42 }`) compiles fine against the metadata-only reference of `Unity.Entities.dll`, but the DLL must exist on the build machine (see R1).

The binding-seam tests for `WEBuildingFn`, `WEVehicleFn`, `WERouteFn` all use `Entity.Null` extensively.

**Mitigations:**
1. Same as R1 — game DLL reference strategy resolves this. `Unity.Entities.dll` from the game's managed folder is sufficient as metadata-only reference.
2. If CI cannot have the game installed: these tests are tagged `[Category("RequiresGameDLL")]` and skipped in CI.

**Unknown:** Whether `Entity` from the NuGet package `Unity.Entities` (com.unity.entities) could be used instead of the game's DLL. Unity's NuGet packages are not always 1:1 with game versions — version mismatch would cause runtime errors if the test somehow runs against the game.

---

## R7 — Binding seam `[TearDown]` contamination between parallel tests

**Category:** Test Design  
**Likelihood:** 🟢 Low  
**Impact:** 🟡 Medium — If the test runner runs tests in parallel (NUnit 3 supports parallel execution), binding seam fields are static and shared across threads, causing test interference.

**Details:**  
Binding seam fields are public static fields on production classes:
```csharp
public static Func<Entity, Entity> GetBuildingRoad_binding = ...
```
If two tests run in parallel and one overwrites the binding, the other may see an unexpected delegate.

**Mitigations:**
1. **Disable parallel execution** for all test assembles containing binding-seam tests. Add `[assembly: Parallelizable(ParallelScope.None)]` to `TestBase.cs` or the test assembly.
2. Alternative: use `[NonParallelizable]` on individual test fixtures that use binding seams.
3. Document the rule: test methods that modify static state must `NOT` use `[Parallelizable]`.

**Unknown:** None — this is a known NUnit pattern. The mitigation is straightforward.

---

## R8 — Formula evaluation uses Mono.Cecil / MonoMod for IL patching (runtime dependencies)

**Category:** Technical / Formulae Engine  
**Likelihood:** 🟡 Medium  
**Impact:** 🔴 High for formula registration tests — if `WEFormulaeHelper` uses MonoMod or Mono.Cecil IL rewriting, those assemblies must be present in the test runner process.

**Details:**  
The research identified that `WEFormulaeHelper` uses `System.Reflection.Emit` for delegate generation. However, cross-checking with `WENumberFormattingFn` comments suggests MonoMod may also be involved. If so, the test runner must have MonoMod loaded — which involves patching `System.Runtime.dll` in-process.

**Mitigations:**
1. During FR-07 / SR-07 development: carefully inspect `WEFormulaeHelper.cs` for MonoMod / Cecil imports. If present, assess whether they are needed at test time or only at game-boot time.
2. If MonoMod is needed: the test project must reference `MonoMod.RuntimeDetour` or equivalent. Pin the same version used in production.
3. If MonoMod patches cannot run outside the game: the formulae registration tests may be limited to testing the logic around `SetFormulae` (method discovery, metadata reading) without calling the Emit path.

**Unknown:** Exact dependencies of `WEFormulaeHelper.cs`. Must be audited at SR-06 / SR-07 time (Sprint 7).

---

## R9 — Missing game types cause cascading compile errors in test project

**Category:** Infrastructure  
**Likelihood:** 🟡 Medium  
**Impact:** 🟡 Medium — When `BelzontWE.csproj` is referenced project-to-project from `BelzontWE.Tests.csproj`, the test project tries to compile all of BelzontWE's source. If game type references fail, the test project will not compile even for pure-logic files.

**Details:**  
Project-to-project references compile the referenced project as part of the build. If `UnityEngine.dll` is not found and `BelzontWE` has compilation errors without it, `BelzontWE.Tests` fails too.

However, `BelzontWE.csproj` already compiles successfully against the game DLLs in its own build context. The question is whether the test project's game DLL path (TI-02) will resolve the same DLLs.

**Mitigations:**
1. Ensure test project's `GameDllRefs.targets` resolves the same DLL paths that `BelzontWE.csproj` uses.
2. Alternatively, reference `BelzontWE` as an **output assembly** reference (DLL, not project) — this avoids recompiling `BelzontWE` from source in the test build but loses rebuild integration.
3. Test option 1 first (project ref with shared DLL config). Fall back to option 2 if compile errors persist.

**Unknown:** Whether `BelzontWE.csproj` already has a correct DLL reference strategy that can be directly inherited by the test project. Audit in TI-01.

---

## U1 — Unknown: `WEFormulaeEvalCore.cs` actual formula parsing algorithm

**Category:** Unknown  
**Details:** The testability matrix estimates `WEFormulaeEvalCore.cs` at 60% coverage. The actual tokenizer and evaluator have not been deeply analyzed. SR-06 (sprint 7) requires reading the full file before designing the extraction. It's possible the parser is simpler than expected (pure string splitting) or more complex (stateful machine with backtracking).  
**Resolution path:** Read `WEFormulaeEvalCore.cs` line-by-line before starting SR-06.

---

## U2 — Unknown: whether `WEColorsFn.cs` has any pure logic

**Category:** Unknown  
**Details:** `WEColorsFn.cs` is 16 lines — likely just ECS lookups. BF-07 in the `builtin-fn` epic explicitly audits this. If it has no pure logic, it becomes an F-tier skip. If it has color math, it becomes a quick S-tier win.  
**Resolution path:** Read the file at BF-07 time.

---

## U3 — Unknown: behavior of `WEStringsBank` in a fresh test process (singleton state)

**Category:** Unknown  
**Details:** `WEStringsBank.Instance` is a lazy singleton. In a test process it initializes fresh on first access. But test isolation between test classes requires careful management — index 0 is always `""`, but if tests run in sequence and some tests add strings, the indices will grow. Tests must not depend on specific integer values being at specific positions unless they control the state.  
**Resolution path:** In PL-03, implement `[TearDown]` that resets the singleton, OR design tests to not depend on absolute index values (only on idempotency and round-trip correctness).

---

## U4 — Unknown: whether CI environment supports `.NET Framework 4.7.2`

**Category:** Infrastructure  
**Details:** The project targets `net472`. Common CI environments (GitHub Actions ubuntu-latest) do not ship the .NET Framework 4.7.2 SDK — they ship Mono or .NET 8. Running `dotnet test` against a `net472` target on Linux may require installing the `mono` runtime or targeting `netstandard2.0` for tests.  
**Resolution path:** In TI-07, test on a CI matrix with both Windows and ubuntu runners. If ubuntu fails, restrict CI to `windows-latest`.

---

## Summary Table

| Risk | Category | Likelihood | Impact | Sprint First Relevant |
|---|---|---|---|---|
| R1 — Game DLL reference | Infrastructure | 🟡 | 🔴 | Sprint 1 (TI-02) |
| R2 — Reflection.Emit on net472 | Technical | 🟡 | 🟡 | Sprint 7 (SR-06, FE-04) |
| R3 — TTF font licensing | Legal | 🟢 | 🟡 | Sprint 1 (TI-05) |
| R4 — NativeArray allocator blocked | Technical | 🔴 | 🟡 | Sprint 3 (FR-06) |
| R5 — WEModData.InstanceWE null | Technical | 🔴 | 🟡 | Sprint 5 (BF-05) |
| R6 — Entity struct needs game DLL | Technical | 🟡 | 🟡 | Sprint 5 (BF-01..04) |
| R7 — Parallel test contamination | Test Design | 🟢 | 🟡 | Sprint 5 (BF-02..04) |
| R8 — MonoMod in WEFormulaeHelper | Technical | 🟡 | 🔴 | Sprint 7 (SR-06, SR-07) |
| R9 — Cascading compile errors | Infrastructure | 🟡 | 🟡 | Sprint 1 (TI-01) |
| U1 — WEFormulaeEvalCore parser depth | Unknown | — | — | Sprint 7 |
| U2 — WEColorsFn pure logic exists? | Unknown | — | — | Sprint 6 (BF-07) |
| U3 — WEStringsBank singleton isolation | Unknown | — | — | Sprint 2 (PL-03) |
| U4 — net472 on Linux CI | Infrastructure | 🟡 | 🟡 | Sprint 1 (TI-07) |

**Highest priority to resolve first:** R1 (game DLL refs) and R9 (compile errors) — both in Sprint 1. Unblocking the project compile is prerequisite to everything else.
