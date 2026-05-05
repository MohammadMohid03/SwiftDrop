using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwiftDrop.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwiftDrop.ViewModels
{
    /// <summary>
    /// Manages multiple grid profiles, each with its own ActionGridViewModel and StashViewModel.
    /// Provides tab-switching, add/remove/rename profiles, and persistence.
    /// </summary>
    public partial class MultiGridViewModel : ObservableObject
    {
        public ObservableCollection<GridProfile> Profiles { get; } = new();

        [ObservableProperty]
        private GridProfile? _activeProfile;

        [ObservableProperty]
        private ActionGridViewModel? _activeActionGrid;

        [ObservableProperty]
        private StashViewModel? _activeStash;

        // Per-profile ViewModels
        private readonly System.Collections.Generic.Dictionary<string, ActionGridViewModel> _actionGrids = new();
        private readonly System.Collections.Generic.Dictionary<string, StashViewModel> _stashes = new();

        private readonly string _profilesDir;
        private readonly string _scriptsFolderPath;

        public MultiGridViewModel(string scriptsFolderPath)
        {
            _scriptsFolderPath = scriptsFolderPath;
            _profilesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SwiftDrop", "Profiles");
            Directory.CreateDirectory(_profilesDir);
        }

        public async Task InitializeAsync()
        {
            await LoadProfilesAsync();

            // If no profiles, create default
            if (Profiles.Count == 0)
            {
                var defaultProfile = new GridProfile
                {
                    Name = "Default",
                    AccentColor = "#818CF8",
                    IconGlyph = "\uE80F",
                    IsDefault = true
                };
                Profiles.Add(defaultProfile);
                await SaveProfilesAsync();
            }

            // Initialize ViewModels for each profile
            foreach (var profile in Profiles)
            {
                EnsureViewModelsForProfile(profile);
            }

            // Activate the first profile
            SwitchToProfile(Profiles[0]);
        }

        // ── Profile switching ────────────────────────────────────────────

        [RelayCommand]
        public void SwitchToProfile(GridProfile profile)
        {
            if (profile == ActiveProfile) return;

            ActiveProfile = profile;
            EnsureViewModelsForProfile(profile);
            ActiveActionGrid = _actionGrids[profile.Id];
            ActiveStash = _stashes[profile.Id];
        }

        private void EnsureViewModelsForProfile(GridProfile profile)
        {
            if (!_actionGrids.ContainsKey(profile.Id))
            {
                var actionGrid = new ActionGridViewModel(_scriptsFolderPath);
                _ = actionGrid.InitializeAsync();
                _actionGrids[profile.Id] = actionGrid;
            }

            if (!_stashes.ContainsKey(profile.Id))
            {
                _stashes[profile.Id] = new StashViewModel();
            }
        }

        // ── Add / Remove / Rename ────────────────────────────────────────

        [RelayCommand]
        public async Task AddProfile()
        {
            int num = Profiles.Count + 1;
            var colors = new[] { "#F97316", "#06B6D4", "#8B5CF6", "#EF4444", "#14B8A6" };
            var icons = new[] { "\uE8F1", "\uE8D6", "\uE80F", "\uE8B7", "\uE8A5" };

            var profile = new GridProfile
            {
                Name = $"Grid {num}",
                AccentColor = colors[(num - 1) % colors.Length],
                IconGlyph = icons[(num - 1) % icons.Length]
            };

            Profiles.Add(profile);
            EnsureViewModelsForProfile(profile);
            SwitchToProfile(profile);
            await SaveProfilesAsync();
        }

        [RelayCommand]
        public async Task RemoveProfile(GridProfile profile)
        {
            if (profile.IsDefault || Profiles.Count <= 1) return;

            Profiles.Remove(profile);
            _actionGrids.Remove(profile.Id);
            _stashes.Remove(profile.Id);

            // Switch to first remaining profile
            if (ActiveProfile == profile)
                SwitchToProfile(Profiles[0]);

            await SaveProfilesAsync();
        }

        [RelayCommand]
        public async Task RenameProfile(GridProfile profile)
        {
            // The name is bound and editable via the UI
            await SaveProfilesAsync();
        }

        // ── Persistence ──────────────────────────────────────────────────

        private record ProfileRecord(string Id, string Name, string AccentColor,
            string IconGlyph, bool IsDefault);

        private async Task SaveProfilesAsync()
        {
            try
            {
                var records = Profiles.Select(p => new ProfileRecord(
                    p.Id, p.Name, p.AccentColor, p.IconGlyph, p.IsDefault)).ToList();

                var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(
                    Path.Combine(_profilesDir, "profiles.json"), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MultiGrid] Save failed: {ex.Message}");
            }
        }

        private async Task LoadProfilesAsync()
        {
            try
            {
                var filePath = Path.Combine(_profilesDir, "profiles.json");
                if (!File.Exists(filePath)) return;

                var json = await File.ReadAllTextAsync(filePath);
                var records = JsonSerializer.Deserialize<ProfileRecord[]>(json);
                if (records == null) return;

                foreach (var rec in records)
                {
                    Profiles.Add(new GridProfile
                    {
                        Id = rec.Id,
                        Name = rec.Name,
                        AccentColor = rec.AccentColor,
                        IconGlyph = rec.IconGlyph,
                        IsDefault = rec.IsDefault
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MultiGrid] Load failed: {ex.Message}");
            }
        }
    }
}
