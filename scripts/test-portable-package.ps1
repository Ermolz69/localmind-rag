param(
    [string]$PackagePath = "artifacts/localmind-portable-win-x64",
    [string]$ZipPath = "artifacts/localmind-portable-win-x64.zip"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$package = Join-Path $root $PackagePath
$zip = Join-Path $root $ZipPath

function Assert-PathExists {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path $Path)) {
        throw "Portable smoke test failed. Missing ${Description}: $Path"
    }
}

function Assert-FileNotEmpty {
    param(
        [string]$Path,
        [string]$Description
    )

    Assert-PathExists -Path $Path -Description $Description
    $item = Get-Item -LiteralPath $Path
    if ($item.Length -le 0) {
        throw "Portable smoke test failed. ${Description} is empty: $Path"
    }
}

Assert-PathExists -Path $package -Description "portable package folder"
Assert-FileNotEmpty -Path (Join-Path $package "localmind.exe") -Description "desktop executable"
Assert-FileNotEmpty -Path (Join-Path $package "bin/KnowledgeApp.LocalApi.exe") -Description "LocalApi sidecar"
Assert-FileNotEmpty -Path (Join-Path $package "config/appsettings.json") -Description "portable config"
Assert-FileNotEmpty -Path (Join-Path $package "appsettings.json") -Description "root appsettings"
Assert-FileNotEmpty -Path (Join-Path $package "README.txt") -Description "portable README"

$requiredDirectories = @(
    "runtime/app/data",
    "runtime/app/files",
    "runtime/app/indexes",
    "runtime/app/logs",
    "runtime/ai/bin",
    "runtime/ai/models"
)

foreach ($directory in $requiredDirectories) {
    Assert-PathExists -Path (Join-Path $package $directory) -Description $directory
}

Assert-FileNotEmpty -Path $zip -Description "portable zip"

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entries = @{}
    foreach ($entry in $archive.Entries) {
        $normalized = $entry.FullName.Replace("\", "/").TrimStart("/")
        $entries[$normalized] = $entry
    }

    $requiredZipEntries = @(
        "localmind.exe",
        "bin/KnowledgeApp.LocalApi.exe",
        "config/appsettings.json",
        "appsettings.json",
        "README.txt"
    )

    foreach ($entryName in $requiredZipEntries) {
        if (-not $entries.ContainsKey($entryName)) {
            throw "Portable smoke test failed. Zip is missing entry: $entryName"
        }

        if ($entries[$entryName].Length -le 0) {
            throw "Portable smoke test failed. Zip entry is empty: $entryName"
        }
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Portable smoke test passed for $package"
Write-Host "Portable zip smoke test passed for $zip"
