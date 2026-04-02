**End time:** 2026-04-02 01:20 -0300
**Start time:** 2026-04-02 01:18 -0300
# [0021] Add TTF fixture file as embedded resource (TestFontFixture helper)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a font test author**, I want **a small, free-license TTF file embedded in the test assembly**, so that I **FontInfo and Font tests can use real glyph data without depending on the game's font files**.

---

## Background

[See epic: tst-test-infra](..\RefsLibrary\2026040123_testing-action-plan\epics\01_Epic_testing-infra.md)

Task TI-05. Embed a small free-license TTF file in the test assembly. Provide TestFontFixture helper class with GetTestFontBytes() via Assembly.GetManifestResourceStream.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] A small (~50KB) free-license TTF placed under BelzontWE.Tests/Fixtures/Fonts/
- [x] The font file is set as EmbeddedResource in the .csproj
- [x] TestFontFixture helper class provides byte[] GetTestFontBytes() using Assembly.GetManifestResourceStream
- [x] Verified: Font.FromMemory(TestFontFixture.GetTestFontBytes()) does not throw

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


