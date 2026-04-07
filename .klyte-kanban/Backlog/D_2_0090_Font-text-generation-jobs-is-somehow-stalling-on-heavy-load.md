# [0090] Font text generation jobs is somehow stalling on heavy load

**Developed by:** 

## User Story



---

## Background

I don't know why but the text generation system is getting stuck when a heavy load of words is done, like when a city is just loaded. I suspect it might be a limiter that is being overflowed and never make the queue goes ahead somehow - it already happened in the past on cities with more of 10000 layouts to be loaded, where a 10000 listing cap were not being enough to handle them all.

I believe that this kind of capping variables - if this is the root problem - can be removed; the mod is not suffering with performance as it was in the past. If there are no enough information for work on this (like: it need some log observing the flow before continue) so use this task to prepare the code for printing stuff in the tests and create a future task to avail the log results, to be incremented with the log information you added in this task along with the testing instructions.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)



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


