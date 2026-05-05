
# SwiftDrop

---

## ⚡ What is SwiftDrop?

**SwiftDrop** is a desktop-first file staging and quick-action tool for Windows. It sits quietly in your system tray and instantly appears when you drag a file to the top of your screen. 

Instead of juggling multiple Explorer windows, browser tabs, and desktop shortcuts, just **Drop, Convert, and Move** files seamlessly from one beautifully designed, unified interface.

## ✨ Features

- 🛸 **Global Drag-to-Top:** Drag any file in Windows to the top center of your screen, and the SwiftDrop panel smoothly slides down to catch it.
- 📂 **Drop Bar Stashing:** Stash multiple files from different folders in the Drop Bar. Create "File Stacks" to group them, then process them all at once.
- 🔒 **Item Locking:** Need to copy a file to multiple places? "Lock" it in the Drop Bar so it stays stashed even after you drag it out.
- 🗂️ **Multi-Grid Support:** Create separate workspaces (e.g., "Work", "Media", "Dev") with tabbed Grid Profiles, each holding its own custom actions and file stash.
- 🖼️ **Quick Look Preview:** Hover over a stashed file and press `Space` for an instant, Mac-style Quick Look preview of images, text, and code files.
- 🪟 **Pop-out Window:** Detach your stash into a floating, always-on-top glassmorphic window to keep files handy across multiple monitors.
- 🎨 **Glassmorphism UI:** Built with native WPF but styled with modern, fluid, semi-transparent aesthetics.

### 🛠️ Built-in Actions

- **To ZIP:** Compress all stashed files into a single, timestamped ZIP archive on your Desktop.
- **Image Conversion:** Convert `PNG → JPG` and `JPG → PNG` losslessly with a single click.
- **Convert to PDF:** Instantly turn any image files into a PDF document.
- **Move to Folder:** Move files to a custom destination directory that remembers your choice.
- **Imgur Upload:** Instantly upload images to Imgur and copy the direct link to your clipboard.
- **YT Download:** Drop a YouTube link onto the tile to download it directly as an MP4.

### 🚀 Extensibility

- **Dynamic App Launchers:** Drag any `.exe` or shortcut (`.lnk`) onto the grid to instantly create a custom launcher tile. Drop files onto the tile to open them with that app.
- **Folder Shortcuts:** Drop any folder onto the grid to create a quick "Move files to..." action.
- **PowerShell Plugins:** Write simple `.ps1` scripts in the `%LOCALAPPDATA%\SwiftDrop\Scripts` folder to create your own custom action tiles with full access to dropped file paths!

## 📥 Installation

1. Go to the **Releases** tab.
2. Download the standalone `SwiftDrop.exe`.
3. Run it! (No installation or .NET runtime required—it's fully self-contained).
4. *Tip: Place a shortcut in your Windows Startup folder (`Win+R` > `shell:startup`) to have it always ready.*

## 💻 Tech Stack

- **Framework:** .NET 8 WPF (C#)
- **Architecture:** MVVM using `CommunityToolkit.Mvvm`
- **Native Interop:** Win32 Hooks (`WH_MOUSE_LL`), `dwmapi.dll`
- **Dependencies:** `yt-dlp.exe` (for YouTube downloads), `System.Drawing.Common` (for imaging)

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! 
Feel free to check the [issues page](https://github.com/MohammadMohid03/SwiftDrop/issues).

---

<div align="center">
  <i>Built with ❤️ for a calmer, desktop-first workspace.</i>
</div>
