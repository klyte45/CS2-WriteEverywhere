# [0012] Consolidate Value Wrapper Logic

**Developed by:** 

## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/04_OverallModStructure/02_ImprovementOpportunities.md — Improvement 2

## User Story

> Acting as **a mod developer maintaining or extending the Write Everywhere formula system**, I want **the shared compilation and evaluation logic from the five WETextDataValue* types extracted into a single utility**, so that I **any future change to the formula pipeline needs to be made in only one place**.

---

## Background

The five WETextDataValue* types (WETextDataValueFloat, WETextDataValueInt, WETextDataValueFloat3, WETextDataValueColor, WETextDataValueString) share nearly identical structure and logic: defaultValue and EffectiveValue fields; Formulae string index via WEStringsBank; loadingFnDone lazy flag; UpdateEffectiveValue() method that checks the flag, compiles if needed, and evaluates.

Each is implemented independently. A change to the compilation or caching path (e.g., adding health reporting from task 0009) must currently be replicated across all five types.

The proposed improvement extracts the shared pattern into a static utility class WEFormulaeEvalCore that all five types call, while keeping the concrete unmanaged struct types intact (required by ECS).

---

## Definition of Ready (DoR)

- [ ] All five WETextDataValue* types are read in full
- [ ] The exact code duplication between them is catalogued (line-by-line comparison)
- [ ] Confirmed that the shared logic (compile + evaluate pattern) does not differ in any meaningful way between types other than the generic type parameter T
- [ ] Confirmed that a static class with generic methods is compatible with the calling context

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] A new WEFormulaeEvalCore static class (in Utils/ or Components/WETextData/) is created with a generic TryEvaluate<T>() method encapsulating the compile-check + delegate-invoke + change-detection pattern
- [ ] All five WETextDataValue* types delegate their UpdateEffectiveValue() implementation to WEFormulaeEvalCore.TryEvaluate<T>()
- [ ] The five concrete struct types retain their type-specific fields (defaultValue, EffectiveValue) but no longer contain duplicated logic
- [ ] All existing formula evaluation behaviour is unchanged (same inputs → same outputs)
- [ ] If task 0009 (formula health reporting) is implemented before this task, the formulaeCompilationStatus field is part of the unified core, not duplicated five times
- [ ] The mod compiles and loads without errors in CS2 v1.5.6
- [ ] All formula types (float, int, float3, color, string) continue to evaluate correctly in-game

---

## Implementation Notes

1. Create WEFormulaeEvalCore static class with generic TryEvaluate<T>() method
2. Each WETextDataValue*'s UpdateEffectiveValue() becomes a one-liner delegating to WEFormulaeEvalCore.TryEvaluate()

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Generic method inference fails for value types in the calling context | Low | Explicit <T> type argument can be specified at call site |
| Subtle per-type differences in evaluation logic were missed | Medium | Thorough line-by-line comparison in DoR step; validate all five types after refactor |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0009]
