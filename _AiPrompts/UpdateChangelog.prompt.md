---
mode: agent
---

# Task: Update Changelog and Release Version

Perform all steps below without stopping to prompt the user for input. Use the information already available in the repository to make all decisions automatically.

## Step 1 — Find the last released version commit

In the git repository located at the `gitWorkspace` folder (not any subfolder), run:

```
git log --oneline -60
```

Find the most recent commit whose message starts with the pattern `vX.Y.ZrW` (e.g. `v1.0.1r1`). That is the **last released version**. Note its commit hash, version string, and the date from the changelog file for that version.

## Step 2 — Collect commits since the last release

Run:

```
git log <last_release_hash>..HEAD --format="%H%n%s%n%b%n---COMMIT_END---"
```

Collect all commit messages to use as the source of changelog content.

## Step 3 — Determine the target version

Read the `Version` tag from the main project's `.csproj` file (`BelzontWE/BelzontWE.csproj`). The value is in `X.Y.Z.W` format, which maps to `vX.Y.ZrW`. **This is the version to use for the new changelog entry.** Do not ask the user — use this value directly. If the value is the same of last version, assume it's a patch update and increment the `rW` part by 1 (e.g. `v1.0.1r1` → `v1.0.1r2`) and also update the project file's tag.

## Step 4 — Determine changelog history behavior

Compare the date of the last released version (from the changelog heading) with today's date:

- If **less than 1 month** has passed: keep the previous changelog content, but change the `#` heading of the previous version to `## FROM` (or if it is already `## FROM`, leave it as `## FROM`; any deeper `## FROM` headings become just `##`).
- If **1 month or more** has passed: discard the previous changelog content entirely. Do not include any `## FROM` sections.

## Step 5 — Write the changelog

Update `BelzontWE/changelog.md`. Replace the entire file content with the new version at the top, followed by historical entries (if applicable per Step 4).

The new top entry format:
```
# vX.Y.ZrW (DD-MMM-YY)
```
Use today's date for the entry date. NOTE: Month must be english abbreviated form (e.g. `JAN`, `FEB`, `MAR`, etc.).

Organize the bullet points in this order:
1. **New features added** — with a short phrase showing how to use it
2. **Behaviors changed** — features that already existed but now work differently
3. **Bugs fixed** — always start with `FIXED:` and describe what WAS happening before the fix

Source all content from the commit messages collected in Step 2. Do not invent or pad entries.

## Step 6 — Commit

Stage and commit only `BelzontWE/changelog.md` (the `.csproj` is already correct since the version was read from it):

```
git add BelzontWE/changelog.md
git commit -m "vX.Y.ZrW\nUpdating Changelog"
```

Replace `vX.Y.ZrW` with the actual version string.
