# SwiftDrop-Title: Open Containing Folder
# SwiftDrop-Description: Open Explorer and select the file, or open the folder directly
# SwiftDrop-Icon: E838

param($filePath)

if ([string]::IsNullOrWhiteSpace($filePath)) {
    throw "No input path was provided."
}

if (Test-Path -LiteralPath $filePath -PathType Leaf) {
    Start-Process explorer.exe "/select,`"$filePath`""
    Write-Output "Opened the containing folder."
}
elseif (Test-Path -LiteralPath $filePath -PathType Container) {
    Start-Process explorer.exe "`"$filePath`""
    Write-Output "Opened the folder."
}
else {
    throw "Path not found: $filePath"
}
