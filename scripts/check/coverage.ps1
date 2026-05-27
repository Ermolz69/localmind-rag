$ErrorActionPreference = "Stop"

$testResultsDir = "artifacts/test-results/local"
$coverageReportDir = "artifacts/coverage-report/local"

if (Test-Path $testResultsDir) {
    Remove-Item $testResultsDir -Recurse -Force
}

if (Test-Path $coverageReportDir) {
    Remove-Item $coverageReportDir -Recurse -Force
}

dotnet restore backend/KnowledgeApp.slnx
dotnet build backend/KnowledgeApp.slnx --no-restore

dotnet test backend/tests/KnowledgeApp.UnitTests/KnowledgeApp.UnitTests.csproj `
    --no-build `
    --logger "trx" `
    --results-directory "$testResultsDir/unit" `
    --collect:"XPlat Code Coverage" `
    -- `
    DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet test backend/tests/KnowledgeApp.IntegrationTests/KnowledgeApp.IntegrationTests.csproj `
    --no-build `
    --logger "trx" `
    --results-directory "$testResultsDir/integration" `
    --collect:"XPlat Code Coverage" `
    -- `
    DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet test backend/tests/KnowledgeApp.RagEvaluationTests/KnowledgeApp.RagEvaluationTests.csproj `
    --no-build `
    --logger "trx" `
    --results-directory "$testResultsDir/rag-evaluation" `
    --collect:"XPlat Code Coverage" `
    -- `
    DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet test backend/tests/KnowledgeApp.ArchitectureTests/KnowledgeApp.ArchitectureTests.csproj `
    --no-build `
    --logger "trx" `
    --results-directory "$testResultsDir/architecture"

$reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue

if ($null -eq $reportGenerator) {
    dotnet tool install --global dotnet-reportgenerator-globaltool
}

$reportGeneratorCommand = Get-Command reportgenerator -ErrorAction SilentlyContinue

$reportGeneratorPath = if ($null -eq $reportGeneratorCommand) {
    $null
}
else {
    $reportGeneratorCommand.Source
}

if ([string]::IsNullOrWhiteSpace($reportGeneratorPath)) {
    $reportGeneratorPath = Join-Path $env:USERPROFILE ".dotnet/tools/reportgenerator.exe"
}

& $reportGeneratorPath `
    "-reports:$testResultsDir/**/coverage.cobertura.xml" `
    "-targetdir:$coverageReportDir" `
    "-reporttypes:Html;Cobertura;MarkdownSummaryGithub" `
    "-assemblyfilters:+KnowledgeApp.*;-*.Tests" `
    "-classfilters:-Microsoft.AspNetCore.OpenApi.Generated*;-System.Text.RegularExpressions.Generated*"

$summaryPath = Join-Path $coverageReportDir "SummaryGithub.md"

if (Test-Path $summaryPath) {
    Get-Content $summaryPath
}

Write-Host "Coverage report: $coverageReportDir/index.html"
