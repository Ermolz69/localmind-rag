#!/usr/bin/env bash
set -euo pipefail
dotnet publish backend/src/KnowledgeApp.LocalApi/KnowledgeApp.LocalApi.csproj -c Release -r win-x64 --self-contained true
pnpm --filter desktop build
