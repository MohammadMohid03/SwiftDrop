# SwiftDrop-Title: Rename To Lowercase
# SwiftDrop-Description: Rename a file or folder name to lowercase
# SwiftDrop-Icon: E8AC

param($filePath)

if (-not (Test-Path -LiteralPath $filePath)) {
    throw "Path not found: $filePath"
}

$parent = Split-Path -Path $filePath -Parent
$leaf = Split-Path -Path $filePath -Leaf
$newLeaf = $leaf.ToLowerInvariant()

if ($leaf -eq $newLeaf) {
    Write-Output "Name is already lowercase."
    return
}

$destination = Join-Path $parent $newLeaf
if (Test-Path -LiteralPath $destination) {
    throw "A lowercase target already exists: $newLeaf"
}

Rename-Item -LiteralPath $filePath -NewName $newLeaf
Write-Output "Renamed to $newLeaf"
