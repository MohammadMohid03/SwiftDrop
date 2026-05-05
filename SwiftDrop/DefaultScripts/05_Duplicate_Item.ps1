# SwiftDrop-Title: Duplicate Item
# SwiftDrop-Description: Create a copy next to the original file or folder
# SwiftDrop-Icon: E8C8

param($filePath)

if (-not (Test-Path -LiteralPath $filePath)) {
    throw "Path not found: $filePath"
}

function Get-UniquePath([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        return $path
    }

    $parent = Split-Path -Path $path -Parent
    $leaf = Split-Path -Path $path -Leaf
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($leaf)
    $ext = [System.IO.Path]::GetExtension($leaf)
    $index = 1

    do {
        $candidate = Join-Path $parent "$stem - Copy $index$ext"
        $index++
    } while (Test-Path -LiteralPath $candidate)

    return $candidate
}

$destination = Get-UniquePath $filePath
Copy-Item -LiteralPath $filePath -Destination $destination -Recurse
Write-Output "Created copy: $(Split-Path -Path $destination -Leaf)"
