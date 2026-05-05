using SwiftDrop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    /// <summary>
    /// Compresses files/folders into a single ZIP archive on the Desktop.
    /// In batch mode, ALL stashed files go into ONE zip.
    /// </summary>
    public class ZipActionService : IActionService
    {
        public string Name => "To ZIP";

        /// <summary>Single file → single zip.</summary>
        public Task<ActionResult> ExecuteAsync(string input)
        {
            return ExecuteBatchAsync(new[] { input });
        }

        /// <summary>
        /// Batch mode: ALL files go into ONE zip archive.
        /// This is the key fix — stash files are zipped together, not individually.
        /// </summary>
        public async Task<ActionResult> ExecuteBatchAsync(IReadOnlyList<string> inputs)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (inputs.Count == 0)
                        return ActionResult.Fail("No files to zip.");

                    var outputDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    // Name the zip based on the first file, or "SwiftDrop_Archive"
                    string baseName = inputs.Count == 1
                        ? Path.GetFileNameWithoutExtension(inputs[0])
                        : $"SwiftDrop_Archive_{DateTime.Now:yyyyMMdd_HHmmss}";

                    var zipPath = Path.Combine(outputDir, $"{baseName}.zip");

                    // Avoid overwriting
                    int counter = 1;
                    while (File.Exists(zipPath))
                    {
                        zipPath = Path.Combine(outputDir, $"{baseName} ({counter++}).zip");
                    }

                    using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        foreach (var input in inputs)
                        {
                            if (File.Exists(input))
                            {
                                // Add file to zip
                                zip.CreateEntryFromFile(input, Path.GetFileName(input),
                                    CompressionLevel.Optimal);
                            }
                            else if (Directory.Exists(input))
                            {
                                // Add entire directory recursively
                                var dirName = Path.GetFileName(input) ?? "folder";
                                AddDirectoryToZip(zip, input, dirName);
                            }
                        }
                    }

                    var fileInfo = new FileInfo(zipPath);
                    string sizeStr = FormatSize(fileInfo.Length);

                    return ActionResult.Ok(
                        $"Zipped {inputs.Count} item(s) → {Path.GetFileName(zipPath)} ({sizeStr})",
                        outputPath: zipPath);
                }
                catch (Exception ex)
                {
                    return ActionResult.Fail($"ZIP failed: {ex.Message}");
                }
            });
        }

        private static void AddDirectoryToZip(ZipArchive zip, string sourceDir, string entryPrefix)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var entryName = Path.Combine(entryPrefix, relativePath).Replace('\\', '/');
                zip.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
            }
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024):F1} MB"
        };
    }
}