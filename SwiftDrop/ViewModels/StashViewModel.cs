using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwiftDrop.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftDrop.ViewModels
{
    public partial class StashViewModel : ObservableObject
    {
        /// <summary>Flat list of all stashed items.</summary>
        public ObservableCollection<DroppedFileItem> StashedItems { get; } = new();

        /// <summary>File stacks (groups of items).</summary>
        public ObservableCollection<FileStack> Stacks { get; } = new();

        public bool HasItems => StashedItems.Count > 0;
        public int ItemCount => StashedItems.Count;
        public int StackCount => Stacks.Count;

        public StashViewModel()
        {
            StashedItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(ItemCount));
            };

            Stacks.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(StackCount));
            };
        }

        [RelayCommand]
        private void RemoveItem(DroppedFileItem item) => StashedItems.Remove(item);

        [RelayCommand]
        private void ClearAll()
        {
            StashedItems.Clear();
            Stacks.Clear();
        }

        public Task AddItemAsync(DroppedFileItem item)
        {
            StashedItems.Add(item);
            return Task.CompletedTask;
        }

        // ── File Stacks (Feature 10) ─────────────────────────────────────

        /// <summary>
        /// Creates a new stack from the given items and removes them from the flat list.
        /// </summary>
        [RelayCommand]
        private void CreateStack()
        {
            if (StashedItems.Count < 2) return;

            var stack = new FileStack();
            var items = StashedItems.ToList();

            foreach (var item in items)
            {
                StashedItems.Remove(item);
                stack.Items.Add(item);
            }

            stack.AutoName();
            Stacks.Add(stack);
        }

        /// <summary>
        /// Merges a dropped item into an existing stack.
        /// </summary>
        public void MergeIntoStack(FileStack stack, DroppedFileItem item)
        {
            if (StashedItems.Contains(item))
                StashedItems.Remove(item);

            stack.Items.Add(item);
            stack.AutoName();
        }

        /// <summary>
        /// Explodes a stack back into individual items.
        /// </summary>
        [RelayCommand]
        private void UnpackStack(FileStack stack)
        {
            foreach (var item in stack.Items)
                StashedItems.Add(item);

            Stacks.Remove(stack);
        }

        /// <summary>
        /// Removes an entire stack.
        /// </summary>
        [RelayCommand]
        private void RemoveStack(FileStack stack) => Stacks.Remove(stack);

        /// <summary>
        /// Gets ALL file paths (flat items + all stack items) for batch operations.
        /// </summary>
        public string[] GetAllPaths()
        {
            var paths = StashedItems
                .Where(i => File.Exists(i.Path) || Directory.Exists(i.Path))
                .Select(i => i.Path)
                .ToList();

            foreach (var stack in Stacks)
            {
                paths.AddRange(stack.Items
                    .Where(i => File.Exists(i.Path) || Directory.Exists(i.Path))
                    .Select(i => i.Path));
            }

            return paths.ToArray();
        }
    }
}