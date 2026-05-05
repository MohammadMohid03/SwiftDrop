using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SwiftDrop.Models;
using SwiftDrop.ViewModels;

namespace SwiftDrop.Views
{
    public partial class DropBarPopoutWindow : Window
    {
        public DropBarPopoutWindow(StashViewModel stashViewModel)
        {
            InitializeComponent();
            DataContext = stashViewModel;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ── Accept file drops ────────────────────────────────────────────

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is not StashViewModel vm) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                    await vm.AddItemAsync(new DroppedFileItem { Path = file });
                Services.ToastNotification.Show($"📁 {files.Length} file(s) added");
            }
            e.Handled = true;
        }

        // ── Drag item out ────────────────────────────────────────────────

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not Border border) return;
            if (DataContext is not StashViewModel vm) return;

            var allPaths = vm.StashedItems
                .Where(item => File.Exists(item.Path) || Directory.Exists(item.Path))
                .Select(item => item.Path)
                .ToArray();

            if (allPaths.Length > 0)
            {
                var data = new DataObject(DataFormats.FileDrop, allPaths);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        // ── Context menu: Remove ─────────────────────────────────────────

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is DroppedFileItem item &&
                DataContext is StashViewModel vm)
            {
                vm.RemoveItemCommand.Execute(item);
            }
        }
    }
}
