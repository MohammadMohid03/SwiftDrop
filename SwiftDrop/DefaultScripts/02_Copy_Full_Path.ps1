# SwiftDrop-Title: Copy Full Path
# SwiftDrop-Description: Copy the full file or folder path to the clipboard
# SwiftDrop-Icon: E8C8

param($filePath)

if ([string]::IsNullOrWhiteSpace($filePath)) {
    throw "No input path was provided."
}

Set-Clipboard -Value $filePath
Write-Output "Copied path to clipboard."
