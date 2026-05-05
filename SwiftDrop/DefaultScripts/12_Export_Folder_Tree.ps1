# SwiftDrop-Title: Export Folder Tree
# SwiftDrop-Description: Write a folder tree report into Tree_Report.txt
# SwiftDrop-Icon: E8B7

param($filePath)

if (-not (Test-Path -LiteralPath $filePath -PathType Container)) {
    throw "This script requires a folder."
}

$outputPath = Join-Path $filePath "Tree_Report.txt"

function Write-Tree([string]$rootPath) {
    Get-ChildItem -LiteralPath $rootPath -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($rootPath.Length).TrimStart('\')
        $depth = ($relative -split '\\').Count - 1
        $indent = ('  ' * $depth)
        if ($_.PSIsContainer) {
            "$indent[$($_.Name)]"
        }
        else {
            "$indent$($_.Name)"
        }
    }
}

$lines = @("[ROOT] $(Split-Path -Path $filePath -Leaf)")
$lines += Write-Tree $filePath
Set-Content -LiteralPath $outputPath -Value $lines
Write-Output "Exported folder tree to Tree_Report.txt"
