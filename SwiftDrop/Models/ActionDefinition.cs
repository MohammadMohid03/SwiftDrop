using System;
using SwiftDrop.Services;

namespace SwiftDrop.Models
{
    public class ActionDefinition
    {
        public string Title { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = "\uE8A5";
        public string AccentColor { get; set; } = "#0078D4";
        public string Description { get; set; } = string.Empty;
        public IActionService Service { get; set; } = null!;
        public string[] AcceptedExtensions { get; set; } = Array.Empty<string>();

        // ── Persistence fields (for user-added dynamic actions) ──────────

        /// <summary>True if this action was dynamically added by the user (not built-in).</summary>
        public bool IsUserAdded { get; set; } = false;

        /// <summary>Type discriminator for serialization: "app", "folder", "script".</summary>
        public string ActionType { get; set; } = "builtin";

        /// <summary>Target path (.exe path or folder path) for dynamic actions.</summary>
        public string TargetPath { get; set; } = string.Empty;
    }
}