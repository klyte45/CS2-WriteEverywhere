**Start time:** 2026-04-21 12:01 -0300
# [0116] Add GameProp Enum and Data Components

**Developed by:** Claude Sonnet 4.5 (claude-sonnet-4-5@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **WESimulationTextType.GameProp=7 and all supporting data components to be available**, so that I **subsequent systems can compile and reference the new type without errors**.

---

## Background

Foundational types for the GameProp feature. No logic, just type declarations. See docs 01+04 in RefsLibrary/2026042100_GamePropResearch/. WESubObject: buffer of spawned prop entity refs on GameProp text node. WEOwner: ICleanupComponentData pointing to the GameProp WE text node. WEChild: IEmptySerializable stale marker. WEInheritedVarsCache: IEnableableComponent holding inheritableVars for spawned props.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] WESimulationTextType.GameProp = 7 added after WhiteCube=6 and project compiles
- [ ] WESubObject.cs created: IBufferElementData+ICleanupBufferElementData, field Entity m_SubObject
- [ ] WEOwner.cs created: IComponentData+ICleanupComponentData, field Entity m_weOwnerEntity, NOT ISerializable
- [ ] WEChild.cs created: IComponentData+IEmptySerializable (serialized stale marker, survives save/load)
- [ ] WEInheritedVarsCache.cs created: IComponentData+IEnableableComponent, field FixedString512Bytes vars
- [ ] All existing tests continue to pass

---

## Implementation Notes

1. WESimulationTextType.cs: add GameProp=7 after WhiteCube=6
2. WESubObject.cs: new file in BelzontWE/Components/
3. WEOwner.cs: new file in BelzontWE/Components/; must NOT implement ISerializable
4. WEChild.cs: new file in BelzontWE/Components/; implements IEmptySerializable so it survives save/load
5. WEInheritedVarsCache.cs: new file in BelzontWE/Components/; IEnableableComponent

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


