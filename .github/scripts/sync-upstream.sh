#!/usr/bin/env bash
set -euo pipefail

base_branch="${BASE_BRANCH:?BASE_BRANCH is required}"
upstream_repository="${UPSTREAM_REPOSITORY:?UPSTREAM_REPOSITORY is required}"
fork_suffix="${FORK_SUFFIX:-bin1}"
latest_upstream_tag="${UPSTREAM_TAG_OVERRIDE:-}"

git config user.name "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"

git remote remove upstream 2>/dev/null || true
git remote add upstream "https://github.com/${upstream_repository}.git"

git fetch origin "${base_branch}" --tags
git fetch upstream --tags

if [[ -z "${latest_upstream_tag}" ]]; then
  latest_upstream_tag="$(
    git ls-remote --tags --refs "https://github.com/${upstream_repository}.git" |
      sed 's#.*refs/tags/##' |
      grep -E '^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' |
      sort -V |
      tail -n 1
  )"
fi

if [[ -z "${latest_upstream_tag}" ]]; then
  echo "Unable to resolve latest upstream tag" >&2
  exit 1
fi

latest_fork_tag="$(
  git tag -l "v*-${fork_suffix}" --sort=-v:refname |
    grep -E "^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+-${fork_suffix}$" |
    head -n 1 || true
)"

latest_fork_upstream="${latest_fork_tag%-${fork_suffix}}"
current_base_upstream_tag="$(
  git tag --merged "origin/${base_branch}" --list 'v*' --sort=-v:refname |
    grep -E '^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' |
    head -n 1 || true
)"
sync_branch="sync/upstream-${latest_upstream_tag}"

echo "Latest upstream tag: ${latest_upstream_tag}"
echo "Latest fork tag: ${latest_fork_tag:-<none>}"
echo "Current base upstream tag: ${current_base_upstream_tag:-<none>}"

if [[ "${latest_fork_upstream}" == "${latest_upstream_tag}" ]]; then
  echo "Fork already published for ${latest_upstream_tag}"
  exit 0
fi

if [[ "${current_base_upstream_tag}" == "${latest_upstream_tag}" ]]; then
  echo "Base branch already includes ${latest_upstream_tag}"
  exit 0
fi

if git ls-remote --exit-code --heads origin "${sync_branch}" >/dev/null 2>&1; then
  echo "Sync branch already exists: ${sync_branch}"
  exit 0
fi

existing_pr="$(
  gh pr list \
    --base "${base_branch}" \
    --head "${sync_branch}" \
    --state open \
    --json number \
    --jq '.[0].number // empty'
)"

if [[ -n "${existing_pr}" ]]; then
  echo "Existing PR already open: #${existing_pr}"
  exit 0
fi

git checkout -B "${sync_branch}" "origin/${base_branch}"

merge_message="merge: upstream ${latest_upstream_tag}"
manual_title="chore: manual upstream sync ${latest_upstream_tag}"
auto_title="chore: sync upstream ${latest_upstream_tag}"

if git merge --no-ff "refs/tags/${latest_upstream_tag}" -m "${merge_message}"; then
  git push origin "${sync_branch}"

  pr_body=$(cat <<EOF
Automated upstream sync for \`${latest_upstream_tag}\`.

- upstream repository: \`${upstream_repository}\`
- base branch: \`${base_branch}\`
- image suffix: \`${fork_suffix}\`

This PR was created automatically because the merge completed without conflicts.
If CI is green, auto-merge is enabled.
EOF
)

  pr_url="$(
    gh pr create \
      --base "${base_branch}" \
      --head "${sync_branch}" \
      --title "${auto_title}" \
      --body "${pr_body}"
  )"

  gh pr merge "${pr_url}" --auto --merge || true
  exit 0
fi

conflict_files="$(
  git diff --name-only --diff-filter=U || true
)"

git merge --abort || true
git checkout -B "${sync_branch}" "origin/${base_branch}"
git commit --allow-empty -m "${manual_title}"
git push origin "${sync_branch}"

pr_body=$(cat <<EOF
Automated upstream sync for \`${latest_upstream_tag}\` could not be merged cleanly.

- upstream repository: \`${upstream_repository}\`
- base branch: \`${base_branch}\`
- image suffix: \`${fork_suffix}\`

Manual merge work is required.

Conflicting files detected during the automated merge attempt:

\`\`\`
${conflict_files:-Unable to resolve conflict file list}
\`\`\`

Recommended manual flow:

1. fetch \`${latest_upstream_tag}\` from upstream
2. merge it into \`${base_branch}\`
3. resolve conflicts
4. keep the fork-specific recycle bin changes intact
5. merge this PR only after the real sync branch is ready
EOF
)

gh pr create \
  --base "${base_branch}" \
  --head "${sync_branch}" \
  --title "${manual_title}" \
  --body "${pr_body}" \
  --draft
