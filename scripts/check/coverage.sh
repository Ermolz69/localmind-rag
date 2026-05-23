#!/usr/bin/env bash
set -euo pipefail

test_results_dir="artifacts/test-results/local"
coverage_report_dir="artifacts/coverage-report/local"

rm -rf "$test_results_dir" "$coverage_report_dir"

dotnet restore backend/KnowledgeApp.slnx
dotnet build backend/KnowledgeApp.slnx --no-restore

dotnet test backend/tests/KnowledgeApp.UnitTests/KnowledgeApp.UnitTests.csproj \
  --no-build \
  --logger "trx" \
  --results-directory "$test_results_dir/unit" \
  --collect:"XPlat Code Coverage" \
  -- \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet test backend/tests/KnowledgeApp.IntegrationTests/KnowledgeApp.IntegrationTests.csproj \
  --no-build \
  --logger "trx" \
  --results-directory "$test_results_dir/integration" \
  --collect:"XPlat Code Coverage" \
  -- \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet test backend/tests/KnowledgeApp.ArchitectureTests/KnowledgeApp.ArchitectureTests.csproj \
  --no-build \
  --logger "trx" \
  --results-directory "$test_results_dir/architecture"

if ! command -v reportgenerator >/dev/null 2>&1; then
  dotnet tool install --global dotnet-reportgenerator-globaltool
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

reportgenerator \
  "-reports:$test_results_dir/**/coverage.cobertura.xml" \
  "-targetdir:$coverage_report_dir" \
  "-reporttypes:Html;Cobertura;MarkdownSummaryGithub" \
  "-assemblyfilters:+KnowledgeApp.*;-*.Tests" \
  "-classfilters:-Microsoft.AspNetCore.OpenApi.Generated*;-System.Text.RegularExpressions.Generated*"

summary_path="$coverage_report_dir/SummaryGithub.md"
if [[ -f "$summary_path" ]]; then
  cat "$summary_path"
fi

echo "Coverage report: $coverage_report_dir/index.html"
