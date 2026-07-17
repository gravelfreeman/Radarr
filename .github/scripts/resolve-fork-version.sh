#!/usr/bin/env bash
set -euo pipefail

suffix="${1:-bin1.1}"

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
