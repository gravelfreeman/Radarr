#!/usr/bin/env bash
set -Eeuo pipefail

workspace_dir="${1:-$PWD}"

cd "$workspace_dir"

corepack enable
corepack prepare yarn@1.22.19 --activate

git config --global --add safe.directory "$workspace_dir"
git lfs install --skip-repo

yarn install --frozen-lockfile --network-timeout 120000
dotnet restore src/Radarr.sln
