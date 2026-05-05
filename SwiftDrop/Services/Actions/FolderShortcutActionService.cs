using SwiftDrop.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    /// <summary>
    /// Action that opens a folder in Explorer.
    /// If files are dropped onto this action, they are moved into the target folder.
    /// Created dynamically when user drops a folder onto the Action Grid.
    /// </summary>
    public class FolderShortcutActionService : IActionService
    {
        public string Name { get; }

        /// <summary>Full path to the target folder.</summary>
        public string FolderPath { get; }

        public FolderShortcutActionService(string folderPath)
        {
            FolderPath = folderPath;
            Name = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(Name))
                Name = folderPath; // root drive like C:\
        }

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                    return ActionResult.Fail($"Folder not found: {FolderPath}");

                // If input is a valid file, move it into the folder
                if (!string.IsNullOrWhiteSpace(input) && File.Exists(input))
                {
                    var fileName = Path.GetFileName(input);
                    var destPath = Path.Combine(FolderPath, fileName);

                    // Handle name collision
                    int counter = 1;
                    while (File.Exists(destPath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(FolderPath, $"{nameWithoutExt} ({counter}){ext}");
                        counter++;
                    }

                    await Task.Run(() => File.Move(input, destPath));
                    return ActionResult.Ok($"Moved to {Path.GetFileName(FolderPath)}/",
                        outputPath: destPath);
                }
                // If input is a folder, move it
                else if (!string.IsNullOrWhiteSpace(input) && Directory.Exists(input))
                {
                    var dirName = Path.GetFileName(input);
                    var destPath = Path.Combine(FolderPath, dirName!);

                    if (Directory.Exists(destPath))
                        return ActionResult.Fail($"Folder already exists: {destPath}");

                    await Task.Run(() => Directory.Move(input, destPath));
                    return ActionResult.Ok($"Moved folder to {Path.GetFileName(FolderPath)}/",
                        outputPath: destPath);
                }
                else
                {
                    // No file input — just open the folder
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{FolderPath}\"")
                    {
                        UseShellExecute = true
                    });
                    return ActionResult.Ok($"Opened {Name}");
                }
            }
            catch (Exception ex)
            {
                return ActionResult.Fail($"Error: {ex.Message}");
            }
        }
    }
}
