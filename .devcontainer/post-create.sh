#!/usr/bin/env bash
set -euo pipefail

sudo chown vscode:vscode \
  /home/vscode/.cache \
  /home/vscode/.cache/icf \
  /home/vscode/.nuget \
  /home/vscode/.nuget/packages \
  /workspaces/icf/src/ICFAccounting.Web/node_modules

for certificate_file in icfaccounting-web.pem icfaccounting-web.key; do
  if [[ ! -r "$HOME/.aspnet/https/$certificate_file" ]]; then
    printf 'Cannot read mounted certificate file: %s. Check host file permissions.\n' "$certificate_file" >&2
    exit 1
  fi
done

dotnet restore ICFAccounting.slnx
dotnet tool restore
npm ci --prefix src/ICFAccounting.Web
docker info > /dev/null
