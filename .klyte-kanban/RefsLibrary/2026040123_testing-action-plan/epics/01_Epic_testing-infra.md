# Epic: `testing-infra` — Testing Infrastructure Setup

## Objective

Establish the complete foundation that makes all subsequent test epics possible. This epic produces a working `BelzontWE.Tests` project, integrated into the MSBuild solution, with all NuGet dependencies resolved, game DLL reference strategy configured, and CI built in. No test cases are written here — but after this epic, writing any test in a later epic must be a matter of "create file, write test, run `dotnet test`" with zero further setup friction.

## Why This Epic Comes First

All other epics are blocked until:
1. The test project compiles.
2. `dotnet test` can discover and run tests.
3. Game DLL and Unity NuGet references are accessible.
4. The team has a convention to follow (naming, fixture base classes, folder structure).

Skipping this or doing it "inline" with the first test epic will generate rework.

---

## Task Drafts (8 tasks)

### TI-01 — Create `BelzontWE.Tests` project file
**Story:** As a developer, I want a `.csproj` that targets `net472` (matching the game runtime), references `BelzontWE.csproj`, and declares NUnit + NSubstitute NuGet dependencies so that the build system can compile the test assembly.

**DoD checklist:**
- [ ] `BelzontWE.Tests/BelzontWE.Tests.csproj` exists with `net472` target
- [ ] References `BelzontWE.csproj` project-to-project
- [ ] `NUnit`, `NUnit3TestAdapter`, `NSubstitute`, `NSubstitute.Analyzers.CSharp` declared as PackageReference
- [ ] Added to `BelzontWE.sln` under a `Tests` solution folder
- [ ] `dotnet build BelzontWE.Tests.csproj` succeeds (even with zero test files)

---

### TI-02 — Configure game DLL metadata references
**Story:** As a developer, I want the test project to reference critical game assemblies (UnityEngine.CoreModule, Unity.Entities, Unity.Collections, Unity.Mathematics) as metadata-only references so that files with Unity types compile in the test assembly.

**DoD checklist:**
- [ ] A `GameDllRefs.targets` MSBuild props file under `_Build/` builds the reference list from a configurable game install path
- [ ] The game install path is set via an environment variable or `Directory.Build.props` local override (not committed to git)
- [ ] `BelzontWE.Tests.csproj` imports `GameDllRefs.targets`
- [ ] The following DLLs resolve: `UnityEngine.CoreModule.dll`, `Unity.Entities.dll`, `Unity.Collections.dll`, `Unity.Mathematics.dll`
- [ ] If game path not found, build emits a warning but does not fail (fallback: exclude Unity-dependent test files via `#if GAME_DLLS_AVAILABLE`)

---

### TI-03 — Add `InternalsVisibleTo` to `BelzontWE`
**Story:** As a test author, I want to access `internal` members of `BelzontWE` from the test project so that internal helpers and state can be observed in tests without forcing them to be `public`.

**DoD checklist:**
- [ ] `[assembly: InternalsVisibleTo("BelzontWE.Tests")]` added to `BelzontWE/Properties/AssemblyInfo.cs` (or equivalent)
- [ ] No existing public API changes
- [ ] Verified: at least one `internal` member (e.g., `WEStringsBank._instance`) is accessible in a test file

---

### TI-04 — Create test folder structure and naming conventions
**Story:** As the team, we want a documented, consistent folder structure for test files so that contributors know exactly where to place new tests.

**DoD checklist:**
- [ ] `BelzontWE.Tests/` mirrors `BelzontWE/` folder structure (e.g., `BuiltinFn/`, `Font/FileReader/`, `Components/WETextData/`)
- [ ] `README.md` inside `BelzontWE.Tests/` explains: test file naming (`<TestedClass>Tests.cs`), test method naming (`MethodName_Condition_ExpectedResult`), and fixture class pattern
- [ ] `TestBase.cs` created with `[SetUp]`/`[TearDown]` hooks for shared binding restoration (for BuiltinFn binding seam resets)

---

### TI-05 — Add TTF fixture file to test assembly
**Story:** As a font test author, I want a small, free-license TTF file embedded in the test assembly so that `FontInfo` and `Font` tests can use real glyph data without depending on the game's font files.

**DoD checklist:**
- [ ] A small (~50KB) free-license TTF (e.g., derived from open-source font) placed under `BelzontWE.Tests/Fixtures/Fonts/`
- [ ] The font file is set as `EmbeddedResource` in the `.csproj`
- [ ] A `TestFontFixture` helper class provides `byte[] GetTestFontBytes()` using `Assembly.GetManifestResourceStream`
- [ ] Verified: `Font.FromMemory(TestFontFixture.GetTestFontBytes())` does not throw

---

### TI-06 — Integrate `dotnet test` into MSBuild workflow
**Story:** As a developer, I want to be able to run all tests from the same MSBuild command used for the mod build so that the test suite runs automatically during local development builds.

**DoD checklist:**
- [ ] `Frontend.targets` or a new `Tests.targets` adds an `AfterBuild` target that runs `dotnet test BelzontWE.Tests.csproj --no-build`
- [ ] Tests only run when `Configuration=Debug` (not Release, to keep release builds fast)
- [ ] Build does not fail if test runner is not installed — it emits a warning instead
- [ ] Running `MSBuild.exe BelzontWE.sln /p:Configuration=Debug` reports test pass/fail to the build log

---

### TI-07 — Create GitHub Actions / CI pipeline (optional but documented)
**Story:** As the project maintainer, I want CI to run tests on every push so that regressions are caught before manual testing.

**DoD checklist:**
- [ ] `.github/workflows/tests.yml` added (or Codeberg equivalent pipeline)
- [ ] CI restores NuGet packages and builds the test project
- [ ] CI runs `dotnet test` and reports exit code
- [ ] README.md in `BelzontWE.Tests/` updated to describe CI status badge (even if offline initially)
- [ ] Build matrix note: game DLLs will NOT be available in CI — Unity-dep tests are skipped via `#if` or `[Ignore]`

---

### TI-08 — Smoke test: validate the full test pipeline end-to-end
**Story:** As a QA baseline, I want a trivial test that always passes and a trivial test that I can toggle to always fail so that I can verify the test runner is wired correctly before writing real tests.

**DoD checklist:**
- [ ] `PipelineSmokTests.cs` has `AlwaysPasses()` returning `Assert.Pass()`
- [ ] Running `dotnet test` shows `1 passed` (or more if previous TI tests added assertions)
- [ ] The smoke test file documents what NuGet packages and assemblies were successfully resolved

---

## Epic Acceptance Criteria

- [ ] `dotnet test BelzontWE.Tests.csproj` runs without configuration errors
- [ ] At least 1 test discovered and passes in CI
- [ ] Game DLL config is documented and team can follow it without help
- [ ] No existing BelzontWE production code changed (except `InternalsVisibleTo`)
