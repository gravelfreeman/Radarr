#!/usr/bin/env bash
set -euo pipefail

base_branch="${BASE_BRANCH:?BASE_BRANCH is required}"
upstream_repository="${UPSTREAM_REPOSITORY:?UPSTREAM_REPOSITORY is required}"
fork_suffix="${FORK_SUFFIX:-auto}"
latest_upstream_tag="${UPSTREAM_TAG_OVERRIDE:-}"
github_repository="${GITHUB_REPOSITORY:-}"

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

if [[ -z "${github_repository}" ]]; then
  github_repository="$(
    git remote get-url origin |
      sed -E 's#^https://github.com/##; s#^git@github.com:##; s#\.git$##'
  )"
fi

latest_fork_tag_for_suffix()
{
  local suffix="${1:?suffix is required}"

  git tag -l "v*-${suffix}" --sort=-v:refname |
    while IFS= read -r tag; do
      if [[ "${tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+- && "${tag##*-}" == "${suffix}" ]]; then
        echo "${tag}"
        break
      fi
    done
}

latest_fork_tag_any_suffix()
{
  git tag -l 'v*-bin*' --sort=-v:refname |
    while IFS= read -r tag; do
      if [[ "${tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+-bin[0-9]+(\.[0-9]+)?$ ]]; then
        echo "${tag}"
        break
      fi
    done
}

if [[ -z "${fork_suffix}" || "${fork_suffix}" == "auto" ]]; then
  latest_fork_tag="$(latest_fork_tag_any_suffix)"
  if [[ -n "${latest_fork_tag}" ]]; then
    fork_suffix="${latest_fork_tag##*-}"
  else
    fork_suffix="bin1.1"
  fi
else
  latest_fork_tag="$(latest_fork_tag_for_suffix "${fork_suffix}")"
fi

latest_fork_upstream="${latest_fork_tag%-${fork_suffix}}"
current_base_upstream_tag="$(
  git tag --merged "origin/${base_branch}" --list 'v*' --sort=-v:refname |
    grep -E '^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' |
    head -n 1 || true
)"
sync_branch="sync/upstream-${latest_upstream_tag}"
remote_sync_ref="refs/remotes/origin/${sync_branch}"

echo "Latest upstream tag: ${latest_upstream_tag}"
echo "Latest fork tag: ${latest_fork_tag:-<none>}"
echo "Current base upstream tag: ${current_base_upstream_tag:-<none>}"

if [[ "${current_base_upstream_tag}" == "${latest_upstream_tag}" ]]; then
  echo "Base branch already includes ${latest_upstream_tag}"
  exit 0
fi

if [[ "${latest_fork_upstream}" == "${latest_upstream_tag}" ]]; then
  echo "Fork tag already exists for ${latest_upstream_tag}; continuing because base branch is not synced yet"
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

create_auto_pr()
{
  local pr_body
  pr_body=$(cat <<EOF
Automated upstream sync for \`${latest_upstream_tag}\`.

- upstream repository: \`${upstream_repository}\`
- base branch: \`${base_branch}\`
- image suffix: \`${fork_suffix}\`

This PR was created automatically because the merge completed without conflicts.
If CI is green, auto-merge is enabled.
EOF
)

  local pr_url
  pr_url="$(
    create_pr \
      "${base_branch}" \
      "${sync_branch}" \
      "${auto_title}" \
      "${pr_body}"
  )"

  gh pr merge "${pr_url}" --auto --merge || true
}

create_manual_pr()
{
  local conflict_files="${1:-Unable to resolve conflict file list}"
  local pr_body
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

  create_pr \
    "${base_branch}" \
    "${sync_branch}" \
    "${manual_title}" \
    "${pr_body}" \
    true
}

create_pr()
{
  local base="${1:?base branch is required}"
  local head="${2:?head branch is required}"
  local title="${3:?title is required}"
  local body="${4:?body is required}"
  local draft="${5:-false}"

  gh api \
    --method POST \
    "repos/${github_repository}/pulls" \
    --raw-field "base=${base}" \
    --raw-field "head=${head}" \
    --raw-field "title=${title}" \
    --raw-field "body=${body}" \
    --field "draft=${draft}" \
    --jq '.html_url'
}

git checkout -B "${sync_branch}" "origin/${base_branch}"

merge_message="merge: upstream ${latest_upstream_tag}"
manual_title="chore: manual upstream sync ${latest_upstream_tag}"
auto_title="chore: sync upstream ${latest_upstream_tag}"

if git ls-remote --exit-code --heads origin "${sync_branch}" >/dev/null 2>&1; then
  echo "Sync branch already exists: ${sync_branch}"
  git fetch origin "+refs/heads/${sync_branch}:${remote_sync_ref}"

  if git merge-base --is-ancestor "refs/tags/${latest_upstream_tag}" "${remote_sync_ref}"; then
    echo "Existing sync branch contains ${latest_upstream_tag}; creating missing PR"
    create_auto_pr
    exit 0
  fi

  echo "Existing sync branch does not contain ${latest_upstream_tag}; creating manual PR"
  create_manual_pr "Existing sync branch ${sync_branch} does not contain ${latest_upstream_tag}."
  exit 0
fi

if git merge --no-ff "refs/tags/${latest_upstream_tag}" -m "${merge_message}"; then
  git push origin "${sync_branch}"
  create_auto_pr
  exit 0
fi

conflict_files="$(
  git diff --name-only --diff-filter=U || true
)"

git merge --abort || true
git checkout -B "${sync_branch}" "origin/${base_branch}"
git commit --allow-empty -m "${manual_title}"
git push origin "${sync_branch}"

create_manual_pr "${conflict_files}"
