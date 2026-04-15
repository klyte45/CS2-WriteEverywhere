**End time:** 2026-04-15 02:24 -0300
**Start time:** 2026-04-15 02:21 -0300
# [0102] savegame-bc7-serialization

**Developed by:** GitHub Copilot <claude-sonnet-4-5@kwytco.com.br>
## User Story

> Acting as **the atlas system**, I want **city atlas data in savegames use BC7 instead of PNG**, so that I **smaller save files (60-75% reduction)**.

---

## Background



---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] New save uses version 1 with BC7
- [x] Load version 0 decodes PNG
- [x] Load version 1 loads BC7
- [x] Save file reduced 60-75%
- [x] Test: v0 to v1 migration
- [x] Test: v1 rendering matches v0

---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on

- [0097]
### Is dependent for

---
