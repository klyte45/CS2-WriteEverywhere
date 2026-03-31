# Task project workspace - Klyte Kanban CLI format

This folder is used to organize all actionable improvement tasks to be done on the project.

Use the \`npx kk\` CLI tool to manage tasks, sprints, and workspace metadata.

## Folder structure

### ActiveSprint/

Tasks currently being worked on in the active sprint. Each task is a markdown file following the naming convention described below.

### Backlog/

Tasks not yet scheduled for a sprint. Organized by priority and status.

### Archive/

Completed or cancelled tasks moved here at the end of each sprint.

### RefsLibrary/

Reference documents, research notes, and design decisions related to the project.

## Task file naming convention

```
X[_epic]_N_AAAA_resume-of-task.md
```

- `X`     — Status letter: N (New) | P (Progress) | T (Terminated) | Z (Cancelled) | ...
- `epic`  — Optional epic label: 3–15 lowercase alphanumeric characters
- `N`     — Priority digit: 0 (Very High) – 4 (Very Low)
- `AAAA`  — Unique sequential ID, 4+ zero-padded digits
- `title` — kebab-case description

## Mutable data

- Last task ID: 0000
- Last sprint number: 000
