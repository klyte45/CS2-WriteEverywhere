# BelzontWE.Tests

[![Tests](https://github.com/klyte45/CS2-WriteEverywhere/actions/workflows/tests.yml/badge.svg)](https://github.com/klyte45/CS2-WriteEverywhere/actions/workflows/tests.yml)

Unit and integration tests for the BelzontWE mod (Write Everywhere — Cities Skylines 2).

## Folder Structure

Mirrors the production source tree under `BelzontWE/`:

```
BelzontWE.Tests/
├── BuiltinFn/          → tests for BelzontWE/BuiltinFn/
├── Components/
│   └── WETextData/     → tests for BelzontWE/Components/WETextData*
├── Font/
│   └── FileReader/     → tests for BelzontWE/Font/ font-reading code
├── Fixtures/
│   └── Fonts/          → embedded TTF/OTF resources used by font tests
└── TestBase.cs         → shared NUnit base class (SetUp / TearDown)
```

## Naming Conventions

### Test files
`<TestedClass>Tests.cs` — one file per production class, placed in the folder that mirrors the production class location.

Examples:
- `BelzontWE/Font/WEFontFile.cs` → `BelzontWE.Tests/Font/FileReader/WEFontFileTests.cs`
- `BelzontWE/Components/WETextDataMaterial.cs` → `BelzontWE.Tests/Components/WETextData/WETextDataMaterialTests.cs`

### Test methods
`MethodName_Condition_ExpectedResult`

Examples:
- `Load_ValidTtfBytes_ReturnsFontWithGlyphs`
- `Serialize_NullMaterial_ThrowsArgumentNullException`
- `AlwaysPasses` (pipeline smoke tests — no condition/result suffix needed)

### Namespaces
Match the folder structure: `BelzontWE.Tests.<Subfolder>`.

## Guarded game-DLL tests
Tests that require Unity types should be wrapped in `#if GAME_DLLS_AVAILABLE / #endif`
so the CI pipeline (where `CSII_MANAGEDPATH` is not set) can still compile and run
the pure-.NET test suite.

> **CI note**: The `GAME_DLLS_AVAILABLE` constant is **not** defined in GitHub Actions
> because the runner has no game installation. The `_CIStubs/Managed/` folder is
> intentionally empty. Wrap any test that touches Unity or CS2 types in
> `#if GAME_DLLS_AVAILABLE` or tag it with `[Category("GameDlls")]` and filter it out
> via `--filter TestCategory!=GameDlls`. Pure .NET tests run unconditionally.
