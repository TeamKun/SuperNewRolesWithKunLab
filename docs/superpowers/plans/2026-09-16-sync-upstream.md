# Upstream Synchronization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve the pre-sync fork state, configure `sub` for the canonical SuperNewRoles repository, and make `master` exactly match the current `sub/master` tip.

**Architecture:** Keep the old fork history on a dedicated backup branch and annotated tag instead of resolving thousands of upstream changes into the old fork tip. Reset the primary branch to the fetched upstream commit, then update `origin/master` with a guarded force push. GitHub reports this repository as an independent repository (`fork: false`), so the 5.25-GiB object store was uploaded incrementally through a temporary staging branch before the final ref update. This leaves future upstream synchronization as a fast-forward operation.

**Tech Stack:** Git, GitHub HTTPS remotes, PowerShell

**Spec:** User request recorded in the Codex task started on 2026-09-16.

## Global Constraints

- The canonical upstream remote must be named `sub` and use `https://github.com/SuperNewRoles/SuperNewRoles`.
- Upstream content takes precedence over fork-specific changes.
- The old fork state must remain recoverable after synchronizing `master`.
- The remote primary branch must only be overwritten with `--force-with-lease` after refreshing `origin`.

---

### Task 1: Preserve the old fork state

**Files:**
- Create: `docs/superpowers/plans/2026-09-16-sync-upstream.md`

**Interfaces:**
- Consumes: the old fork tip `86bf23c03f22d4d28768339b289c465c168fef7b`
- Produces: branch `backup/pre-sub-sync-20260916` and tag `backup-pre-sub-sync-20260916`

- [x] **Step 1: Verify the working tree is clean**

  Run `git status --short --branch` and require no changed paths.

- [x] **Step 2: Fetch the canonical primary branch**

  Run `git fetch --no-tags --no-recurse-submodules sub master:refs/remotes/sub/master`.

- [x] **Step 3: Create recovery refs**

  Create `backup/pre-sub-sync-20260916` at the old tip and the annotated tag `backup-pre-sub-sync-20260916`.

- [x] **Step 4: Commit this recovery document on the backup branch**

  Run `git add docs/superpowers/plans/2026-09-16-sync-upstream.md` followed by `git commit -m "docs: record upstream sync recovery plan"`.

### Task 2: Replace the primary branch with upstream

**Files:**
- Modify: Git ref `refs/heads/master`

**Interfaces:**
- Consumes: `sub/master` at `4225b8a27df1660371d2ef43e81ce87514267a65`
- Produces: local `master` with the same tree and commit ID

- [x] **Step 1: Switch to the primary branch**

  Run `git switch master`.

- [x] **Step 2: Reset the primary branch**

  Run `git reset --hard sub/master`.

- [x] **Step 3: Configure upstream tracking**

  Run `git branch --set-upstream-to=sub/master master` so ordinary status and pull operations compare against the canonical repository.

### Task 3: Publish and verify the synchronized fork

**Files:**
- Modify: remote ref `origin/master`
- Create: remote branch `origin/backup/pre-sub-sync-20260916`
- Create: remote tag `backup-pre-sub-sync-20260916`

**Interfaces:**
- Consumes: the local backup refs and synchronized `master`
- Produces: a recoverable backup on `origin` and an updated fork primary branch

- [x] **Step 1: Publish the recovery refs**

  Push the backup branch and annotated tag to `origin` before replacing its primary branch.

- [x] **Step 2: Refresh the fork remote**

  Run `git fetch origin master` immediately before the guarded force push.

- [x] **Step 3: Upload upstream history incrementally**

  Create `sync/upstream-staging-20260916` at the common ancestor and advance it through all 135 first-parent checkpoints to avoid a single multi-gigabyte push. The direct push failed with GitHub HTTP 500; all incremental pushes succeeded.

- [x] **Step 4: Update the fork primary branch**

  Run a guarded force push with the expected old SHA: `git push --force-with-lease=refs/heads/master:86bf23c03f22d4d28768339b289c465c168fef7b origin master:master`.

- [x] **Step 5: Remove staging and verify commit identities**

  Delete `sync/upstream-staging-20260916`. Require `master`, `sub/master`, and `origin/master` to resolve to `4225b8a27df1660371d2ef43e81ce87514267a65`, and require `git status --short` to be empty.

## Recovery

- View the preserved fork state: `git switch backup/pre-sub-sync-20260916`
- Restore the old primary branch locally: `git switch master; git reset --hard backup-pre-sub-sync-20260916`
- Restore it remotely if ever required: `git push --force-with-lease origin backup-pre-sub-sync-20260916:master`
- The annotated tag points exactly to the original fork tip `86bf23c03f22d4d28768339b289c465c168fef7b`, independent of later documentation commits on the backup branch.
