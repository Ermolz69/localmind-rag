param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputPath = "artifacts/localmind-portable-win-x64",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$output = Join-Path $root $OutputPath
$bin = Join-Path $output "bin"
$config = Join-Path $output "config"
$runtimeApp = Join-Path $output "runtime/app"
$runtimeAi = Join-Path $output "runtime/ai"
$desktop = Join-Path $root "apps/desktop"
$tauriExe = Join-Path $desktop "src-tauri/target/release/localmind.exe"

if (Test-Path $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}

if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
    throw "Rust/Cargo is required to build the Tauri desktop executable. Install Rust or run this workflow on GitHub Actions."
}

New-Item -ItemType Directory -Force -Path $bin, $config | Out-Null
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
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $bin

pnpm.cmd --filter desktop run tauri build --no-bundle

if (-not (Test-Path $tauriExe)) {
    throw "Tauri executable was not produced at $tauriExe"
}

Copy-Item -Path $tauriExe -Destination (Join-Path $output "localmind.exe") -Force
Copy-Item -Path (Join-Path $root "backend/src/KnowledgeApp.LocalApi/appsettings.json") -Destination (Join-Path $config "appsettings.json") -Force
Copy-Item -Path (Join-Path $root "backend/src/KnowledgeApp.LocalApi/appsettings.json") -Destination (Join-Path $output "appsettings.json") -Force

@"
localmind portable

Contents:
- localmind.exe: Tauri desktop shell
- bin/KnowledgeApp.LocalApi.exe: local API sidecar
- runtime/app/: portable SQLite, files, indexes, logs
- runtime/ai/: local AI runtime binaries and models
- config/appsettings.json: portable runtime settings

Run localmind.exe. The desktop shell starts the LocalApi sidecar automatically.
"@ | Set-Content -Path (Join-Path $output "README.txt") -Encoding UTF8

@"
KNOWLEDGE_APP_PORTABLE=true
LOCAL_API_URL=http://127.0.0.1:49321
LOCAL_STORAGE_PATH=runtime/app/files
LOCAL_DATABASE_PATH=runtime/app/data/knowledge-app.db
LOCAL_INDEX_PATH=runtime/app/indexes
"@ | Set-Content -Path (Join-Path $output ".env.example") -Encoding UTF8

$requiredPaths = @(
    (Join-Path $output "localmind.exe"),
    (Join-Path $bin "KnowledgeApp.LocalApi.exe"),
    (Join-Path $config "appsettings.json"),
    (Join-Path $runtimeApp "data"),
    (Join-Path $runtimeApp "files"),
    (Join-Path $runtimeApp "indexes"),
    (Join-Path $runtimeApp "logs")
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path $path)) {
        throw "Portable package is missing required path: $path"
    }
}

Write-Host "Portable package created at $output"

if (-not $SkipZip) {
    $zipPath = "$output.zip"
    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $output "*") -DestinationPath $zipPath -Force
    Write-Host "Portable zip created at $zipPath"
}
