#!/usr/bin/env bash
set -euo pipefail

suffix="${1:-auto}"

latest_fork_tag_any_suffix()
{
  git tag --merged HEAD --list 'v*-bin*' --sort=-v:refname |
    while IFS= read -r tag; do
      if [[ "${tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+-bin[0-9]+(\.[0-9]+)?$ ]]; then
        echo "${tag}"
        break
      fi
    done
}

latest_fork_tag_for_upstream()
{
  local upstream_tag="${1:?upstream tag is required}"

  git tag --merged HEAD --list "${upstream_tag}-bin*" --sort=-v:refname |
    while IFS= read -r tag; do
      if [[ "${tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+-bin[0-9]+(\.[0-9]+)?$ ]]; then
        echo "${tag}"
        break
      fi
    done
}

next_fork_suffix()
{
  local suffix="${1:?suffix is required}"

  if [[ "${suffix}" =~ ^(bin[0-9]+)\.([0-9]+)$ ]]; then
    echo "${BASH_REMATCH[1]}.$((BASH_REMATCH[2] + 1))"
    return 0
  fi

  if [[ "${suffix}" =~ ^bin[0-9]+$ ]]; then
    echo "${suffix}.1"
    return 0
  fi

  echo "Unable to increment fork suffix: ${suffix}" >&2
  return 1
}

if [[ "${GITHUB_REF_TYPE:-}" == "tag" && "${GITHUB_REF_NAME:-}" =~ ^(v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)-(.+)$ ]]; then
  upstream_tag="${BASH_REMATCH[1]}"
  suffix="${BASH_REMATCH[2]}"
else
  upstream_tag="$(
    git tag --merged HEAD --list 'v*' --sort=-v:refname |
      grep -E '^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' |
      head -n 1
  )"
fi

if [[ -z "${upstream_tag}" ]]; then
  echo "Unable to resolve merged upstream tag from current commit" >&2
  exit 1
fi

if [[ -z "${suffix}" || "${suffix}" == "auto" ]]; then
  latest_current_upstream_fork_tag="$(latest_fork_tag_for_upstream "${upstream_tag}")"

  if [[ -n "${latest_current_upstream_fork_tag}" ]]; then
    suffix="$(next_fork_suffix "${latest_current_upstream_fork_tag##*-}")"
  else
    latest_fork_tag="$(latest_fork_tag_any_suffix)"
    if [[ -n "${latest_fork_tag}" ]]; then
      suffix="${latest_fork_tag##*-}"
    else
      suffix="bin1.1"
    fi
  fi
fi

release_version="${upstream_tag#v}"
image_version="${release_version}-${suffix}"
fork_tag="${upstream_tag}-${suffix}"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "upstream_tag=${upstream_tag}"
    echo "suffix=${suffix}"
    echo "release_version=${release_version}"
    echo "image_version=${image_version}"
    echo "fork_tag=${fork_tag}"
  } >> "${GITHUB_OUTPUT}"
else
  cat <<EOF
upstream_tag=${upstream_tag}
suffix=${suffix}
release_version=${release_version}
image_version=${image_version}
fork_tag=${fork_tag}
EOF
fi
