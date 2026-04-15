**End time:** 2026-04-15 09:22 -0300
**Start time:** 2026-04-15 06:33 -0300
# [0115] vt-integration-testing-and-validation

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **comprehensive integration tests for the VT registration system**, so that I **VT registration works reliably across all atlas types and lifecycle scenarios**.

---

## Background

VT registration is high-complexity and touches multiple systems. Integration tests must verify: registration succeeds, materials render correctly, deregistration cleans up properly, reload cycles work, and fallback to non-VT works if registration fails. Visual parity between VT and direct-texture rendering must be validated. Memory measurements should confirm VRAM reduction.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] All lifecycle scenarios tested: register, deregister, re-register, dispose
- [x] Visual parity confirmed between VT and direct texture rendering
- [x] No VRAM/VT atlas slot leaks across 10+ reload cycles
- [x] Graceful fallback when VT registration fails
- [x] Font materials confirmed unaffected
- [x] Memory reduction measured and documented
- [x] No existing test regressions

---

## Implementation Notes

1. Test: build atlas → register to VT → verify ENABLE_VT keyword on material → render → compare with direct texture rendering
2. Test: register → deregister → re-register cycle (10x) → no errors or leaks
3. Test: atlas reload during runtime → old VT deregistered, new registered
4. Test: VT registration failure (simulated full atlas) → graceful fallback to direct texture
5. Test: memory measurement — compare VRAM with VT vs without VT for typical scenario
6. Test: font materials are never VT-registered (verify no ENABLE_VT on font materials)
7. Test: city atlas save → load → VT registered → modify → re-register
8. Test: ExtendedPVTStack layer count handling — verify fallback for control/emissive if insufficient layers

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


