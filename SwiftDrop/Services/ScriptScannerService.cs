// SwiftDrop/Services/ScriptScannerService.cs
//
// Scans the user-extensible Scripts folder and produces ActionDefinition
// objects for each .ps1 file found. This enables end-users to extend
// SwiftDrop with custom behaviors without touching C# code.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SwiftDrop.Models;
using SwiftDrop.Services.Actions;

namespace SwiftDrop.Services
{
    public sealed class ScriptScannerService
    {
        private readonly string _scriptsFolderPath;

        // Accent colors cycled through for script tiles (avoids all being same color)
        private static readonly string[] ScriptAccentColors =
        {
            "#5E5CE6", // Purple
            "#BF5AF2", // Violet
            "#FF6B35", // Deep orange
            "#30D158", // Mint green
            "#64D2FF", // Sky blue
        };

        public ScriptScannerService(string scriptsFolderPath)
        {
            _scriptsFolderPath = scriptsFolderPath;
        }

        /// <summary>
        /// Scans the scripts folder and returns one ActionDefinition per .ps1 file.
        /// 
        /// Script metadata can optionally be declared in special comment headers:
        ///   # SwiftDrop-Title: My Action
        ///   # SwiftDrop-Description: Does something cool
        ///   # SwiftDrop-Icon: E8B7
        /// </summary>
        public async Task<IReadOnlyList<ActionDefinition>> ScanAsync()
        {
            var results = new List<ActionDefinition>();

            await EnsureStarterScriptsAsync();

            var scriptFiles = Directory.GetFiles(_scriptsFolderPath, "*.ps1",
                                                  SearchOption.TopDirectoryOnly);
            int colorIndex = 0;

            foreach (var scriptPath in scriptFiles)
            {
                var metadata = await ReadScriptMetadataAsync(scriptPath);

                results.Add(new ActionDefinition
                {
                    Title = metadata.Title,
                    Description = metadata.Description,
                    IconGlyph = char.ConvertFromUtf32(Convert.ToInt32(metadata.IconHex, 16)), // parse hex to char
                    AccentColor = ScriptAccentColors[colorIndex % ScriptAccentColors.Length],
                    Service = new PowerShellScriptActionService(scriptPath)
                });

                colorIndex++;
            }

            return results;
        }

        // ── Metadata parsing ─────────────────────────────────────────────────

        private record ScriptMetadata(string Title, string Description, string IconHex);

        private static async Task<ScriptMetadata> ReadScriptMetadataAsync(string scriptPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(scriptPath)
                                   .Replace('_', ' ');

            string title = fileName;
            string description = $"Run script: {Path.GetFileName(scriptPath)}";
            string iconHex = "E8B7"; // default: Archive icon

            try
            {
                // Read only the first 20 lines for performance
                var lines = new List<string>();
                using var reader = new StreamReader(scriptPath);
                for (int i = 0; i < 20 && !reader.EndOfStream; i++)
                    lines.Add(await reader.ReadLineAsync() ?? "");

                foreach (var line in lines)
                {
                    var m = Regex.Match(line,
                        @"#\s*SwiftDrop-(\w+):\s*(.+)", RegexOptions.IgnoreCase);
                    if (!m.Success) continue;

                    string key = m.Groups[1].Value.ToLower();
                    string val = m.Groups[2].Value.Trim();

                    switch (key)
                    {
                        case "title": title = val; break;
                        case "description": description = val; break;
                        case "icon": iconHex = val; break;
                    }
                }
            }
            catch { /* If we can't read metadata, defaults are fine */ }

            return new ScriptMetadata(title, description, iconHex);
        }

        private async Task EnsureStarterScriptsAsync()
        {
            Directory.CreateDirectory(_scriptsFolderPath);
            RemoveDeprecatedStarterScripts();

            var bundledScriptsPath = Path.Combine(AppContext.BaseDirectory, "DefaultScripts");
            if (!Directory.Exists(bundledScriptsPath))
                return;

            var bundledScripts = Directory.GetFiles(bundledScriptsPath, "*.ps1", SearchOption.TopDirectoryOnly);
            foreach (var sourceScript in bundledScripts.OrderBy(Path.GetFileName))
            {
                var targetScript = Path.Combine(_scriptsFolderPath, Path.GetFileName(sourceScript));
                if (File.Exists(targetScript))
                    continue;

                using var source = File.OpenRead(sourceScript);
                using var destination = File.Create(targetScript);
                await source.CopyToAsync(destination);
            }
        }

        private void RemoveDeprecatedStarterScripts()
        {
            string[] deprecatedScripts =
            {
                "Example_Hello.ps1",
                "15_Compress_Item_To_Zip.ps1"
            };

            foreach (var scriptName in deprecatedScripts)
            {
                var scriptPath = Path.Combine(_scriptsFolderPath, scriptName);
                if (!File.Exists(scriptPath))
                    continue;

                try
                {
                    File.Delete(scriptPath);
                }
                catch
                {
                    // Leave the file in place if it is locked or inaccessible.
                }
            }
        }
    }
}
