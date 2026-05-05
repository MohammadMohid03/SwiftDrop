# SwiftDrop-Title: Rename To Uppercase
# SwiftDrop-Description: Rename a file or folder name to uppercase
# SwiftDrop-Icon: E8AC

param($filePath)

if (-not (Test-Path -LiteralPath $filePath)) {
    throw "Path not found: $filePath"
}

$parent = Split-Path -Path $filePath -Parent
$leaf = Split-Path -Path $filePath -Leaf
$newLeaf = $leaf.ToUpperInvariant()

if ($leaf -eq $newLeaf) {
    Write-Output "Name is already uppercase."
    return
}

$destination = Join-Path $parent $newLeaf
if (Test-Path -LiteralPath $destination) {
    throw "An uppercase target already exists: $newLeaf"
}

Rename-Item -LiteralPath $filePath -NewName $newLeaf
Write-Output "Renamed to $newLeaf"
