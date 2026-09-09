#!/usr/bin/env bash
set -Eeuo pipefail

cache_dir="$HOME/.cache/radarr-devcontainer"

install -d -m 0700 "$cache_dir"
rm -f "$cache_dir/ssh-agent.sock"

agent_socket=""
for candidate in \
  "${SSH_AUTH_SOCK:-}" \
  "$HOME/.1password/agent.sock" \
  /tmp/1password-ssh-agent.sock
do
  [ -n "$candidate" ] || continue
  [ -S "$candidate" ] || continue

  if SSH_AUTH_SOCK="$candidate" ssh-add -l >/dev/null 2>&1; then
    agent_socket="$candidate"
    break
  fi
done

if [ -z "$agent_socket" ]; then
  echo "Unable to find a working SSH agent socket" >&2
  exit 1
fi

ln -s "$agent_socket" "$cache_dir/ssh-agent.sock"
