$ErrorActionPreference = "Stop"
dotnet restore backend/KnowledgeApp.slnx
dotnet build backend/KnowledgeApp.slnx --no-restore
dotnet test backend/KnowledgeApp.slnx --no-build
pnpm.cmd --filter desktop lint
pnpm.cmd --filter desktop typecheck
pnpm.cmd --filter desktop format:check
pnpm.cmd --filter desktop build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check-colors.ps1
