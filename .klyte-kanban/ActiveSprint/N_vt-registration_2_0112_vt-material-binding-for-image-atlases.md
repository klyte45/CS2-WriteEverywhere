# [0112] vt-material-binding-for-image-atlases

**Developed by:** 

## User Story

> Acting as **the atlas system**, I want **to bind image atlas materials to VT instead of direct texture references**, so that I **materials sample from the VT streaming atlas, enabling on-demand tile loading and reducing constant VRAM pressure**.

---

## Background

Currently GenerateMaterial sets textures directly via Material.SetTexture(_BaseColorMap, ...). With VT, materials must instead: (1) call tss.BindMaterial(material, stackGlobalIndex, stackConfigIndex, paramBlock), (2) set VTTextureParamBlock as shader properties, (3) enable ENABLE_VT keyword, (4) set _UseStack0/_UseStack1 toggles. Font materials are excluded — they continue using direct texture binding. See 03_AtlasVTActionPlan.md R9 Path B step 4 and R10.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] VT-registered atlas materials use BindMaterial + ENABLE_VT instead of SetTexture
- [ ] VTTextureParamBlock transform/textureInfo are set as shader properties
- [ ] _UseStack0 and _UseStack1 are properly set
- [ ] Non-VT atlases and font atlases continue using direct texture binding unchanged
- [ ] Materials render correctly with VT binding (visual parity with direct binding)
- [ ] Test: generate material from VT atlas → verify ENABLE_VT keyword is set and fallback textures are not bound

---

## Implementation Notes

1. Modify WETextureAtlas.GenerateMaterial to check IsVTRegistered
2. If VT: tss.BindMaterial for each stack, set atlasParams0/1, enable ENABLE_VT keyword, set _UseStack0/_UseStack1 = 1.0
3. If not VT: keep existing Material.SetTexture path (backward compatible)
4. Update WERenderingHelper.GenerateBri to pass TextureStreamingSystem reference when atlas is VT-registered
5. Do NOT modify font material path (FontAtlas materials always use direct binding)
6. Materials must store a reference to their VT atlas for later unbinding on deregistration

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| VT-bound materials may render differently due to tile filtering or border artifacts | Medium | Compare screenshots of VT vs direct binding side by side; adjust tile padding if artifacts visible |
| BindMaterial may not work with WE's custom shader variants | Medium | Test with sg_defaultshader first; verify shader has VT sampling path compiled in |

---

## Related Tasks

### Depends on



### Is dependent for


