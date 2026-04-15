**Start time:** 2026-04-15 06:25 -0300
# [0114] vt-registration-lifecycle-in-library

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **the atlas library system**, I want **to orchestrate the full VT registration lifecycle (register on load, deregister on dispose/reload) for all atlas types**, so that I **local, mod, and city atlases all benefit from VT streaming automatically without per-caller logic**.

---

## Background

WEAtlasesLibrary manages three atlas dictionaries (LocalAtlases, ModAtlases, CityAtlases). Each atlas must be VT-registered after creation/loading and deregistered before disposal. The registration must happen after FromCacheFile or after Apply+WriteBC7Cache+Reload. The lifecycle hooks are: RegisterLocalAtlas, EnqueueModAtlasLoader, OnStartRunning (city atlases), ClearAtlasDict, UnregisterModAtlas, and Dispose. The TextureStreamingSystem reference is already stored in m_textureStreamingSystem.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] All atlas types (local, mod, city) are VT-registered after loading
- [ ] All atlas types are VT-deregistered before disposal
- [ ] Atlas reload path: deregister old → dispose → load new → register new
- [ ] No orphaned VT registrations after full atlas clear
- [ ] OnDestroy properly cleans up all VT registrations
- [ ] Test: full lifecycle — load local atlases → reload → verify no VT leaks
- [ ] Test: mod atlas register/unregister cycle

---

## Implementation Notes

1. After WETextureAtlas.FromCacheFile or after atlas reload, call atlas.RegisterToVT(m_textureStreamingSystem)
2. Before atlas.Dispose(), call atlas.DeregisterFromVT(m_textureStreamingSystem) if IsVTRegistered
3. In LoadImagesFromLocalFoldersCoroutine: deregister before disposing stale atlases, register after loading new ones
4. In EnqueueModAtlasLoader: deregister old atlas before replacing with new one
5. In ClearAtlasDict: deregister all atlases before clearing
6. In OnDestroy: deregister all atlases
7. City atlases: register in OnStartRunning after Init, deregister on RemoveFromCity
8. Pass TextureStreamingSystem to all lifecycle methods or access via Instance

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


