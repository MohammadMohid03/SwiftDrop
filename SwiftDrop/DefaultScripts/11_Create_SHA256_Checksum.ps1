# SwiftDrop-Title: Create SHA256 Checksum
# SwiftDrop-Description: Create a .sha256 file next to the dropped file
# SwiftDrop-Icon: E9D9

param($filePath)

if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
    throw "This script requires a file."
}

$hash = Get-FileHash -LiteralPath $filePath -Algorithm SHA256
$outputPath = "$filePath.sha256"
$content = "$($hash.Hash) *$(Split-Path -Path $filePath -Leaf)"
Set-Content -LiteralPath $outputPath -Value $content
Write-Output "Wrote checksum file: $(Split-Path -Path $outputPath -Leaf)"
