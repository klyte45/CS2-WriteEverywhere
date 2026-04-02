**End time:** 2026-04-02 19:36 -0300
**Start time:** 2026-04-02 18:53 -0300
# [0047] Tests for WETextDataXmlTree.cs round-trip fidelity (>=15 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **tests that verify the XML serialization of WETextDataXmlTree produces the exact same object graph when deserialized**, so that I **saved layouts are never silently corrupted**.

---

## Background

[See epic: tst-io-xml](..\RefsLibrary\2026040123_testing-action-plan\epics\05_Epic_io-xml.md)

Task IX-01. Verify WETextDataXmlTree XML serialization: ToXML()->FromXML() deep-equal round-trip, XML structure (<WELayout>, <self>, <children>), null/empty input safety, Guid auto-generated, variables survive serialize/deserialize.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [x] IO/WETextDataXmlTreeTests.cs exists with >=15 test methods
- [x] Tests cover: ToXML()->FromXML() produces equal object (deep comparison), serialized XML contains <WELayout>, <self> and <children> structure, FromXML(null) returns null, FromXML("") returns null, children empty by default, ShouldSerializechildren() behavior, Guid non-empty, WETemplateVariable key/value survives serialization

---

## Implementation Notes



---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


