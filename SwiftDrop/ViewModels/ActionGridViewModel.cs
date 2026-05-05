using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwiftDrop.Models;
using SwiftDrop.Services;
using SwiftDrop.Services.Actions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwiftDrop.ViewModels
{
    public partial class ActionGridViewModel : ObservableObject
    {
        public ObservableCollection<ActionDefinition> Actions { get; } = new();

        [ObservableProperty]
        private ActionDefinition? _hoveredAction;

        private readonly string _scriptsFolderPath;
        private readonly ScriptScannerService _scriptScanner;
        private readonly string _userActionsFilePath;

        // ── Accent colors for dynamically-added tiles ────────────────────
        private static readonly string[] DynAccentColors =
        {
            "#F97316", "#06B6D4", "#8B5CF6", "#EF4444",
            "#14B8A6", "#F59E0B", "#EC4899", "#6366F1"
        };

        public ActionGridViewModel(string scriptsFolderPath)
        {
            _scriptsFolderPath = scriptsFolderPath;
            _scriptScanner = new ScriptScannerService(scriptsFolderPath);

            // Persistence path
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SwiftDrop");
            Directory.CreateDirectory(appDataDir);
            _userActionsFilePath = Path.Combine(appDataDir, "user_actions.json");
        }

        public async Task InitializeAsync()
        {
            RegisterBuiltInActions();
            await LoadUserActionsAsync();
            await ReloadScriptActionsAsync();
        }

        // ── Built-in actions ─────────────────────────────────────────────

        private void RegisterBuiltInActions()
        {
            Actions.Add(new ActionDefinition
            {
                Title = "To ZIP",
                IconGlyph = "\uE8B7",
                AccentColor = "#E8850C",
                Description = "Compress files/folders into a ZIP archive on your Desktop",
                Service = new ZipActionService(),
                AcceptedExtensions = Array.Empty<string>()
            });

            Actions.Add(new ActionDefinition
            {
                Title = "PNG → JPG",
                IconGlyph = "\uEB9F",
                AccentColor = "#10B981",
                Description = "Convert PNG images to JPG format",
                Service = new ImageConvertActionService(),
                AcceptedExtensions = new[] { ".png" }
            });

            Actions.Add(new ActionDefinition
            {
                Title = "JPG → PNG",
                IconGlyph = "\uEB9F",
                AccentColor = "#06B6D4",
                Description = "Convert JPG/JPEG images to PNG format",
                Service = new JpgToPngActionService(),
                AcceptedExtensions = new[] { ".jpg", ".jpeg" }
            });

            Actions.Add(new ActionDefinition
            {
                Title = "Convert to PDF",
                IconGlyph = "\uEA90",
                AccentColor = "#DC2626",
                Description = "Convert image files to PDF format",
                Service = new ImageToPdfActionService(),
                AcceptedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff" }
            });

            Actions.Add(new ActionDefinition
            {
                Title = "Move to Folder",
                IconGlyph = "\uE8A5",
                AccentColor = "#3B82F6",
                Description = "Move files or stacks to a folder you choose",
                Service = new QuickMoveActionService(),
                AcceptedExtensions = Array.Empty<string>()
            });

            Actions.Add(new ActionDefinition
            {
                Title = "Imgur Upload",
                IconGlyph = "\uE753",
                AccentColor = "#EC4899",
                Description = "Upload image to Imgur and copy link to clipboard",
                Service = new ImageUploadActionService(),
                AcceptedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" }
            });

            Actions.Add(new ActionDefinition
            {
                Title = "YT Download",
                IconGlyph = "\uE896",
                AccentColor = "#EF4444",
                Description = "Drop a YouTube URL to download as MP4",
                Service = new YouTubeDownloadActionService(),
                AcceptedExtensions = Array.Empty<string>()
            });
        }

        // ── Dynamic "Add to Grid" — drop .exe or folder ──────────────────

        /// <summary>
        /// Called when a .exe, .lnk, or folder is dropped directly on the action grid area
        /// (not on an existing tile). Creates a new dynamic action tile.
        /// </summary>
        public async Task<bool> TryAddDynamicActionAsync(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            IActionService? service = null;
            string title;
            string iconGlyph;
            string description;
            string actionType;
            string targetPath = path;

            if (Directory.Exists(path))
            {
                // It's a folder — create a Folder Shortcut action
                service = new FolderShortcutActionService(path);
                title = Path.GetFileName(path) ?? path;
                iconGlyph = "\uE8B7"; // Folder icon
                description = $"Move files to {title}";
                actionType = "folder";
            }
            else if (ext is ".exe" or ".lnk")
            {
                // Resolve .lnk to actual target
                if (ext == ".lnk")
                {
                    targetPath = ResolveShortcut(path) ?? path;
                }

                service = new AppLauncherActionService(targetPath);
                title = Path.GetFileNameWithoutExtension(path);
                iconGlyph = "\uE8FC"; // App icon
                description = $"Launch {title}";
                actionType = "app";
            }
            else
            {
                return false; // Not a droppable type for grid creation
            }

            // Avoid duplicates
            if (Actions.Any(a => a.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
                return false;

            int colorIdx = Actions.Count % DynAccentColors.Length;

            var actionDef = new ActionDefinition
            {
                Title = title,
                IconGlyph = iconGlyph,
                AccentColor = DynAccentColors[colorIdx],
                Description = description,
                Service = service,
                AcceptedExtensions = Array.Empty<string>(),
                IsUserAdded = true,
                ActionType = actionType,
                TargetPath = targetPath
            };

            Actions.Add(actionDef);

            // Persist
            await SaveUserActionsAsync();

            return true;
        }

        /// <summary>
        /// Removes a user-added action from the grid.
        /// </summary>
        [RelayCommand]
        public async Task RemoveUserAction(ActionDefinition action)
        {
            if (action.IsUserAdded)
            {
                Actions.Remove(action);
                await SaveUserActionsAsync();
            }
        }

        // ── .lnk resolution ─────────────────────────────────────────────

        private static string? ResolveShortcut(string lnkPath)
        {
            try
            {
                // Simple approach: read the .lnk file target using Shell32
                // Uses dynamic COM to avoid needing a reference
                dynamic shell = Activator.CreateInstance(
                    Type.GetTypeFromProgID("WScript.Shell")!)!;
                var shortcut = shell.CreateShortcut(lnkPath);
                string target = shortcut.TargetPath;
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                return string.IsNullOrEmpty(target) ? null : target;
            }
            catch
            {
                return null;
            }
        }

        // ── Script actions ───────────────────────────────────────────────

        [RelayCommand]
        public async Task ReloadScriptActionsAsync()
        {
            var scriptActions = Actions
                .Where(a => a.Service is PowerShellScriptActionService)
                .ToList();
            foreach (var action in scriptActions)
                Actions.Remove(action);

            var newScriptActions = await _scriptScanner.ScanAsync();
            foreach (var action in newScriptActions)
                Actions.Add(action);
        }

        public async Task<ActionResult> ExecuteActionAsync(ActionDefinition action, string filePath)
        {
            try
            {
                return await action.Service.ExecuteAsync(filePath);
            }
            catch (Exception ex)
            {
                return ActionResult.Fail($"Unexpected error: {ex.Message}");
            }
        }

        public IActionService? FindActionByType<T>() where T : IActionService
            => Actions.FirstOrDefault(a => a.Service is T)?.Service;

        // ══════════════════════════════════════════════════════════════════
        //  PERSISTENCE — Feature 4: Save/Load user-added actions
        // ══════════════════════════════════════════════════════════════════

        private record UserActionRecord(string Title, string IconGlyph, string AccentColor,
            string Description, string ActionType, string TargetPath);

        private async Task SaveUserActionsAsync()
        {
            try
            {
                var userActions = Actions
                    .Where(a => a.IsUserAdded)
                    .Select(a => new UserActionRecord(
                        a.Title, a.IconGlyph, a.AccentColor,
                        a.Description, a.ActionType, a.TargetPath))
                    .ToList();

                var json = JsonSerializer.Serialize(userActions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_userActionsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ActionGrid] Save failed: {ex.Message}");
            }
        }

        private async Task LoadUserActionsAsync()
        {
            try
            {
                if (!File.Exists(_userActionsFilePath)) return;

                var json = await File.ReadAllTextAsync(_userActionsFilePath);
                var records = JsonSerializer.Deserialize<UserActionRecord[]>(json);
                if (records == null) return;

                foreach (var rec in records)
                {
                    IActionService? service = rec.ActionType switch
                    {
                        "app" => new AppLauncherActionService(rec.TargetPath),
                        "folder" => new FolderShortcutActionService(rec.TargetPath),
                        _ => null
                    };

                    if (service == null) continue; // skip unknown types

                    if (Actions.Any(a => a.Title.Equals(rec.Title, StringComparison.OrdinalIgnoreCase)))
                        continue; // skip duplicates

                    Actions.Add(new ActionDefinition
                    {
                        Title = rec.Title,
                        IconGlyph = rec.IconGlyph,
                        AccentColor = rec.AccentColor,
                        Description = rec.Description,
                        Service = service,
                        AcceptedExtensions = Array.Empty<string>(),
                        IsUserAdded = true,
                        ActionType = rec.ActionType,
                        TargetPath = rec.TargetPath
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ActionGrid] Load failed: {ex.Message}");
            }
        }
    }
}
