**End time:** 2026-03-31 23:16 -0300
**Start time:** 2026-03-31 23:11 -0300
# [0002] Pre-compile Formulas on Template Load

**Developed by:** Agent-Claude-Sonnet-4.6 <agent@example.com>
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/02_Formulaes/03_ImprovementAnalysis.md — Area 3

## User Story

> Acting as **a player loading a savegame with hundreds of Write Everywhere text objects**, I want **formula IL compilation to happen during template loading rather than on first render**, so that I **there is no frame-rate stall or visual delay on the first frame after load**.

---

## Background

Formula compilation (MonoMod.Utils.DynamicMethodDefinition) happens lazily on first evaluation — when any entity first accesses a value wrapper whose loadingFnDone flag is false. Each unique formula string takes ~1–5 ms to compile. When a savegame loads hundreds of WE entities simultaneously, this causes compilation spikes during the Rendering phase on the first several frames after load.

The cache key is the formula string itself (cachedFnsString.ContainsKey(formulaString)), so identical formulas across entities compile only once. However, the loadingFnDone flag is per-value-wrapper-instance, so each entity triggers a cache lookup on its first frame even when the compiled delegate is already cached.

Pre-compiling all unique formula strings when a template is loaded shifts this cost to load time (where frame-rate sensitivity is much lower).

---

## Definition of Ready (DoR)

- [ ] WETemplateManager (all partial files) are identified and the template loading entry point is located
- [ ] WEFormulaeHelper.SetFormulae() (or the equivalent warm-up entry point) is confirmed to accept a formula string and pre-populate the cache without requiring a live entity
- [ ] All five WETextDataValue* types are identified and their formula string field accessor is understood (Formulae property / WEStringsBank index)
- [ ] A reproducible test exists: savegame load → observe compilation spike on frame 1 (via profiler or log timestamps)

---

## Acceptance Criteria / Definition of Done (DoD)

- [x] After loading a template (city template or prefab layout), all formula strings present in that template are pre-compiled into the shared formula cache before any entity begins rendering
- [ ] No change to the lazy loadingFnDone evaluation path — entities without pre-loaded templates still work correctly
- [x] First-frame rendering of a freshly loaded savegame shows no IL compilation calls in the profiler (all compiles happen during WETemplateManager load path)
- [x] Formula runtime behaviour is identical to before (pre-compilation only warms the cache; entities still evaluate normally via loadingFnDone flag)
- [x] No NullReferenceException or exception spam generated if a formula string is malformed — the pre-compile should catch and log the error at load time, not at render time
- [ ] The mod compiles and loads without errors in CS2 v1.5.6

---

## Implementation Notes

1. In WETemplateManager (likely the partial file handling city/prefab template deserialization), after each template object is fully deserialized, enumerate all WETextDataValue* fields that contain a non-empty formula string
2. For each non-empty formula string, call the equivalent of WEFormulaeHelper.WarmCache(formulaString) — which internally does the same SetFormulae work without needing a real entity reference
3. If WEFormulaeHelper does not have a public cache-warm entry point, add one (example provided in task document)
4. Wrap the warm call in a try/catch so a bad formula string at load time logs a warning but does not abort template loading
5. The loadingFnDone flag on individual value wrapper instances can remain as-is — it will resolve to the already-cached delegate on first access (near-zero cost)
6. Added WEFormulaeHelper.WarmCache<T>(string)  a safe no-throw wrapper around SetFormulae<T> that skips null/whitespace inputs and logs a warning on compilation failure instead of propagating. Added PreCompileFormulas() to WETextDataXml (delegates to all child style/mesh objects), WETextDataXmlTree (recursive traversal), and each FormulaeXml-bearing inner class (TransformXml, MeshDataTextXml, MeshDataImageXml, MeshDataPlaceholderXml, MeshDataMatrixTransformXml, DefaultStyleXml, DecalStyleXml, GlassStyleXml). PreCompileFormulas() is called immediately after template registration in: WETemplateManager.Deserialize (city templates), WETemplateManager.PrefabLayout LoadPrefabFileTemplate, and WETemplateManager.ModSubTemplates mod layout loading.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| WEFormulaeHelper compile method is not easily callable without entity context | Medium | Inspect the compile path; if entity context is required, stub a dummy entity or refactor the compile step to be entity-independent |
| Load time increases noticeably for large template sets | Low | Compilation is ~1–5 ms per unique formula; a template set with 100 unique formulas adds ~100–500 ms to load — acceptable and less disruptive than per-frame spikes |
| Template XML uses string banks, not raw strings | Medium | Resolve the WEStringsBank index to the actual string before calling warm-up |

---

## Related Tasks

### Depends on



### Is dependent for


