**End time:** 2026-03-31 23:55 -0300
**Start time:** 2026-03-31 23:47 -0300
# [0009] Unified Formula Health Reporting

**Developed by:** Agent-Claude-Opus-4.6 <agent@example.com>
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/02_Formulaes/03_ImprovementAnalysis.md — Area 5

## User Story

> Acting as **a Write Everywhere template author or power user**, I want **to see which formulas are failing and why (via a UI panel or log)**, so that I **I can diagnose broken formula strings without needing to read source code or attach a debugger**.

---

## Background

When a formula encounters a runtime error (missing component, null reference, divide by zero), the system silently returns a sentinel value (NaN, MinValue, magenta color, etc.). No log entry is written (by design, to avoid spam). There is no way for the user to know a formula is silently broken.

WETextDataValueColor already has a formulaeCompilationStatus field that tracks compilation success/failure for color values. This pattern is not consistently applied to the other four value wrapper types (Float, Int, Float3, String).

The improvement has two parts: 1) Consistency: Apply the formulaeCompilationStatus field (or equivalent) to all five value wrapper types. 2) Visibility: Expose formula health (per entity, per field) through WEFormulaeController's UI binding so template authors can see errors in the editor panel.

---

## Definition of Ready (DoR)

- [ ] All five WETextDataValue* types are located and their structure is read
- [ ] WETextDataValueColor.formulaeCompilationStatus is read and the population/reporting pattern is understood
- [ ] WEFormulaeController.cs UI binding methods are read and the pattern for adding new bound data is understood
- [ ] The UI component that displays formula status in the frontend is identified (or new UI work is scoped and explicitly out-of-scope for this task)

---

## Acceptance Criteria / Definition of Done (DoD)

- [x] All five WETextDataValue* types (Float, Int, Float3, Color, String) have a formulaeCompilationStatus-equivalent field that tracks: OK, COMPILE_ERROR, RUNTIME_ERROR, NO_FORMULA
- [x] On first occurrence of a runtime error for a given entity+field, the error type and message are logged once at Debug level (no repeated spam)
- [x] WEFormulaeController exposes an aggregated formula health status for the currently selected WE entity via its UI binding
- [ ] The frontend UI (or at minimum the data binding) surfaces at least a per-field status indicator; full UI styling is acceptable as a follow-up
- [x] No performance regression in steady state (healthy formulas): the status field is updated only on status change, not every frame
- [x] The mod compiles and loads without errors in CS2 v1.5.6

---

## Implementation Notes

1. Define a shared enum or constants for formula status values (alternatively, reuse the pattern from WETextDataValueColor)
2. In each WETextDataValue*, wrap the UpdateEffectiveValue() evaluation call in a try/catch; on catch, set formulaeCompilationStatus = RUNTIME_ERROR and log once
3. In WEFormulaeController, add a UI-bindable query method that returns the health snapshot for the selected entity's WE component
4. Avoid storing the full exception message in an unmanaged struct — use a string bank index or a separate managed dictionary keyed by entity+field to store error messages
5. Added formulaeCompilationStatus byte field + runtimeErrorLogged flag to Float, Int, Float3, String types. Color already had the field but now also tracks runtime errors (status=255). Added CollectFormulaHealth() to WETextDataMaterial (14 fields), WETextDataTransform (2 fields), WETextDataMesh (6 fields). Added formulae.getFormulaHealth call binding in WEFormulaeController that returns per-field status dict for current sub-entity. DoD item 4 (frontend indicator) deferred as follow-up per risk assessment.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Adding try/catch inside hot path adds overhead | Low | Exceptions are rare; try/catch with no exception has near-zero cost in .NET |
| UI binding changes require frontend TypeScript changes | Medium | Scope the backend binding as the primary deliverable; frontend display can be a follow-up |

---

## Related Tasks

### Depends on



### Is dependent for



### Is related to

- [0012]
