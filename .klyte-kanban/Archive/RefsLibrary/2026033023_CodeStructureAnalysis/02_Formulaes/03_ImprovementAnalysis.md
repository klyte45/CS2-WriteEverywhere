# Formulae System: Improvement Analysis

> **Purpose**: Evaluates the current formulae system for real improvement opportunities in performance, maintainability, and extensibility.

## Current System Strengths

Before analyzing improvements, it's important to acknowledge what works well:

1. **IL compilation with caching** — Formulas compile once and execute as native delegates. No interpretation overhead per frame.
2. **Staggered update scheduling** — `nextUpdateFrame = base + interval + (index % interval)` prevents CPU spikes.
3. **String deduplication** — `WEStringsBank` avoids duplicate string allocations across entities sharing the same formula.
4. **Variable caching** — `WEVarsCacheBank` deduplicates identical variable dictionaries.
5. **LOD-based evaluation cutoff** — Distant entities stop evaluating formulas entirely.
6. **Enableable dirty flag** — `WETextDataDirtyFormulae` uses ECS enableable component pattern, avoiding structural changes for dirty marking.

## Area 1: Burst Incompatibility (Fundamental Limitation)

### Problem
The formulae system generates IL code via `MonoMod.Utils.DynamicMethodDefinition`. The generated delegates call `EntityManager.GetComponentData<T>()` and navigate managed objects via reflection-resolved member access. This is fundamentally incompatible with Burst compilation because:
- Generated methods contain managed references
- `EntityManager` access from jobs requires `[NativeDisableUnsafePtrRestriction]` workarounds
- Dictionary lookup is managed code

### Assessment
**No viable improvement path exists for Burst-compiling the formula evaluation itself.** The flexibility of arbitrary component navigation and method invocation inherently requires managed code. This is a correct architectural trade-off: the alternative would be a domain-specific bytecode interpreter running in Burst, which would be far more complex and may not be faster than the current IL-compiled approach for the small batch sizes involved.

### Verdict: No change recommended
The current approach is optimal given the constraints. The IL compilation already produces near-native-speed delegates.

## Area 2: Per-Entity Evaluation Cost

### Problem
Each entity with formulas evaluates up to **19 fields** per update cycle. Each field call goes through:
1. Cache lookup (dictionary access)
2. Delegate invocation
3. `EntityManager.GetComponentData` (one or more per segment)
4. Member navigation chain
5. Type conversion
6. Change detection

For 1000 visible entities at update interval 2, this means ~9500 field evaluations per frame.

### Assessment
The per-field cost is dominated by `EntityManager.GetComponentData<T>()` calls, which involve:
- Component type index lookup
- Archetype chunk navigation
- Memory copy of component data

When a formula navigates 3 segments, it makes 3 separate `GetComponentData` calls. There's no batching or prefetching.

### Potential Improvement: Component Pre-fetch Cache
For entities where the same component type appears in multiple formula fields (e.g., `Transform` used in position, rotation, and scale formulas), the component data could be fetched once and reused across fields within the same entity's update.

**However**: This would require changing the `FormulaeFn<T>` delegate signature to accept a pre-fetched component cache, which would invalidate all compiled formulas and add complexity to the IL generation.

### Verdict: Not recommended
The `EntityManager.GetComponentData` calls are already well-optimized by Unity's ECS. The overhead of a caching layer would likely exceed the savings for typical formula complexity (1-2 segments most of the time).

## Area 3: Compilation Cost and Timing

### Problem
Formula compilation happens lazily on first evaluation via `SetFormulae()`. If many new entities appear simultaneously (e.g., loading a savegame with hundreds of WE text objects), compilation spikes can occur because:
- `DynamicMethodDefinition` creation is ~1-5ms per formula
- Compilation happens on the main thread during the job's `Execute()`
- Many entities may share the same formula but each triggers `!loadingFnDone` check

### Assessment
The cache check (`cachedFnsString.ContainsKey(formulaString)`) prevents recompilation of identical formulas. But the `loadingFnDone` flag is per-value-wrapper-instance, so even cached formulas require a cache lookup on first access per entity.

### Potential Improvement: Pre-compilation During Template Loading
When a template is loaded (via `WETemplateManager` or `WEPrefabLayoutSystem`), all formula strings in the template are known. These could be pre-compiled before any entity uses them:

```
Template Load → Extract all formula strings → Compile batch → Cache warm
```

This would shift compilation cost from rendering time to loading time, where frame-rate sensitivity is lower.

### Verdict: ⚠️ Minor improvement possible
Pre-compilation during template load is a clean optimization that doesn't change the runtime path. The implementation would require `WETemplateManager` to call `WEFormulaeHelper.SetFormulae()` for each formula string in a template when loading it. This is low-risk and eliminates first-frame compilation stalls.

