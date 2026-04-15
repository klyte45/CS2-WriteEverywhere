**Start time:** 2026-04-15 03:27 -0300
# [0107] fix-stream-handler-make-readable

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a user**, I want **the texture atlas preview in the UI to display correctly after BC7 conversion**, so that I **the atlas manager panel shows the correct images even after optimization**.

---

## Background

GameUIResourceHandlerOverrides.BeforeOnResourceStreamRequest (~line 106) calls textureAtlas.Main_preview.EncodeToPNG() directly. After BC7 conversion Main_preview is a GPU-only BC7 texture (isReadable=false), so EncodeToPNG() returns empty or corrupt data. The non-streaming handler BeforeOnResourceRequest already correctly uses MakeReadable(out isCopy) and destroys the copy — the stream handler needs the same fix.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] BeforeOnResourceStreamRequest uses MakeReadable before EncodeToPNG
- [ ] Temp texture is Destroy'd after encoding when isCopy is true
- [ ] Build compiles 0 errors

---

## Implementation Notes

1. In GameUIResourceHandlerOverrides.BeforeOnResourceStreamRequest, in the '_textureAtlas' branch:
2. Replace: response.SetStreamReader(new StreamReader(textureAtlas.Main_preview.EncodeToPNG()))
3. With: var tempStream = textureAtlas.Main_preview.MakeReadable(out var isStreamCopy); var pngData = tempStream.EncodeToPNG(); if (isStreamCopy) GameObject.Destroy(tempStream); response.SetStreamReader(new StreamReader(pngData));

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


