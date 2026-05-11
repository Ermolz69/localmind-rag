param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputPath = "artifacts/localmind-portable-win-x64"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$output = Join-Path $root $OutputPath
$bin = Join-Path $output "bin"
$ui = Join-Path $output "ui"
$config = Join-Path $output "config"
$runtimeApp = Join-Path $output "runtime/app"
$runtimeAi = Join-Path $output "runtime/ai"

if (Test-Path $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $bin, $ui, $config | Out-Null
New-Item -ItemType Directory -Force -Path `
    (Join-Path $runtimeApp "data"), `
    (Join-Path $runtimeApp "files"), `
    (Join-Path $runtimeApp "indexes"), `
    (Join-Path $runtimeApp "logs"), `
    (Join-Path $runtimeAi "bin"), `
    (Join-Path $runtimeAi "models") | Out-Null

dotnet publish `
    (Join-Path $root "backend/src/KnowledgeApp.LocalApi/KnowledgeApp.LocalApi.csproj") `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -o $bin

pnpm.cmd --filter desktop build

Copy-Item -Path (Join-Path $root "apps/desktop/dist/*") -Destination $ui -Recurse -Force
Copy-Item -Path (Join-Path $root "backend/src/KnowledgeApp.LocalApi/appsettings.json") -Destination (Join-Path $config "appsettings.json") -Force

@"
localmind portable preview

Contents:
- bin/KnowledgeApp.LocalApi.exe: local API sidecar
- ui/: built React desktop UI assets
- runtime/app/: portable SQLite, files, indexes, logs
- runtime/ai/: local AI runtime binaries and models
- config/appsettings.json: portable runtime settings

Current scaffold note:
This artifact is a portable preview of the app payload. The full Tauri shell executable will be wired into this package once native desktop packaging is enabled.
"@ | Set-Content -Path (Join-Path $output "README.txt") -Encoding UTF8

@"
KNOWLEDGE_APP_PORTABLE=true
LOCAL_API_URL=http://127.0.0.1:49321
LOCAL_STORAGE_PATH=runtime/app/files
LOCAL_DATABASE_PATH=runtime/app/data/knowledge-app.db
LOCAL_INDEX_PATH=runtime/app/indexes
"@ | Set-Content -Path (Join-Path $output ".env.example") -Encoding UTF8

Write-Host "Portable preview created at $output"
