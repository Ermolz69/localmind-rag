$ErrorActionPreference = "Stop"

$paths = @(
    "apps/desktop/src/app",
    "apps/desktop/src/pages",
    "apps/desktop/src/widgets",
    "apps/desktop/src/features",
    "apps/desktop/src/entities",
    "apps/desktop/src/shared"
)

$patterns = @(
    "bg-\[#",
    "text-\[#",
    "border-\[#",
    "style=\{\{[^}]*color",
    "style=\{\{[^}]*background",
    "rgb\(",
    "rgba\(",
    "#[0-9a-fA-F]{3,8}"
)

$files = Get-ChildItem -Path $paths -Recurse -Include *.ts,*.tsx -File
$violations = foreach ($file in $files) {
    foreach ($pattern in $patterns) {
        Select-String -Path $file.FullName -Pattern $pattern -AllMatches | ForEach-Object {
            [pscustomobject]@{
                Path = Resolve-Path -Relative $_.Path
                Line = $_.LineNumber
                Text = $_.Line.Trim()
            }
        }
    }
}

if ($violations) {
    $violations | Format-Table -AutoSize
    throw "Hardcoded frontend colors are not allowed. Use semantic theme tokens."
}

Write-Host "No hardcoded frontend colors found."
