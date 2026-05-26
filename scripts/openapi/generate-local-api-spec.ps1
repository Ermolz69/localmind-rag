[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $RepositoryRoot,

    [Parameter(Mandatory = $true)]
    [string] $OutputFile,

    [string] $RuntimeRoot,

    [string] $RuntimeDatabaseFileName = "knowledge-app-openapi.db"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$localApiProject = Join-Path $repoRoot "backend/src/KnowledgeApp.LocalApi/KnowledgeApp.LocalApi.csproj"

$outputFilePath = [System.IO.Path]::GetFullPath($OutputFile)
$outputDirectory = Split-Path -Parent $outputFilePath

if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) {
    $runtimeRootPath = Join-Path $repoRoot "artifacts/openapi/runtime"
}
else {
    $runtimeRootPath = [System.IO.Path]::GetFullPath($RuntimeRoot)
}

if (Test-Path $outputDirectory) {
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force $outputDirectory | Out-Null
New-Item -ItemType Directory -Force $runtimeRootPath | Out-Null

$env:LocalRuntime__DataPath = Join-Path $runtimeRootPath "app/data"
$env:LocalRuntime__DatabasePath =
    Join-Path $runtimeRootPath "app/data/$RuntimeDatabaseFileName"
$env:LocalRuntime__FilesPath = Join-Path $runtimeRootPath "app/files"
$env:LocalRuntime__IndexPath = Join-Path $runtimeRootPath "app/indexes"
$env:LocalRuntime__LogsPath = Join-Path $runtimeRootPath "app/logs"

$env:Ai__EmbeddingProvider = "Stub"
$env:Ai__RuntimePath = Join-Path $runtimeRootPath "ai/bin/llama-server.exe"
$env:Ai__ModelsPath = Join-Path $runtimeRootPath "ai/models"

$env:IngestionWorker__Enabled = "false"

dotnet build $localApiProject `
    --configuration Release `
    --no-incremental `
    /p:OpenApiGenerateDocuments=true `
    /p:OpenApiDocumentsDirectory="$outputDirectory"

if ($LASTEXITCODE -ne 0) {
    throw "LocalApi build failed while generating the OpenAPI specification."
}

$generatedOpenApiFiles = @(
    Get-ChildItem -LiteralPath $outputDirectory -Filter "*.json"
)

if ($generatedOpenApiFiles.Count -eq 0) {
    throw "LocalApi build did not generate an OpenAPI JSON document."
}

if ($generatedOpenApiFiles.Count -gt 1) {
    throw "LocalApi build generated more than one OpenAPI JSON document."
}

$generatedOpenApi = $generatedOpenApiFiles[0]

if ($generatedOpenApi.FullName -ne $outputFilePath) {
    Move-Item -LiteralPath $generatedOpenApi.FullName -Destination $outputFilePath -Force
}

Write-Host "OpenAPI specification generated at $outputFilePath"
