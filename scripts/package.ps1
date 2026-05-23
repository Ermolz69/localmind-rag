$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "package/package.ps1") @args
