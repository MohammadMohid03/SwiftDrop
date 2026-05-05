# SwiftDrop-Title: Move To Downloads
# SwiftDrop-Description: Move the file or folder to your Downloads folder
# SwiftDrop-Icon: E896

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
        $candidate = Join-Path $parent "$stem ($index)$ext"
        $index++
    } while (Test-Path -LiteralPath $candidate)

    return $candidate
}

$downloads = Join-Path ([Environment]::GetFolderPath("UserProfile")) "Downloads"
$destination = Get-UniquePath (Join-Path $downloads (Split-Path -Path $filePath -Leaf))
Move-Item -LiteralPath $filePath -Destination $destination
Write-Output "Moved to Downloads: $(Split-Path -Path $destination -Leaf)"
