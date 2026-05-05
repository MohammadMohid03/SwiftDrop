using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SwiftDrop.Models;
using SwiftDrop.ViewModels;

namespace SwiftDrop.Views
{
    public partial class DropBarView : UserControl
    {
        private const int RowAnimationThreshold = 24;

        public StashViewModel? ViewModel
        {
            get => DataContext as StashViewModel;
            set => DataContext = value;
        }

        private bool _isDraggingStack = false;

        public DropBarView()
        {
            InitializeComponent();
        }

        // ── Accept file drops into the stash ─────────────────────────────

        private void DropBar_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.Text))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private async void DropBar_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel == null) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                    await ViewModel.AddItemAsync(new DroppedFileItem { Path = file });
                e.Handled = true;
                Services.ToastNotification.Show($"📁 {files.Length} file(s) added to Drop Bar");
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.Data.GetData(DataFormats.Text);
                if (!string.IsNullOrWhiteSpace(text))
                    await ViewModel.AddItemAsync(new DroppedFileItem { Path = text.Trim() });
                e.Handled = true;
            }
        }

        // ── Drag ENTIRE STACK ────────────────────────────────────────────

        private void StackPile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingStack = false;
            AnimateStackPile(0.97, 120);
        }

        private void StackPile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_isDraggingStack) return;
            if (ViewModel == null || (ViewModel.StashedItems.Count == 0 && ViewModel.Stacks.Count == 0)) return;

            _isDraggingStack = true;

            // Get ALL paths (flat items + items inside stacks)
            var allPaths = ViewModel.GetAllPaths();

            if (allPaths.Length > 0)
            {
                // Determine drag effect based on lock state
                // If ANY item is locked, use Copy (retains items in stash)
                bool hasLocked = ViewModel.StashedItems.Any(i => i.IsLocked);

                var data = new DataObject(DataFormats.FileDrop, allPaths);
                var effects = hasLocked
                    ? DragDropEffects.Copy
                    : DragDropEffects.Copy | DragDropEffects.Move;

                DragDrop.DoDragDrop(StackPile, data, effects);
            }

            _isDraggingStack = false;
            AnimateStackPile(1.0, 180);
        }

        private void StackPile_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimateStackPile(1.03, 220);
        }

        private void StackPile_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingStack)
                AnimateStackPile(1.0, 220);
        }

        private void AnimateStackPile(double scale, int durationMs)
        {
            var anim = new DoubleAnimation(scale, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            StackPileScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            StackPileScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim.Clone());
        }

        // ── Remove item ──────────────────────────────────────────────────

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DroppedFileItem item)
                ViewModel?.RemoveItemCommand.Execute(item);
        }

        private void RemoveItem_ContextClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is DroppedFileItem item)
                ViewModel?.RemoveItemCommand.Execute(item);
        }

        // ── Pop-out the Drop Bar into a floating window ──────────────────

        private void PopOut_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            var popout = new DropBarPopoutWindow(ViewModel);
            popout.Show();
        }

        // ── Quick Look: Spacebar preview ─────────────────────────────────

        private void DropBar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && ViewModel != null && ViewModel.StashedItems.Count > 0)
            {
                var firstItem = ViewModel.StashedItems[0];
                if (System.IO.File.Exists(firstItem.Path) || System.IO.Directory.Exists(firstItem.Path))
                {
                    QuickLookWindow.Toggle(firstItem.Path);
                    e.Handled = true;
                }
            }
        }

        // ── File Stack context menu handlers ─────────────────────────────

        private void UnpackStack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi &&
                mi.DataContext is Models.FileStack stack)
            {
                ViewModel?.UnpackStackCommand.Execute(stack);
            }
        }

        private void RemoveStack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi &&
                mi.DataContext is Models.FileStack stack)
            {
                ViewModel?.RemoveStackCommand.Execute(stack);
            }
        }

        private void FileRow_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Loaded -= FileRow_Loaded;
                AnimateRowIn(border, ViewModel?.StashedItems.IndexOf((DroppedFileItem)border.DataContext) ?? 0);
            }
        }

        private void FileRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
                AnimateRowHover(border, 1.01, -2, 0.12);
        }

        private void FileRow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
                AnimateRowHover(border, 1.0, 0, 0);
        }

        private void StackRow_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Loaded -= StackRow_Loaded;
                AnimateRowIn(border, ViewModel?.Stacks.IndexOf((FileStack)border.DataContext) ?? 0);
            }
        }

        private void StackRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
                AnimateRowHover(border, 1.01, -2, 0.10);
        }

        private void StackRow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
                AnimateRowHover(border, 1.0, 0, 0);
        }

        private static void AnimateRowIn(Border border, int index)
        {
            var (scale, translate) = EnsureRowTransforms(border);

            if (index >= RowAnimationThreshold)
            {
                border.Opacity = 1;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
                translate.Y = 0;
                return;
            }

            var delay = TimeSpan.FromMilliseconds(Math.Max(0, index) * 8);

            border.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120))
            {
                BeginTime = delay
            });

            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(140))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            var scaleAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(140))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim.Clone());
        }

        private static void AnimateRowHover(Border border, double scaleTo, double yTo, double shadowOpacity)
        {
            var (scale, translate) = EnsureRowTransforms(border);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scaleTo, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = ease
            });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scaleTo, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = ease
            });
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(yTo, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = ease
            });
        }

        private static (ScaleTransform scale, TranslateTransform translate) EnsureRowTransforms(Border border)
        {
            if (border.RenderTransform is TransformGroup group &&
                group.Children.OfType<ScaleTransform>().FirstOrDefault() is ScaleTransform scale &&
                group.Children.OfType<TranslateTransform>().FirstOrDefault() is TranslateTransform translate)
            {
                if (group.IsFrozen || scale.IsFrozen || translate.IsFrozen)
                {
                    var clonedGroup = group.Clone();
                    border.RenderTransform = clonedGroup;

                    return (
                        clonedGroup.Children.OfType<ScaleTransform>().First(),
                        clonedGroup.Children.OfType<TranslateTransform>().First());
                }

                return (scale, translate);
            }

            scale = new ScaleTransform(1, 1);
            translate = new TranslateTransform(0, 0);
            group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(translate);
            border.RenderTransform = group;
            return (scale, translate);
        }
    }
}
