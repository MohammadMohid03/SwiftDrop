using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SwiftDrop.Helpers;
using SwiftDrop.Services;
using SwiftDrop.ViewModels;

namespace SwiftDrop
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        public TrayIconService? TrayService { get; set; }
        public GlobalDragHookService? DragHookService { get; set; }

        private bool _isPanelOpen = true;
        private bool _isDragActive = false;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            // Enable file drop on the whole window
            AllowDrop = true;
            DragEnter += MainWindow_DragEnter;
            DragOver += MainWindow_DragOver;
            DragLeave += MainWindow_DragLeave;
            Drop += MainWindow_Drop;

            // Position at top-center of screen
            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;

            // Click-outside-to-close (protected during drag)
            Deactivated += MainWindow_Deactivated;

            // Rebind child views when active profile changes
            ViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.StashViewModel))
                    DropBarView.DataContext = ViewModel.StashViewModel;
                if (args.PropertyName == nameof(ViewModel.ActionGridViewModel))
                    ActionGridView.DataContext = ViewModel.ActionGridViewModel;
            };
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PositionAtTopCenter();

            // Initialize multi-grid system asynchronously
            await ViewModel.InitializeAsync();

            // Bind initial view models
            DropBarView.DataContext = ViewModel.StashViewModel;
            ActionGridView.DataContext = ViewModel.ActionGridViewModel;

            // Build grid tabs
            RebuildGridTabs();
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Removed AcrylicHelper.EnableAcrylic(this) to prevent the OS from 
            // drawing a grayish backdrop over the entire transparent window area.
            // This ensures full transparency behind the arrow and the main panel.
        }

        private void PositionAtTopCenter()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            Left = (screenWidth - Width) / 2;
            Top = 0;
        }

        // â”€â”€ Grid Profile Tabs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void RebuildGridTabs()
        {
            // Clear all except the "+" button (last child)
            while (GridTabsPanel.Children.Count > 1)
                GridTabsPanel.Children.RemoveAt(0);

            foreach (var profile in ViewModel.MultiGrid.Profiles)
            {
                bool isActive = profile == ViewModel.MultiGrid.ActiveProfile;

                var tab = new System.Windows.Controls.Button
                {
                    Content = profile.Name,
                    Style = (Style)FindResource("GridTabButtonStyle"),
                    FontSize = 11,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isActive
                        ? new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(profile.AccentColor))
                        : (System.Windows.Media.Brush)FindResource("TextMutedBrush"),
                    Background = isActive
                        ? new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(0xEE, 0xFF, 0xFF, 0xFF))
                        : new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF)),
                    BorderBrush = isActive
                        ? new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(0x18, 0x0F, 0x17, 0x2A))
                        : System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = profile,
                    ToolTip = profile.IsDefault ? "Default Grid" : $"Right-click to rename/remove"
                };

                tab.RenderTransformOrigin = new Point(0.5, 0.5);
                tab.RenderTransform = new ScaleTransform(isActive ? 1.0 : 0.985, isActive ? 1.0 : 0.985);
                tab.Opacity = 0;
                tab.Loaded += GridTab_Loaded;
                tab.MouseEnter += GridTab_MouseEnter;
                tab.MouseLeave += GridTab_MouseLeave;

                // Click to switch
                tab.Click += (s, _) =>
                {
                    if (s is System.Windows.Controls.Button btn && btn.Tag is Models.GridProfile p)
                    {
                        ViewModel.MultiGrid.SwitchToProfile(p);
                        RebuildGridTabs();
                    }
                };

                // Right-click context menu (for non-default profiles)
                if (!profile.IsDefault)
                {
                    var menu = new System.Windows.Controls.ContextMenu();

                    var renameItem = new System.Windows.Controls.MenuItem { Header = "Rename..." };
                    renameItem.Click += (_, _) => RenameGrid_Click(profile);
                    menu.Items.Add(renameItem);

                    var removeItem = new System.Windows.Controls.MenuItem { Header = "Remove" };
                    removeItem.Click += async (_, _) =>
                    {
                        await ViewModel.MultiGrid.RemoveProfile(profile);
                        RebuildGridTabs();
                    };
                    menu.Items.Add(removeItem);

                    tab.ContextMenu = menu;
                }

                // Insert before the "+" button
                GridTabsPanel.Children.Insert(GridTabsPanel.Children.Count - 1, tab);
            }
        }

        private void GridTab_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            int index = GridTabsPanel.Children.IndexOf(button);
            if (button.RenderTransform is not ScaleTransform scale)
                return;

            var delay = TimeSpan.FromMilliseconds(Math.Max(0, index) * 20);
            button.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))
            {
                BeginTime = delay
            });

            var scaleAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(220))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim.Clone());
        }

        private void GridTab_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
                AnimateGridTab(button, 1.03);
        }

        private void GridTab_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
                AnimateGridTab(button, 1.0);
        }

        private static void AnimateGridTab(Button button, double scaleTo)
        {
            if (button.RenderTransform is not ScaleTransform scale)
                return;

            var anim = new DoubleAnimation(scaleTo, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim.Clone());
        }

        private async void AddGrid_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.MultiGrid.AddProfile();
            RebuildGridTabs();
        }

        private void RenameGrid_Click(Models.GridProfile profile)
        {
            // Simple input box â€” using a TextBox in a message-like approach
            var inputBox = new System.Windows.Controls.TextBox
            {
                Text = profile.Name,
                FontSize = 13,
                FontFamily = (FontFamily)FindResource("AppFontText"),
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0xF8, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0x18, 0x0F, 0x17, 0x2A)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                MinWidth = 180
            };

            var dialog = new Window
            {
                Title = "Rename Grid",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Content = inputBox,
                Topmost = true,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0xFF, 0xF5, 0xF7, 0xFB))
            };

            inputBox.KeyDown += (_, args) =>
            {
                if (args.Key == System.Windows.Input.Key.Enter)
                    dialog.DialogResult = true;
                else if (args.Key == System.Windows.Input.Key.Escape)
                    dialog.DialogResult = false;
            };

            inputBox.SelectAll();
            inputBox.Focus();

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputBox.Text))
            {
                profile.Name = inputBox.Text.Trim();
                _ = ViewModel.MultiGrid.RenameProfile(profile);
                RebuildGridTabs();
            }
        }

        // â”€â”€ Panel Show/Hide â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void TogglePanel()
        {
            if (_isPanelOpen)
                HidePanel();
            else
                ShowPanel();
        }

        public void ShowPanel()
        {
            if (_isPanelOpen) return;
            _isPanelOpen = true;

            MainPanel.Visibility = Visibility.Visible;
            MainPanel.Opacity = 0;

            // Show the trigger pill (stays visible while panel is open)
            FadeTriggerPill(1.0);

            // Rotate arrow to point up
            var rotateAnim = new DoubleAnimation(0, 180, TimeSpan.FromMilliseconds(360))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
            };
            ArrowRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);

            // Slide panel down from above
            var slideAnim = new DoubleAnimation(-32, 0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            PanelTranslate.BeginAnimation(TranslateTransform.YProperty, slideAnim);

            var scaleAnim = new DoubleAnimation(0.992, 1.0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim.Clone());

            // Fade in
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160));
            MainPanel.BeginAnimation(OpacityProperty, fadeAnim);
        }

        public void HidePanel()
        {
            if (!_isPanelOpen) return;
            _isPanelOpen = false;

            // Hide the trigger pill back to subtle hint
            FadeTriggerPill(0.08);

            // Rotate arrow back to point down
            var rotateAnim = new DoubleAnimation(180, 0, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            ArrowRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);

            // Slide panel up
            var slideAnim = new DoubleAnimation(0, -36, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            slideAnim.Completed += (_, _) =>
            {
                MainPanel.Visibility = Visibility.Collapsed;
            };
            PanelTranslate.BeginAnimation(TranslateTransform.YProperty, slideAnim);

            var scaleAnim = new DoubleAnimation(1.0, 0.992, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim.Clone());

            // Fade out
            var fadeAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            MainPanel.BeginAnimation(OpacityProperty, fadeAnim);
        }

        // â”€â”€ Trigger Pill Fade â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void FadeTriggerPill(double toOpacity)
        {
            var anim = new DoubleAnimation(toOpacity, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            TriggerPill.BeginAnimation(OpacityProperty, anim);
        }

        private void AnimateTriggerScale(double scale)
        {
            var anim = new DoubleAnimation(scale, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            TriggerPillScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            TriggerPillScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim.Clone());
        }

        // â”€â”€ Trigger Bar Events â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void TriggerBar_Click(object sender, MouseButtonEventArgs e)
        {
            TogglePanel();
            e.Handled = true;
        }

        private void TriggerBar_MouseEnter(object sender, MouseEventArgs e)
        {
            // Fade in the trigger pill
            FadeTriggerPill(1.0);
            AnimateTriggerScale(1.04);
        }

        private void TriggerBar_MouseLeave(object sender, MouseEventArgs e)
        {
            // Only fade out if the panel is NOT open
            if (!_isPanelOpen)
            {
                FadeTriggerPill(0.08);
            }

            AnimateTriggerScale(1.0);
        }

        // â”€â”€ Close/Deactivate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            // Don't hide during active drag operations
            if (_isDragActive)
                return;

            // Click outside â†’ hide panel
            if (_isPanelOpen)
                HidePanel();
        }

        // â”€â”€ Drag & Drop â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            _isDragActive = true;

            // Auto-show panel when dragging files onto the window
            if (!_isPanelOpen)
                ShowPanel();

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            _isDragActive = true;
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void MainWindow_DragLeave(object sender, DragEventArgs e)
        {
            // Small delay before clearing drag flag (avoids flickering
            // when cursor moves between child elements)
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                _isDragActive = false;
            });
        }

        private async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            _isDragActive = false;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                int addedToGrid = 0;
                int addedToStash = 0;

                foreach (var file in files)
                {
                    string ext = System.IO.Path.GetExtension(file).ToLowerInvariant();

                    // .exe, .lnk, or folder â†’ try to create action tile
                    if (ext is ".exe" or ".lnk" || System.IO.Directory.Exists(file))
                    {
                        bool added = await ViewModel.ActionGridViewModel.TryAddDynamicActionAsync(file);
                        if (added) addedToGrid++;
                        else addedToStash++; // duplicate, fall through to stash
                    }
                    else
                    {
                        // Regular files â†’ stash
                        var item = new Models.DroppedFileItem { Path = file };
                        await ViewModel.StashViewModel.AddItemAsync(item);
                        addedToStash++;
                    }
                }

                // Feedback
                if (addedToGrid > 0 && addedToStash > 0)
                    Services.ToastNotification.Show($"âš¡ {addedToGrid} action(s) added to grid + {addedToStash} file(s) stashed");
                else if (addedToGrid > 0)
                    Services.ToastNotification.Show($"âš¡ {addedToGrid} shortcut(s) added to action grid!");
                else if (addedToStash > 0)
                    Services.ToastNotification.Show($"ðŸ“ {addedToStash} file(s) added to Drop Bar");
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.Data.GetData(DataFormats.Text);
                await ViewModel.HandleDroppedTextAsync(text);
            }
        }
    }
}
