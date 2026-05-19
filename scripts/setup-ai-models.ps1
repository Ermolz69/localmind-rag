Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$modelsDir = Join-Path $repoRoot "runtime\ai\models"
$fileName = "bge-m3-Q4_K_M.gguf"
$modelPath = Join-Path $modelsDir $fileName
$downloadPath = "$modelPath.download"
$sourceUrl = "https://huggingface.co/gpustack/bge-m3-GGUF/resolve/main/bge-m3-Q4_K_M.gguf"
$expectedSha256 = "6d39681b26c61279ac1f82db35a04a05009e94c415b51c858ff571489a82fc06"

function Test-ModelChecksum {
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    $actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
    return $actualSha256 -eq $expectedSha256
}

New-Item -ItemType Directory -Force -Path $modelsDir | Out-Null

if (Test-ModelChecksum -Path $modelPath) {
    Write-Host "Embedding model is already installed: $modelPath"
    exit 0
}

if (Test-Path -LiteralPath $downloadPath) {
    Remove-Item -LiteralPath $downloadPath -Force
}

Write-Host "Downloading $fileName..."
try {
    $curl = Get-Command "curl.exe" -ErrorAction SilentlyContinue
    if ($null -ne $curl) {
        & $curl.Source --location --fail --retry 3 --retry-delay 2 --output $downloadPath $sourceUrl
        if ($LASTEXITCODE -ne 0) {
            throw "curl.exe failed with exit code $LASTEXITCODE."
        }
    }
    else {
        Invoke-WebRequest -Uri $sourceUrl -OutFile $downloadPath
    }
}
catch {
    if (Test-Path -LiteralPath $downloadPath) {
        Remove-Item -LiteralPath $downloadPath -Force
    }

    throw
}

if (-not (Test-ModelChecksum -Path $downloadPath)) {
    Remove-Item -LiteralPath $downloadPath -Force
    throw "Downloaded model checksum does not match expected SHA-256."
}

Move-Item -LiteralPath $downloadPath -Destination $modelPath -Force
Write-Host "Embedding model installed: $modelPath"
