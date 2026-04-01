**End time:** 2026-04-01 01:41 -0300
**Start time:** 2026-04-01 00:47 -0300
# [0013] BuiltinFn Attribute Registration Pattern

**Developed by:** Agent-Claude-Opus-4.6 <agent@example.com>
## Reference

Source: RefsLibrary/20260330_CodeStructureAnalysis/04_OverallModStructure/02_ImprovementOpportunities.md — Improvement 3

## User Story

> Acting as **a mod developer or template author reading the Write Everywhere source code**, I want **each built-in function class and method to carry a [WEBuiltinFunction] / [WEFormula] attribute**, so that I **the function contract is discoverable via IDE tooling and can be used to auto-generate documentation without reading all 14 files**.

---

## Background

Built-in functions (14 classes, ~50 methods in BuiltinFn/) are discovered at runtime via reflection. While this works correctly, it: Has no compile-time verification that a method matches the expected delegate signature; Provides no IDE navigation from formula string to function declaration; Creates a startup cost from assembly scanning.

Adding lightweight attributes provides compile-time contract documentation and enables tooling without changing the discovery mechanism.

---

## Definition of Ready (DoR)

- [ ] All 14 BuiltinFn/ classes are read
- [ ] The reflection-based discovery mechanism is located and its expected method signature/contract is understood
- [ ] Confirmed that adding attributes is purely additive — the existing reflection path continues to work unchanged

---

## Acceptance Criteria / Definition of Done (DoD)

- [x] A [WEBuiltinFunction(string category)] attribute is defined (in Utils/ or BuiltinFn/)
- [x] A [WEFormula(Type returnType)] attribute is defined for individual methods
- [x] All 14 built-in function classes have [WEBuiltinFunction] applied
- [x] All public formula-callable methods have [WEFormula] applied with the correct return type
- [x] The existing reflection-based registration path is unchanged (attributes are additive)
- [x] The RefsLibrary/20260330_CodeStructureAnalysis/02_Formulaes/02_BuiltinFunctionsReference.md document is updated to note that attributes now provide IDE-discoverable contracts
- [x] The mod compiles and loads without errors in CS2 v1.5.6

---

## Implementation Notes

1. Create minimal attribute definitions: [WEBuiltinFunction(string category)] and [WEFormula(Type returnType, string description)]
2. Apply [WEBuiltinFunction] to all 14 built-in function classes and [WEFormula] to all public methods
3. Alternative (lower effort): If attributes are deemed too ceremonious, a README.md in BuiltinFn/ documenting all function signatures is acceptable as a substitute

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Attribute assembly adds to mod binary size | Very low | Two tiny attribute classes add ~1 KB |
| Future developers apply attributes incorrectly | Low | Attribute names are self-documenting |

---

## Related Tasks

### Depends on



### Is dependent for


