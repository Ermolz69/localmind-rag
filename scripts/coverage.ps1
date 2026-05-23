$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "check/coverage.ps1") @args
