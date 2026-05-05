# SwiftDrop-Title: Create File List
# SwiftDrop-Description: Write a flat file list report for a folder
# SwiftDrop-Icon: E8B7

param($filePath)

if (-not (Test-Path -LiteralPath $filePath -PathType Container)) {
    throw "This script requires a folder."
}

$outputPath = Join-Path $filePath "File_List.txt"
$files = Get-ChildItem -LiteralPath $filePath -File -Recurse | ForEach-Object {
    $_.FullName.Substring($filePath.Length).TrimStart('\')
}

Set-Content -LiteralPath $outputPath -Value $files
Write-Output "Exported file list to File_List.txt"
