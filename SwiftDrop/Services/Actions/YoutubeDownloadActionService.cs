using SwiftDrop.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SwiftDrop.Services.Actions
{
    public class YouTubeDownloadActionService : IActionService
    {
        public string Name => "YouTube Downloader";

        private static readonly string DownloadFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "SwiftDrop");

        public async Task<ActionResult> ExecuteAsync(string input)
        {
            try
            {
                input = input.Trim();
                if (!IsYouTubeUrl(input))
                    return ActionResult.Fail("Not a valid YouTube URL.");

                Directory.CreateDirectory(DownloadFolder);

                string arguments = $"-x --merge-output-format mp4 -o \"{DownloadFolder}%(title)s.%(ext)s\" \"{input}\"";

                var psi = new ProcessStartInfo("yt-dlp.exe", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi) ?? throw new Exception("Failed to start yt-dlp");
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                    return ActionResult.Fail($"Download error (exit {process.ExitCode})");

                return ActionResult.Ok($"Downloaded to {DownloadFolder}", outputPath: DownloadFolder);
            }
            catch (Exception ex)
            {
                return ActionResult.Fail($"Error: {ex.Message}");
            }
        }

        private static bool IsYouTubeUrl(string url)
            => Regex.IsMatch(url, @"^https?://(www\.)?(youtube\.com/watch\?|youtu\.be/)", RegexOptions.IgnoreCase);
    }
}