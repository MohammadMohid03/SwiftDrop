// SwiftDrop/Models/ActionResult.cs
// Returned by every IActionService.ExecuteAsync() call.

namespace SwiftDrop.Models
{
    /// <summary>
    /// The result of executing an action on a dropped file.
    /// Used to drive UI feedback (success toast vs error dialog).
    /// </summary>
    public sealed class ActionResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Optional: path to the output file, shown in success toast.
        /// </summary>
        public string? OutputPath { get; init; }

        /// <summary>
        /// Optional: URL copied to clipboard (for Imgur action).
        /// </summary>
        public string? ClipboardUrl { get; init; }

        // ── Factory helpers ────────────────────────────────────────────────────

        public static ActionResult Ok(string message, string? outputPath = null, string? url = null)
            => new() { Success = true, Message = message, OutputPath = outputPath, ClipboardUrl = url };

        public static ActionResult Fail(string message)
            => new() { Success = false, Message = message };
    }
}