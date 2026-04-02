# [0048] Tests for WETextDataXml.cs object graph and XML structure (>=20 tests)

**Developed by:** 

## User Story

> Acting as **a developer**, I want **tests that cover the full WETextDataXml XML serialization model**, so that I **property bindings and XML attributes are correct**.

---

## Background

[See epic: tst-io-xml](..\RefsLibrary\2026040123_testing-action-plan\epics\05_Epic_io-xml.md)

Task IX-02. Test WETextDataXml XML serialization model: property round-trips, nested sub-tree serialization, valid XML output, version field present, FromXML() on garbage XML returns null or throws InvalidOperationException.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] IO/WETextDataXmlTests.cs exists with >=20 test methods
- [ ] Tests cover: property round-trips for all primitive fields, nested sub-tree serialization, ToXML() output is valid XML (XDocument.Parse does not throw), version field present, FromXML() on garbage XML returns null or throws InvalidOperationException (not NullReferenceException)

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


