# [0090] Font text generation jobs is somehow stalling on heavy load

**Developed by:** 

## User Story

> Acting as **a player loading a city with many WE layouts (1000+)**, I want **font text generation to fully process all queued strings on city load without permanently stalling**, so that I **all WE texts appear rendered within a few seconds of loading instead of some texts being permanently blank**.

---

## Background

Font text rendering in `FontSystem.cs` uses a `NativeQueue<StringRenderingQueueItem>` (`itemsQueue`) that is drained in `ScheduleJobs`. Each frame, at most `WEConstants.STRING_RENDERING_BATCH` (256) items are dequeued and processed in a parallel Burst job.

Two stall mechanisms have been identified:

**1. Atlas-reset re-queue storm (primary suspect):** When `PostJob` detects that a rendered string's `AtlasVersion` does not match `CurrentAtlas.Version` (because the atlas was expanded or reset during the same frame's glyph preparation), affected items are re-enqueued (`itemsQueueWriter.Enqueue(item)`). If the atlas resets every frame for the same large glyph set (e.g., a city with many unique characters filling a 8192×8192 atlas that continuously resets), all 256 items are re-queued every frame and the queue never shrinks.

**2. `byte framesBuffering` overflow (secondary):** `framesBuffering` is `private byte` and increments until it wraps at 256. The flush condition `framesBuffering++ > 60` only fires every 60+ frames, but since the byte wraps silently at 256, a cycle of 256 low-traffic frames resets the counter without processing. This means a queue with fewer than 256 strings can silently stall for up to 256 extra frames.

**3. `postJobCounter > 256` check is unreachable:** In `ProcessResults`, the early-break condition `if (++postJobCounter > STRING_RENDERING_BATCH)` can never trigger in normal operation because `ScheduleJobs` sends at most 256 items to the job which produces at most 256 results. The accompanying log message 'Skipping next frame' never fires, masking accumulation.

If the root cause cannot be confirmed from code inspection alone (e.g., the stall only reproduces on a heavy city), the minimum deliverable for this task is **diagnostic instrumentation** — unconditional warning logs for queue-depth and re-queue counters — plus a follow-up task with specific log collection instructions.

---

## Definition of Ready (DoR)

- [ ] `FontSystem.cs` `ScheduleJobs` and `ProcessResults` methods are read in full
- [ ] `framesBuffering` field type (`byte`) is confirmed
- [ ] The `PostJob` method's re-enqueue conditions are understood (AtlasVersion mismatch and null mesh fill)
- [ ] Confirmed `WEConstants.STRING_RENDERING_BATCH = 256` is used in both schedule and process paths

---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] Option A (fix confirmed): At least one of the stall conditions is resolved — `framesBuffering` promoted to `int`, and/or re-queue storm is bounded (e.g. per-string re-queue counter with eviction after N retries)
- [ ] Option A (fix confirmed): No unconditional log spam is introduced — new logs are gated behind `DebugMode` or fire at most once per queue-drain cycle
- [ ] Option B (diagnostic only, if root cause unconfirmed): `FontSystem.ScheduleJobs` logs queue depth unconditionally at most once per 60 frames; `ProcessResults` logs re-queue count when > 0; a new follow-up task is created in the backlog with instructions for collecting the diagnostic logs
- [ ] Project compiles without errors
- [ ] Existing text rendering behavior is unchanged under normal load

---

## Implementation Notes

1. Fix `framesBuffering` type: change `private byte framesBuffering = 0` to `private int framesBuffering = 0` in FontSystem.cs.
2. To bound re-queue storms: add a `Dictionary<FixedString512Bytes, int> m_reQueueCount` field in FontSystem. In `PostJob`, increment the counter for re-queued items. If the counter exceeds a threshold (e.g., 10), log a warning and skip the item (treat as unrenderable for this session).
3. If pursuing Option B: wrap a queue-depth log in `ScheduleJobs` with `if (BasicIMod.DebugMode && itemsQueue.Count > WEConstants.STRING_RENDERING_BATCH * 2) LogUtils.DoWarnLog(...)`; add a `reQueueThisFrame` counter in ProcessResults and log it once per call if > 0 in DebugMode.
4. The `postJobCounter > STRING_RENDERING_BATCH` check in `ProcessResults` is effectively dead — if it should be a meaningful guard, change the condition to `postJobCounter > results.Count * 2` or remove it; if kept, lower the threshold so it can actually fire.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


