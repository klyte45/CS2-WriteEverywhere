**End time:** 2026-04-02 04:20 -0300
**Start time:** 2026-04-02 04:18 -0300
# [0033] Tests for Buf.cs (buffer cursor + big-endian reads, >=25 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests for every buffer navigation and parsing method in Buf**, so that I **the binary parsing layer that drives TTF/CFF decoding is regression-proof**.

---

## Background

[See epic: tst-font-reader](..\RefsLibrary\2026040123_testing-action-plan\epics\03_Epic_font-reader.md)

Task FR-02. Test every buffer navigation and parsing method in Buf (size/cursor, get8/peek8/seek/skip, big-endian get(1/2/4), buf_range, cff_int encoding).

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Font/FileReader/BufTests.cs exists with >=25 test methods
- [x] Tests cover: construction (size, cursor=0), get8 normal+past-end, peek8 without cursor advance, seek exact/negative/past-end clamping, skip, get(1/2/4) big-endian reads, buf_range valid+invalid
- [x] Tests cover cff_int for all op-code ranges (32-246, 247-250, 251-254, 255, 28, 29)

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


