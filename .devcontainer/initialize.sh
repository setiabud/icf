#!/usr/bin/env bash
set -euo pipefail

if [[ ! -f .env ]]; then
  printf '%s\n' 'Missing root .env. Follow docs/guides/devcontainer.md to configure PostgreSQL credentials.' >&2
  exit 1
fi

for certificate_file in icfaccounting-web.pem icfaccounting-web.key; do
  if [[ ! -r "$HOME/.aspnet/https/$certificate_file" ]]; then
    printf 'Missing readable certificate file: %s. Follow docs/guides/devcontainer.md to export the host-trusted certificate.\n' "$HOME/.aspnet/https/$certificate_file" >&2
    exit 1
  fi
done

docker compose --env-file .env -f compose.json -f .devcontainer/compose.yaml config --quiet
