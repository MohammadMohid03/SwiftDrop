using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace SwiftDrop.Models
{
    /// <summary>
    /// Represents a named grid profile that contains its own set of actions.
    /// Users can switch between profiles to organize different workflows.
    /// </summary>
    public partial class GridProfile : ObservableObject
    {
        /// <summary>Unique identifier for persistence.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>Display name (e.g. "Default", "Work", "Media").</summary>
        [ObservableProperty]
        private string _name = "Default";

        /// <summary>Accent color for the tab indicator.</summary>
        [ObservableProperty]
        private string _accentColor = "#818CF8";

        /// <summary>Icon glyph for the tab.</summary>
        [ObservableProperty]
        private string _iconGlyph = "\uE80F"; // Grid icon

        /// <summary>Whether this is the built-in default profile (cannot be deleted).</summary>
        public bool IsDefault { get; set; } = false;
    }
}
