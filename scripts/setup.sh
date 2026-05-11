#!/usr/bin/env bash
set -euo pipefail
dotnet restore backend/KnowledgeApp.slnx
pnpm install
