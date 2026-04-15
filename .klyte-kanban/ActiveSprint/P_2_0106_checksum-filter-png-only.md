**Start time:** 2026-04-15 03:14 -0300
# [0106] checksum-filter-png-only

**Developed by:** Claude Sonnet 4.6 (claude-sonnet-4-6@kwytco.com.br)
## User Story

> Acting as **a developer**, I want **the folder checksum to only reflect PNG image files**, so that I **non-image files in the atlas folder do not cause unnecessary atlas rebuilds**.

---

## Background

WEChecksumUtils.ComputeFolderChecksum currently hashes ALL files in the directory. Only .png files are loaded by WEAtlasLoadingUtils. For bridge file-based atlases the full path must be included in the hash key (not just filename) to prevent checksum collisions between mods that use the same image file names. A new overload is needed for memory-based producer atlases that hashes name + byte-array sizes.

---

## Definition of Ready (DoR)



---

## Acceptance Criteria / Definition of Done (DoD)

- [ ] ComputeFolderChecksum only hashes .png files (case-insensitive)
- [ ] Hash key includes full path not just filename
- [ ] ComputeFileListChecksum(IEnumerable<string>) added for bridge file-path based checksums
- [ ] ComputeBridgeMemoryChecksum added for producer-based atlases
- [ ] New tests ComputeFolderChecksum_NonPngIgnored and ComputeFileListChecksum_FullPath_Used pass (no Unity needed)

---

## Implementation Notes

1. In WEChecksumUtils.ComputeFolderChecksum: filter directory file list to *.png only (case-insensitive, using Path.GetExtension comparison).
2. Change the hashed string per file from '{filename}:{size}\0' to '{fullpath}:{size}\0' so bridges using absolute paths get unique checksums.
3. Add static uint ComputeFileListChecksum(IEnumerable<string> paths): hashes full path + File.Exists check + file size for each path (skip missing).
4. Add static uint ComputeBridgeMemoryChecksum(IEnumerable<(string Name, byte[] Main, byte[] ControlMask, byte[] MaskMap, byte[] Normal, byte[] Emissive, string XmlInfo)> entries): hashes Name + length of each non-null byte array per entry.
5. Update existing tests: previous folder-only tests should still pass; add new tests ComputeFolderChecksum_NonPngIgnored and ComputeFileListChecksum_FullPath_Used.

---

## Risk Assessment

| Risk | Likelihood | Mitigation |
|------|-----------|------------|

---

## Related Tasks

### Depends on



### Is dependent for


