Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "setup-ai-runtime.ps1")
& (Join-Path $PSScriptRoot "setup-ai-models.ps1")
