# SwiftDrop-Title: Copy File Name
# SwiftDrop-Description: Copy the file or folder name to the clipboard
# SwiftDrop-Icon: E8C8

param($filePath)

if ([string]::IsNullOrWhiteSpace($filePath)) {
    throw "No input path was provided."
}

$name = Split-Path -Path $filePath -Leaf
Set-Clipboard -Value $name
Write-Output "Copied '$name' to clipboard."
