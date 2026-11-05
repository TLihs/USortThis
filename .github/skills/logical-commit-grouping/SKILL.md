---
name: logical-commit-grouping
description: Use when the user asks to commit repository changes as separate, logically grouped commits. Inspect the worktree, identify cohesive change groups, then stage, review, and commit one group at a time.
---

# Logical Commit Grouping

Create a clear commit history by separating unrelated changes and completing each commit before starting the next.

## Workflow

1. Confirm the user explicitly asked for commits. If they only asked to inspect or group changes, report the proposed groups without staging or committing.
2. Inspect the current branch, `git status --short`, staged and unstaged diffs, and recent commit-message style. Identify pre-existing staged changes and preserve them; do not assume all working-tree changes belong to this task.
3. Group changes by cohesive purpose, such as a feature, a bug fix, tests for that change, or a documentation-only update. Order groups so prerequisites come before dependent changes. Keep unrelated changes separate. If a file contains changes for multiple groups, stage only the relevant hunks.
4. Before staging, summarize the proposed groups when the grouping is not obvious or when pre-existing staged/user changes could be affected. Ask the user if intent is unclear; never guess about including unrelated or sensitive changes.
5. For each group, in order:
   - Stage only that group's files or hunks; do not use broad staging such as `git add -A` when it could include unrelated files.
   - Review `git diff --cached` and `git diff --cached --check`. Confirm the staged diff contains only the intended group and no secrets, generated output, or machine-specific files.
   - Run relevant validation when practical. Do not commit a group if validation for that change fails; report the failure and stop for guidance.
   - Commit this group alone, using a concise imperative message consistent with recent repository history.
   - Verify the commit succeeded and inspect `git status --short` before proceeding to the next group. Stop if unexpected changes appear or the commit fails.
6. After the final group, report each commit's message/hash and any remaining staged or unstaged changes. Never claim the worktree is clean unless verified.

## Safety

- Do not amend existing commits, rewrite history, force-push, discard changes, reset the index, or stash changes unless the user explicitly requests it.
- Preserve unrelated staged and unstaged work. If safe separation is not possible, pause and ask the user rather than altering it.
- Do not combine all groups into one commit or start multiple commits in a batch; complete the inspect-stage-review-validate-commit-verify cycle one group at a time.
