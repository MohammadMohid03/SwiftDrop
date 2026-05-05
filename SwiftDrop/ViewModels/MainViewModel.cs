using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwiftDrop.Models;
using SwiftDrop.Services;
using SwiftDrop.Services.Actions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace SwiftDrop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        /// <summary>Manages multiple grid profiles.</summary>
        public MultiGridViewModel MultiGrid { get; }

        /// <summary>Current active action grid (bound from MultiGrid).</summary>
        public ActionGridViewModel ActionGridViewModel => MultiGrid.ActiveActionGrid!;

        /// <summary>Current active stash (bound from MultiGrid).</summary>
        public StashViewModel StashViewModel => MultiGrid.ActiveStash!;

        [ObservableProperty]
        private bool _isPinned = false;

        public string ScriptsFolderPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SwiftDrop", "Scripts");

        public MainViewModel()
        {
            if (!Directory.Exists(ScriptsFolderPath))
                Directory.CreateDirectory(ScriptsFolderPath);

            MultiGrid = new MultiGridViewModel(ScriptsFolderPath);

            // Forward property changes from MultiGrid to our properties
            MultiGrid.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MultiGrid.ActiveActionGrid))
                    OnPropertyChanged(nameof(ActionGridViewModel));
                if (args.PropertyName == nameof(MultiGrid.ActiveStash))
                    OnPropertyChanged(nameof(StashViewModel));
            };
        }

        public async Task InitializeAsync()
        {
            await MultiGrid.InitializeAsync();
            // Notify UI that the initial active VMs are ready
            OnPropertyChanged(nameof(ActionGridViewModel));
            OnPropertyChanged(nameof(StashViewModel));
        }

        public async Task HandleDroppedFilesAsync(string[] files)
        {
            foreach (var file in files)
            {
                var item = new DroppedFileItem { Path = file };
                await StashViewModel.AddItemAsync(item);
            }
        }

        public async Task HandleDroppedTextAsync(string text)
        {
            text = text.Trim();

            if (text.Contains("youtube.com/watch") || text.Contains("youtu.be/"))
            {
                var youtubeAction = ActionGridViewModel.FindActionByType<YouTubeDownloadActionService>();
                if (youtubeAction != null)
                {
                    var result = await youtubeAction.ExecuteAsync(text);
                    NotificationService.Show(result);
                }
            }
        }
    }
}