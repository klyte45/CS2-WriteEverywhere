# Testing Action Plan — BelzontWE

> **Created:** 2026-04-01  
> **Based on:** `2026040101_testing-suite-research/` — Plan 1 (NUnit + NSubstitute), deep-dive class analysis, T3 mocking analysis

This folder consolidates all test research into a concrete, actionable implementation plan ready for sprint booking.

## File Map

| File | Description |
|---|---|
| [01_FileTestabilityMatrix.md](01_FileTestabilityMatrix.md) | Per-file tier rating (S→F), line count, method count, and coverage estimate |
| [02_SprintAttackPlan.md](02_SprintAttackPlan.md) | Sprint-by-sprint task breakdown with epic tags and sequencing rationale |
| [03_RisksAndUnknowns.md](03_RisksAndUnknowns.md) | Open risks, unknowns, and mitigation strategies |
| [epics/](epics/) | One file per epic: objectives, task drafts, acceptance criteria |

## Tier Reference (used across all files)

| Tier | Meaning |
|---|---|
| **S** | Fully testable without mocking |
| **A** | Fully testable with mocking in some/all methods |
| **B** | Partially testable now, but can be fully testable after some refactor |
| **C** | Partially testable now and can raise coverage after some refactor |
| **D** | Not testable, but fully testable after some refactor |
| **E** | Not testable, but can be partially tested after some refactor |
| **F** | Impossible to test |
| **/** | Not Applicable (no methods to test) |

## Quick Stats

- **Total source files analyzed:** 143
- **S/A-tier (testable now):** ~28 files
- **B/C-tier (testable with seams/refactor):** ~29 files
- **D/E-tier (requires refactor):** ~18 files
- **F-tier (impossible):** ~49 files
- **/-tier (marker types, no methods):** ~19 files
- **Estimated tests (T1+T2 full coverage):** 280–420
- **Total sprints needed:** 7

## Epics Summary

| Epic | ID | Focus | Tasks |
|---|---|---|---|
| Testing Infrastructure | `testing-infra` | Project setup, NuGet, CI | 8 |
| Pure Logic Tests | `pure-logic` | S-tier files, zero deps | 7 |
| Font Reader Tests | `font-reader` | stbtt port, pure-C# parsing | 8 |
| Component Data Tests | `component-data` | WETextData structs, clamp logic | 7 |
| IO/XML Tests | `io-xml` | XML round-trips, descriptors | 7 |
| BuiltinFn Tests | `builtin-fn` | Binding patterns, dict logic, formatting | 7 |
| Seam Refactoring | `seam-refactor` | Interface extraction to unlock B/C tiers | 7 |
| Formulae Engine Tests | `formulae-engine` | WEFormulaeHelper, tokenizer, eval core | 7 |
