using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SwiftDrop.Models;
using Forms = System.Windows.Forms;

namespace SwiftDrop.Services.Actions
{
    public sealed class QuickMoveActionService : IActionService
    {
        public string Name => "Move to Folder";

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SwiftDrop",
            "move_destination.txt");

        private string? _destinationFolder;

        public QuickMoveActionService()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var savedPath = File.ReadAllText(SettingsPath).Trim();
                    if (!string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath))
                        _destinationFolder = savedPath;
                }
            }
            catch
            {
            }
        }

        public Task<ActionResult> ExecuteAsync(string input)
            => ExecuteBatchAsync(new[] { input });

        public async Task<ActionResult> ExecuteBatchAsync(IReadOnlyList<string> inputs)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var validInputs = inputs
                        .Where(path => !string.IsNullOrWhiteSpace(path))
                        .Where(path => File.Exists(path) || Directory.Exists(path))
                        .ToList();

                    if (validInputs.Count == 0)
                        return ActionResult.Fail("No valid files or folders to move.");

                    string? destinationFolder = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        destinationFolder = PromptForFolder(_destinationFolder);
                    });

                    if (string.IsNullOrWhiteSpace(destinationFolder) || !Directory.Exists(destinationFolder))
                        return ActionResult.Fail("Move cancelled - no folder selected.");

                    _destinationFolder = destinationFolder;
                    SaveDestination(destinationFolder);

                    string? lastDestination = null;
                    foreach (var input in validInputs)
                    {
                        var itemName = Path.GetFileName(input);
                        var destinationPath = ResolveCollision(Path.Combine(destinationFolder, itemName));

                        if (File.Exists(input))
                            File.Move(input, destinationPath);
                        else
                            MoveDirectory(input, destinationPath);

                        lastDestination = destinationPath;
                    }

                    var folderName = Path.GetFileName(destinationFolder.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar));

                    if (string.IsNullOrWhiteSpace(folderName))
                        folderName = destinationFolder;

                    return ActionResult.Ok(
                        $"Moved {validInputs.Count} item(s) to {folderName}",
                        outputPath: lastDestination);
                }
                catch (Exception ex)
                {
                    return ActionResult.Fail($"Move failed: {ex.Message}");
                }
            });
        }

        public bool ChangeDestination()
        {
            var folder = PromptForFolder(_destinationFolder);
            if (string.IsNullOrWhiteSpace(folder))
                return false;

            _destinationFolder = folder;
            SaveDestination(folder);
            return true;
        }

        private static string? PromptForFolder(string? initialPath)
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Choose destination folder",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                dialog.SelectedPath = initialPath;

            return dialog.ShowDialog() == Forms.DialogResult.OK
                ? dialog.SelectedPath
                : null;
        }

        private static void SaveDestination(string folder)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, folder);
            }
            catch
            {
            }
        }

        private static string ResolveCollision(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return path;

            var directory = Path.GetDirectoryName(path)!;
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var index = 1;

            string candidate;
            do
            {
                candidate = Path.Combine(directory, $"{name} ({index++}){extension}");
            }
            while (File.Exists(candidate) || Directory.Exists(candidate));

            return candidate;
        }

        private static void MoveDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(source, file);
                var destinationFile = Path.Combine(destination, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Move(file, destinationFile);
            }

            Directory.Delete(source, recursive: true);
        }
    }
}
