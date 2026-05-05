using SwiftDrop.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    /// <summary>
    /// Action that launches an application (.exe) and optionally passes
    /// the dropped file as an argument. Created dynamically when user
    /// drops an .exe or .lnk onto the Action Grid.
    /// </summary>
    public class AppLauncherActionService : IActionService
    {
        public string Name { get; }

        /// <summary>Full path to the .exe to launch.</summary>
        public string ExecutablePath { get; }

        public AppLauncherActionService(string exePath)
        {
            ExecutablePath = exePath;
            Name = Path.GetFileNameWithoutExtension(exePath);
        }

        public Task<ActionResult> ExecuteAsync(string input)
        {
            try
            {
                if (!File.Exists(ExecutablePath))
                    return Task.FromResult(
                        ActionResult.Fail($"Application not found: {ExecutablePath}"));

                var psi = new ProcessStartInfo(ExecutablePath)
                {
                    UseShellExecute = true
                };

                // If input is a valid file/folder, pass it as an argument
                if (!string.IsNullOrWhiteSpace(input) &&
                    (File.Exists(input) || Directory.Exists(input)))
                {
                    psi.Arguments = $"\"{input}\"";
                }

                Process.Start(psi);

                return Task.FromResult(
                    ActionResult.Ok($"Launched {Name}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    ActionResult.Fail($"Failed to launch: {ex.Message}"));
            }
        }
    }
}
