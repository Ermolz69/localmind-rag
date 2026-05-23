$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "package/test-portable-package.ps1") @args