## Area 4: Update Frequency Granularity

### Problem
The dirty flag mechanism uses a single `nextUpdateFrame` counter per entity. All 19 formula fields for an entity update together regardless of whether they actually change.

### Assessment
Some formula fields change rarely (e.g., building address, route name) while others change frequently (e.g., time display, vehicle destination). Currently, all fields update at the same interval.

### Potential Improvement: Per-Field Update Intervals
Fields could track their own change frequency and self-tune their update interval:
- If a field's value hasn't changed in 10 evaluations, double its interval
- If a field changes, reset to base interval

**However**: This would require significant changes:
- Per-field frame counters (19 additional ints per entity)
- Modified dirty-marking logic in `WEPreCullingSystem`
- Complex scheduling coordination

### Verdict: Not recommended
The additional per-entity memory (19 × 4 = 76 bytes) and scheduling complexity outweigh the benefit. The existing staggered scheduling with `FramesCheckUpdate` (user-configurable, 0-7) provides sufficient control. Users experiencing CPU issues can increase the interval.

## Area 5: Error Handling in Compiled Formulas

### Problem
When a formula encounters an error at runtime (component missing, null reference, divide by zero), it returns a sentinel value (NaN, MinValue, magenta, etc.). The error is swallowed silently, and the formula continues to be evaluated every cycle.

### Assessment
This is correct for production use — errors should not crash the mod. However, for development/debugging:
- No log output on formula errors (to avoid spam)
- No way to see which formulas are failing
- No way to see error reason

### Potential Improvement: Formula Health Reporting
Add a debug mode that tracks error counts per formula and exposes them via UI or log:
- `WETextDataValueColor.formulaeCompilationStatus` already exists — extend this pattern to all value types
- In debug mode, log first occurrence of each error type per formula
- Expose formula health via `WEFormulaeController` UI binding

### Verdict: ⚠️ Quality-of-life improvement possible
This doesn't improve performance but significantly improves the development experience. The `formulaeCompilationStatus` field already exists on `WETextDataValueColor` — unifying this across all value types would be a clean improvement.

## Area 6: Dictionary Allocations in Variable Resolution

### Problem
The `WEVarsCacheBank` resolves variables from `FixedString512Bytes` → `int` → `Dictionary<string, string>`. The dictionaries are managed objects on the heap, and each unique variable set creates a new dictionary.

### Assessment
The caching is effective — entities with identical variables share the same dictionary instance. But:
- Dictionary allocation is GC-pressure (managed heap)
- Dictionary lookup involves hash computation and potential collisions
- For formulas that don't use variables (many don't), the dictionary is passed but ignored

### Potential Improvement: NativeHashMap-based Variables
Replace `Dictionary<string, string>` with `NativeHashMap<FixedString32Bytes, FixedString32Bytes>`:
- Zero GC pressure
- Burst-compatible (even though formulas aren't Burst-compiled, it eliminates one source of GC)

**However**: This would require changing the `FormulaeFn<T>` delegate signature, invalidating all compiled formulas and all built-in functions. The cost of changing 50+ function signatures far outweighs the GC benefit.

### Alternative: Pass Null for Variable-Free Formulas
Detect at compilation time whether a formula references variables. If not, pass `null` instead of the full dictionary lookup chain.

### Verdict: Not recommended (full change), ⚠️ minor optimization for null-pass
The variable-free formula optimization is trivially implementable: if the formula string contains no `&` prefix segments that take a `vars` parameter, skip the variable resolution. But the actual savings are minimal since `WEVarsCacheBank` lookup is O(1) cached.

## Summary

|  ID | Area | Opportunity | Recommendation | Impact |
|-----|------|------------|----------------|--------|
| 1 | Burst compilation | None possible | No change | — |
| 2 | Per-entity evaluation cost | Component pre-fetch | Not recommended | Marginal |
| 3 | Compilation timing | Pre-compile on template load | **Recommended** | Medium |
| 4 | Update frequency granularity | Per-field intervals | Not recommended | Complexity > benefit |
| 5 | Error handling | Unified health reporting | **Recommended** (QoL) | Dev experience |
| 6 | Variable allocations | Null-pass for variable-free | Optional micro-optimization | Marginal |

## Conclusion

The formulae system is well-architected for its purpose. The IL compilation approach is the right choice given the constraint that formulas need to access arbitrary ECS components and call arbitrary static methods. The main realistically beneficial improvements are:

1. **Pre-compile formulas during template loading** — eliminates first-frame stalls at negligible development cost
2. **Unified formula health reporting** — improves development experience when debugging formula errors

The system's performance is inherently bounded by `EntityManager.GetComponentData` access patterns, which are already well-optimized by Unity's ECS runtime. Further optimization attempts would add complexity without meaningful gains for typical use cases (hundreds to low thousands of visible text entities).
