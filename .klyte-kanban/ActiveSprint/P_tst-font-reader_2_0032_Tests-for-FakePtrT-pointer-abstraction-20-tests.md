**Start time:** 2026-04-02 03:06 -0300
# [0032] Tests for FakePtr<T> (pointer abstraction, >=20 tests)

**Developed by:** Claude-Sonnet-4-6 <claude-sonnet-4-6@kwytco.com.br>
## User Story

> Acting as **a developer**, I want **exhaustive tests for the FakePtr<T> pointer abstraction**, so that I **the foundation of the entire stbtt port is verified correct**.

---

## Background

[See epic: tst-font-reader](..\RefsLibrary\2026040123_testing-action-plan\epics\03_Epic_font-reader.md)

Task FR-01. Exhaustive tests for the FakePtr<T> pointer abstraction — the foundation of the entire stbtt port. Depends on tst-test-infra being complete.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Font/FileReader/FakePtrTests.cs exists with >=20 test methods
- [ ] Tests cover: construct from array, construct with offset, Value getter/setter, GetAndIncrease/SetAndIncrease, Clear(n), indexer read/write, null pointer (FakePtr<T>.Null.IsNull == true), shared array mutation, arithmetic ptr+n, copy constructor offset propagation
- [ ] At least one test verifies accessing FakePtr<T>.Null throws NullReferenceException or IndexOutOfRangeException

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


