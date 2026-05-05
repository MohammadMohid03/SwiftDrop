using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace SwiftDrop.Models
{
    /// <summary>
    /// Represents a single file/folder dropped into the Drop Bar stash.
    /// Supports locking (Copy instead of Move) and file-stack grouping.
    /// </summary>
    public partial class DroppedFileItem : ObservableObject
    {
        public string Path { get; set; } = string.Empty;
        public string Name => System.IO.Path.GetFileName(Path);
        public string Extension => System.IO.Path.GetExtension(Name).ToLowerInvariant();
        public DateTimeOffset DroppedAt { get; } = DateTimeOffset.Now;
        public bool IsFolder => Directory.Exists(Path);

        /// <summary>
        /// If true, dragging this item out performs a COPY instead of MOVE,
        /// retaining the file in the stash.
        /// </summary>
        [ObservableProperty]
        private bool _isLocked = false;

        /// <summary>
        /// Glyph icon based on file type.
        /// </summary>
        public string FileIconGlyph => Extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => "\uEB9F",
            ".mp4" or ".mov" or ".avi" or ".mkv" => "\uE8B2",
            ".mp3" or ".wav" or ".flac" or ".aac" => "\uEC4F",
            ".pdf" => "\uEA90",
            ".zip" or ".rar" or ".7z" => "\uE8B7",
            ".cs" or ".js" or ".py" or ".ts" or ".xaml" => "\uE943",
            ".exe" or ".lnk" => "\uE8FC",
            _ when IsFolder => "\uE8B7",
            _ => "\uE8A5"
        };

        /// <summary>
        /// Lock icon for UI display.
        /// </summary>
        public string LockIconGlyph => IsLocked ? "\uE72E" : "\uE785"; // Lock / Unlock

        partial void OnIsLockedChanged(bool value)
        {
            OnPropertyChanged(nameof(LockIconGlyph));
        }
    }
}