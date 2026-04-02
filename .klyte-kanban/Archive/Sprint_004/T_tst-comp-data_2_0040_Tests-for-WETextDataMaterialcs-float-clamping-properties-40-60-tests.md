**End time:** 2026-04-02 05:07 -0300
**Start time:** 2026-04-02 05:03 -0300
# [0040] Tests for WETextDataMaterial.cs - float clamping properties (40-60 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **parameterized tests for every clamped float property in WETextDataMaterial**, so that I **any change to a clamping range is immediately detected**.

---

## Background

[See epic: tst-comp-data](..\RefsLibrary\2026040123_testing-action-plan\epics\04_Epic_component-data.md)

Task CD-01. Parameterized tests for every clamped float property in WETextDataMaterial (NormalStrength, GlassRefraction, Metallic, Smoothness, EmissiveIntensity, EmissiveExposureWeight, CoatStrength, GlassThickness). Below-min->min, above-max->max, mid-value unchanged.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] Components/WETextData/WETextDataMaterialTests.cs exists
- [x] Each clamped property has a [TestCase] table: below-min->min, above-max->max, mid-value unchanged
- [x] Properties tested: NormalStrength [0,1], GlassRefraction [1,1000], Metallic [0,1], Smoothness [0,1], EmissiveIntensity [0,1000], EmissiveExposureWeight [0,1], CoatStrength [0,1], GlassThickness [0,10]
- [x] Tests for: Shader setter stores value, RenderBackface setter sets dirty=true, DEFAULT_DECAL_FLAGS constant value

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


