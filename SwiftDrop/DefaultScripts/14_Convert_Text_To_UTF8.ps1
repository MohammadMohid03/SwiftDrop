# SwiftDrop-Title: Convert Text To UTF8
# SwiftDrop-Description: Re-save a text file as UTF-8 with a new filename
# SwiftDrop-Icon: E8A5

param($filePath)

if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
    throw "This script requires a file."
}

$extension = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant()
$allowed = @(".txt", ".md", ".json", ".xml", ".yml", ".yaml", ".ini", ".cfg", ".log", ".ps1", ".cs", ".js", ".ts", ".html", ".css")
if ($allowed -notcontains $extension) {
    throw "Unsupported file type for UTF-8 conversion: $extension"
}

$parent = Split-Path -Path $filePath -Parent
$stem = [System.IO.Path]::GetFileNameWithoutExtension($filePath)
$outputPath = Join-Path $parent "$stem.utf8$extension"
$content = Get-Content -LiteralPath $filePath -Raw
[System.IO.File]::WriteAllText($outputPath, $content, [System.Text.UTF8Encoding]::new($false))
Write-Output "Created UTF-8 copy: $(Split-Path -Path $outputPath -Leaf)"
