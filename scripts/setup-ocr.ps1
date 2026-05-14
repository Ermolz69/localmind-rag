param(
    [string[]] $Languages = @("eng", "osd", "ukr"),
    [switch] $Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ocrRoot = Join-Path $repoRoot "runtime\ocr"
$ocrBin = Join-Path $ocrRoot "bin"
$tessData = Join-Path $ocrRoot "tessdata"

$tessdataFastBaseUrl = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main"

New-Item -ItemType Directory -Force $ocrBin | Out-Null
New-Item -ItemType Directory -Force $tessData | Out-Null

$tesseractTarget = Join-Path $ocrBin "tesseract.exe"

function Find-InstalledTesseractRoot {
    $installedCandidates = @(
        "C:\Program Files\Tesseract-OCR",
        "C:\Program Files (x86)\Tesseract-OCR"
    )

    $command = Get-Command "tesseract.exe" -ErrorAction SilentlyContinue

    if ($command) {
        return Split-Path $command.Source -Parent
    }

    return $installedCandidates | Where-Object {
        Test-Path (Join-Path $_ "tesseract.exe")
    } | Select-Object -First 1
}

function Ensure-TesseractRuntime {
    if ((Test-Path $tesseractTarget) -and -not $Force) {
        Write-Host "OCR runtime already exists: $tesseractTarget"
        return
    }

    $installedRoot = Find-InstalledTesseractRoot

    if (-not $installedRoot) {
        Write-Host "Tesseract OCR is not installed. Trying winget install..."

        $winget = Get-Command "winget.exe" -ErrorAction SilentlyContinue
        if (-not $winget) {
            throw "winget.exe was not found. Install Tesseract OCR manually or add it to PATH."
        }

        winget install --id UB-Mannheim.TesseractOCR --accept-source-agreements --accept-package-agreements

        $installedRoot = Find-InstalledTesseractRoot
    }

    if (-not $installedRoot) {
        throw "Tesseract OCR installation was not found after winget install."
    }

    Write-Host "Using installed Tesseract from: $installedRoot"

    Copy-Item (Join-Path $installedRoot "tesseract.exe") $tesseractTarget -Force
    Copy-Item (Join-Path $installedRoot "*.dll") $ocrBin -Force

    Write-Host "Copied Tesseract executable and DLLs to: $ocrBin"
}

function Ensure-LanguageData {
    param(
        [string] $Language
    )

    $target = Join-Path $tessData "$Language.traineddata"

    if ((Test-Path $target) -and -not $Force) {
        Write-Host "Language data already exists: $Language"
        return
    }

    $installedRoot = Find-InstalledTesseractRoot

    if ($installedRoot) {
        $installedTessData = Join-Path $installedRoot "tessdata"
        $source = Join-Path $installedTessData "$Language.traineddata"

        if (Test-Path $source) {
            Copy-Item $source $target -Force
            Write-Host "Copied language data from installed Tesseract: $Language"
            return
        }
    }

    $downloadUrl = "$tessdataFastBaseUrl/$Language.traineddata"

    Write-Host "Downloading language data: $Language"
    Write-Host $downloadUrl

    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $target
    } catch {
        if (Test-Path $target) {
            Remove-Item $target -Force
        }

        throw "Failed to download language data '$Language' from tessdata_fast."
    }

    Write-Host "Downloaded language data: $Language"
}

Ensure-TesseractRuntime

foreach ($language in $Languages) {
    Ensure-LanguageData -Language $language
}

Write-Host ""
Write-Host "Verifying portable OCR runtime..."

& $tesseractTarget --version

Write-Host ""
& $tesseractTarget --list-langs --tessdata-dir $tessData

Write-Host ""
Write-Host "OCR runtime setup complete."