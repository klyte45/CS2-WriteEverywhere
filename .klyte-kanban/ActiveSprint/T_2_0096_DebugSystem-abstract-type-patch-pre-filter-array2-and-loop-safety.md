**End time:** 2026-04-07 19:15 -0300
**Start time:** 2026-04-07 18:50 -0300
# [0096] DebugSystem abstract-type patch — pre-filter + array2 sizing + loop-body safety check

**Developed by:** Claude-Sonnet-4.6 (claude-sonnet-4.6@kwyt.com.br)

## User Story

> Acting as **a modder running WE alongside other mods**, I want **the DebugSystem abstract-class-instantiation patch to correctly size both the ToolBaseSystem array and the GUIContent array, and to skip patching when another mod has already modified the loop**, so that I **don't get null-reference crashes from mismatched array lengths, and double-patching conflicts are avoided**.

---

## Background

`DebugSystemOverrides` in `BelzontCommons` previously used an in-loop transpiler injection that:
- shifted `allTypesDerivedFromAsArray` (V_1) in place to remove abstract types
- decremented `i` so the next iteration lands on the moved-up element
- used a null-sentinel trick to exit the loop early

Two problems were identified:

1. **`GUIContent[] array2` (V_3) was never resized.** Since `array` (V_2) and `array2` (V_3) are both pre-allocated to `allTypesDerivedFromAsArray.Length`, removing entries from the V_1 array but not from V_2/V_3 left trailing null entries that were passed to `RadioSelection`, causing a null-ref crash.

2. **No conflict detection for other mods.** If another mod already patched the same loop body, the old approach would double-inject, corrupting the IL.

---

## Implementation

Replaced the in-loop injection with a **pre-filter approach**:

- **`FilterAbstractTypes(Type[] types)`**: called immediately after `GetAllTypesDerivedFromAsArray` stores its result in V_1 (`stloc.1`). It replaces V_1 with `Array.FindAll(types, t => t != null && !t.IsAbstract)`.
- Because V_1 is filtered *before* `newarr ToolBaseSystem` and `newarr GUIContent`, both arrays are sized from the already-filtered length — no trailing nulls, no null-sentinel loop logic needed.
- **Loop-body opcode signature check**: before injecting, the transpiler matches all 24 expected opcodes of the unpatched for-loop body (instructions 40–63 in IL). If the sequence differs, the injection is skipped with a `DoWarnLog` — another mod is assumed to have already applied a compatible fix.

### Files changed
- `BelzontWE.Commons/BelzontCommons/DebugSystemOverrides.cs` — full rewrite of transpiler logic

---

## Definition of Done

- [x] `array2` (GUIContent[]) is always sized to the filtered type count
- [x] `array` (ToolBaseSystem[]) is always sized to the filtered type count
- [x] If another mod already modified the loop body, the injection is skipped without error
- [x] Project compiles without errors
