# Repository Operating Model

This fork keeps upstream Radarr updates separate from fork development and Docker publishing.

## Branch Roles

- `master`: 1:1 upstream mirror of `Radarr/Radarr`. Do not add fork-specific code here.
- `main`: fork stable branch. Contains upstream Radarr plus this fork's custom changes, Dockerfile, workflows, and release scripts. Fork operating mechanics such as GitHub workflows, release scripts, Docker publishing configuration, and this file may be maintained directly here.
- `feat/*`: development branches. Use these for Radarr application changes and larger work in progress. Never publish Docker images from these branches.
- `sync/upstream-v*`: temporary upstream integration branches. Automation merges a new upstream tag into `main` through these branches and opens a PR.

## Workflow Rules

- Do not make Radarr application/source changes directly on `main` except for emergency fixes.
- GitHub workflow, release automation, Docker publishing, and repository operating-model changes may be made directly on `main` because they define how the fork is maintained.
- Do not publish Docker images from branch pushes.
- Docker publishing should happen only from fork release tags such as `v6.3.0.10514-bin1.1`, by explicit manual workflow dispatch, or after a `sync/upstream-v*` PR is merged into `main`.
- Scheduled upstream sync should target `main` by default.
- Upstream sync must create or update `sync/upstream-v*` and open a PR into `main`; it must not silently succeed just because the sync branch already exists.

## Release Flow

1. Upstream publishes a tag, for example `v6.3.0.10514`.
2. Sync automation creates `sync/upstream-v6.3.0.10514` from `main`.
3. The upstream tag is merged into that sync branch.
4. A PR is opened from `sync/upstream-v6.3.0.10514` to `main`.
5. CI must pass before merging.
6. After merge, Docker publish builds from `main` and creates a fork tag such as `v6.3.0.10514-bin1.1`.
7. If the automatic publish does not run, manually dispatch Docker publish or push the fork tag.

## Expected Image Tags

Use a stable fork suffix across upstream-only updates. If upstream changes but the fork layer does not, keep the same fork version suffix on the new upstream version.

For fork tag `v6.3.0.10514-bin1.1`, publish:

- `ghcr.io/gravelfreeman/radarr:6.3.0.10514-bin1.1`
- `ghcr.io/gravelfreeman/radarr:latest`
- `ghcr.io/gravelfreeman/radarr:latest-bin1.1`

Examples:

- Upstream-only update: `v6.3.0.10514-bin1.1` -> `v6.3.1.10550-bin1.1`
- Fork-only update on the same upstream: `v6.3.0.10514-bin1.1` -> `v6.3.0.10514-bin1.2`
- Keep incrementing fork-only updates within the same fork line, up to `bin1.9999` if needed.

## Agent Notes

- Keep `.github/workflows/*`, `.github/scripts/*`, and `Dockerfile` on `main`; they are part of the fork's stable operating model.
- `feat/*` branches may edit workflows or Docker files, but those changes become active only after merging to `main`.
- Avoid renaming branches or changing publish triggers without also updating this file and the workflow defaults.
