#!/usr/bin/env bash
set -Eeuo pipefail

workspace_dir="${1:-$PWD}"

setup_op_socket() {
  bash "$workspace_dir/.devcontainer/postStartCommand.sh"
}

setup_ssh_config() {
  local github_auth_key_file="$HOME/.ssh/github-auth.pub"
  local github_auth_key_tmp

  install -d -m 700 "$HOME/.ssh"
  touch "$HOME/.ssh/known_hosts"

  github_auth_key_tmp="$(mktemp "${github_auth_key_file}.tmp.XXXXXX")"
  if ! op item get "GitHub Auth Key" --fields "label=public key" --reveal >"$github_auth_key_tmp"; then
    rm -f "$github_auth_key_tmp"
    echo "Unable to read the GitHub authentication public key from 1Password" >&2
    return 1
  fi
  chmod 600 "$github_auth_key_tmp"
  mv "$github_auth_key_tmp" "$github_auth_key_file"

  cp "$workspace_dir/.devcontainer/templates/.sshconfig" "$HOME/.ssh/config"

  chmod 600 "$HOME/.ssh/config" "$HOME/.ssh/known_hosts"
}

setup_git_config() {
  local ssh_keygen_bin

  ssh_keygen_bin="$(command -v ssh-keygen)"

  sed \
    -e "s|__SSH_KEYGEN__|$ssh_keygen_bin|g" \
    "$workspace_dir/.devcontainer/templates/.gitconfig" >"$HOME/.gitconfig"

  mkdir -p "$HOME/.config/git"
  git config --file "$HOME/.config/git/config" --replace-all safe.directory "$workspace_dir"
}

run_step() {
  local label="$1"
  local log_file
  shift

  log_file="$(mktemp)"
  printf '%s\n' "$label"

  if "$@" >"$log_file" 2>&1; then
    rm -f "$log_file"
    return 0
  fi

  cat "$log_file" >&2
  rm -f "$log_file"
  return 1
}

on_create() {
  mkdir -p "$HOME/.config"
  run_step "Setting up 1Password CLI socket" setup_op_socket
  run_step "Setting up SSH config" setup_ssh_config
  run_step "Setting up git config" setup_git_config
}

on_create
