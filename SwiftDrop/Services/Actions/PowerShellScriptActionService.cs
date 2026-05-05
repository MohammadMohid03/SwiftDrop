// SwiftDrop/Services/Actions/PowerShellScriptActionService.cs
//
// Wraps a user-authored PowerShell (.ps1) script as a SwiftDrop action.
// The script receives the dropped file path as its first argument: $args[0]
//
// Example script (Scripts/Resize_Image.ps1):
//   param($filePath)
//   Add-Type -AssemblyName System.Drawing
//   $img = [System.Drawing.Image]::FromFile($filePath)
//   ... do resizing logic ...
//   Write-Output "Done: $outputPath"

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SwiftDrop.Models;

namespace SwiftDrop.Services.Actions
{
    public sealed class PowerShellScriptActionService : IActionService
    {
        public string Name { get; }

        /// <summary>Full path to the .ps1 script file.</summary>
        private readonly string _scriptPath;

        public PowerShellScriptActionService(string scriptPath)
        {
            _scriptPath = scriptPath;
            // Derive name from script filename without extension
            Name = Path.GetFileNameWithoutExtension(scriptPath)
                       .Replace('_', ' ');
        }

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(_scriptPath))
                        return ActionResult.Fail($"Script not found: {_scriptPath}");

                    // Launch PowerShell 7 (pwsh) preferentially, fall back to 5 (powershell)
                    string pwshExe = ResolvePowerShellExecutable();

                    var psi = new ProcessStartInfo(pwshExe)
                    {
                        // -ExecutionPolicy Bypass allows running unsigned local scripts
                        // -NonInteractive prevents any prompts from blocking SwiftDrop
                        // -File passes the script path
                        // Input file path is passed as an argument
                        Arguments = $"-ExecutionPolicy Bypass -NonInteractive " +
                                                 $"-File \"{_scriptPath}\" \"{input}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi)
                        ?? throw new InvalidOperationException(
                               "Failed to launch PowerShell.");

                    string stdout = await process.StandardOutput.ReadToEndAsync();
                    string stderr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                        return ActionResult.Fail(
                            $"Script exited with code {process.ExitCode}.\n{stderr}");

                    string output = stdout.Trim();
                    return ActionResult.Ok(
                        string.IsNullOrEmpty(output) ? "Script completed." : output);
                }
                catch (Exception ex)
                {
                    return ActionResult.Fail($"Script error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Returns the path to pwsh.exe (PS 7+) if installed,
        /// otherwise falls back to the built-in powershell.exe (PS 5).
        /// </summary>
        private static string ResolvePowerShellExecutable()
        {
            // Common PS 7 installation paths
            string[] ps7Candidates =
            {
                @"C:\Program Files\PowerShell\7\pwsh.exe",
                @"C:\Program Files\PowerShell\7-preview\pwsh.exe"
            };

            foreach (var candidate in ps7Candidates)
                if (File.Exists(candidate)) return candidate;

            return "powershell.exe"; // fall back to built-in PS 5
        }
    }
}