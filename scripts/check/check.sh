#!/usr/bin/env bash
set -euo pipefail
dotnet restore backend/KnowledgeApp.slnx
dotnet build backend/KnowledgeApp.slnx --no-restore
dotnet test backend/KnowledgeApp.slnx --no-build
pnpm --filter desktop lint
pnpm --filter desktop typecheck
pnpm --filter desktop format:check
pnpm --filter desktop build
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -NoProfile -ExecutionPolicy Bypass -File "$script_dir/check-colors.ps1"
