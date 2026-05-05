using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace SwiftDrop.Models
{
    /// <summary>
    /// A group of DroppedFileItems that are stacked together.
    /// The stack appears as a single visual item in the Drop Bar;
    /// expanding it reveals the individual files.
    /// Created when a user drops a file onto an existing stash item.
    /// </summary>
    public partial class FileStack : ObservableObject
    {
        /// <summary>Display name for the stack.</summary>
        [ObservableProperty]
        private string _name = "Stack";

        /// <summary>Whether the stack is expanded (showing individual files).</summary>
        [ObservableProperty]
        private bool _isExpanded = false;

        /// <summary>The files in this stack.</summary>
        public ObservableCollection<DroppedFileItem> Items { get; } = new();

        /// <summary>Number of files in this stack.</summary>
        public int Count => Items.Count;

        /// <summary>Icon glyph based on stack content.</summary>
        public string StackIconGlyph => Items.Count > 0 ? Items[0].FileIconGlyph : "\uE8B7";

        /// <summary>Summary text for collapsed view.</summary>
        public string Summary => Items.Count == 1
            ? Items[0].Name
            : $"{Items.Count} files";

        /// <summary>All file paths for batch operations.</summary>
        public string[] AllPaths => Items.Select(i => i.Path).ToArray();

        public FileStack()
        {
            Items.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(StackIconGlyph));
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(AllPaths));
            };
        }

        /// <summary>Auto-name the stack based on its contents.</summary>
        public void AutoName()
        {
            if (Items.Count == 0)
            {
                Name = "Empty Stack";
                return;
            }

            // Name by common extension or first file
            var extensions = Items.Select(i => i.Extension).Distinct().ToList();
            if (extensions.Count == 1 && !string.IsNullOrEmpty(extensions[0]))
                Name = $"{extensions[0].TrimStart('.').ToUpper()} Stack ({Items.Count})";
            else
                Name = $"Stack ({Items.Count} files)";
        }
    }
}
