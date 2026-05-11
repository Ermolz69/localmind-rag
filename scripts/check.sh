#!/usr/bin/env bash
set -euo pipefail
dotnet restore backend/KnowledgeApp.slnx
dotnet build backend/KnowledgeApp.slnx --no-restore
dotnet test backend/KnowledgeApp.slnx --no-build
pnpm --filter desktop lint
pnpm --filter desktop typecheck
pnpm --filter desktop format:check
pnpm --filter desktop build
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/check-colors.ps1
