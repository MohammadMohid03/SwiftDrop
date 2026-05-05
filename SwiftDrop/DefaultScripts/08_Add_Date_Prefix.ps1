# SwiftDrop-Title: Add Date Prefix
# SwiftDrop-Description: Prefix the file or folder name with today's date
# SwiftDrop-Icon: E787

param($filePath)

if (-not (Test-Path -LiteralPath $filePath)) {
    throw "Path not found: $filePath"
}

$parent = Split-Path -Path $filePath -Parent
$leaf = Split-Path -Path $filePath -Leaf
$prefix = Get-Date -Format "yyyy-MM-dd"
$newLeaf = "$prefix $leaf"
$destination = Join-Path $parent $newLeaf

if (Test-Path -LiteralPath $destination) {
    throw "A dated target already exists: $newLeaf"
}

Rename-Item -LiteralPath $filePath -NewName $newLeaf
Write-Output "Renamed to $newLeaf"
