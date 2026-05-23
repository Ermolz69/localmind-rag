Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$runtimeDir = Join-Path $repoRoot "runtime\ai"
$binDir = Join-Path $runtimeDir "bin"
$tempDir = Join-Path $runtimeDir ".tmp"
$archivePath = Join-Path $tempDir "llama.cpp-b9222-win-vulkan-x64.zip"
$extractDir = Join-Path $tempDir "llama.cpp-b9222"
$sourceUrl = "https://github.com/ggml-org/llama.cpp/releases/download/b9222/llama-b9222-bin-win-vulkan-x64.zip"
$expectedVersion = "version: 9222"
$serverPath = Join-Path $binDir "llama-server.exe"

function Invoke-Download {
    param(
        [string] $Url,
        [string] $OutFile
    )

    $curl = Get-Command "curl.exe" -ErrorAction SilentlyContinue
    if ($null -ne $curl) {
        & $curl.Source --location --fail --retry 3 --retry-delay 2 --output $OutFile $Url
        if ($LASTEXITCODE -ne 0) {
            throw "curl.exe failed with exit code $LASTEXITCODE."
        }

        return
    }

    Invoke-WebRequest -Uri $Url -OutFile $OutFile
}

function Test-LlamaServer {
    if (-not (Test-Path -LiteralPath $serverPath)) {
        return $false
    }

    $version = & $serverPath --version 2>&1
    return ($version -join "`n").Contains($expectedVersion)
}

New-Item -ItemType Directory -Force -Path $binDir | Out-Null
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

if (Test-LlamaServer) {
    Write-Host "llama.cpp runtime is already installed: $serverPath"
    exit 0
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

if (Test-Path -LiteralPath $extractDir) {
    Remove-Item -LiteralPath $extractDir -Recurse -Force
}

Write-Host "Downloading llama.cpp runtime..."
Invoke-Download -Url $sourceUrl -OutFile $archivePath

Write-Host "Extracting llama.cpp runtime..."
Expand-Archive -LiteralPath $archivePath -DestinationPath $extractDir -Force

$extractedServer = Get-ChildItem -LiteralPath $extractDir -Recurse -Filter "llama-server.exe" | Select-Object -First 1
if ($null -eq $extractedServer) {
    throw "Downloaded llama.cpp archive does not contain llama-server.exe."
}

$sourceDirectory = $extractedServer.Directory.FullName
Get-ChildItem -LiteralPath $sourceDirectory -File | Copy-Item -Destination $binDir -Force

if (-not (Test-LlamaServer)) {
    throw "Installed llama-server.exe did not report the expected version $expectedVersion."
}

Remove-Item -LiteralPath $archivePath -Force
Remove-Item -LiteralPath $extractDir -Recurse -Force

Write-Host "llama.cpp runtime installed: $serverPath"
