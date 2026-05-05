using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SwiftDrop.Models;
using SwiftDrop.Services;
using SwiftDrop.Services.Actions;
using SwiftDrop.ViewModels;

namespace SwiftDrop.Views
{
    public partial class ActionGridView : UserControl
    {
        private const int TileAnimationThreshold = 18;

        private static readonly Brush DefaultTileBorderBrush =
            new SolidColorBrush(Color.FromArgb(0x14, 0x0F, 0x17, 0x2A));

        public ActionGridViewModel? ViewModel
        {
            get => DataContext as ActionGridViewModel;
            set => DataContext = value;
        }

        public ActionGridView()
        {
            InitializeComponent();
        }

        private StashViewModel? GetStashViewModel()
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            return mainWindow?.ViewModel?.StashViewModel;
        }

        private static (ScaleTransform scale, TranslateTransform translate) EnsureTransforms(Border tile)
        {
            if (tile.RenderTransform is TransformGroup existing &&
                existing.Children.OfType<ScaleTransform>().FirstOrDefault() is ScaleTransform existingScale &&
                existing.Children.OfType<TranslateTransform>().FirstOrDefault() is TranslateTransform existingTranslate)
            {
                if (existing.IsFrozen || existingScale.IsFrozen || existingTranslate.IsFrozen)
                {
                    var clonedGroup = existing.Clone();
                    tile.RenderTransform = clonedGroup;

                    return (
                        clonedGroup.Children.OfType<ScaleTransform>().First(),
                        clonedGroup.Children.OfType<TranslateTransform>().First());
                }

                return (existingScale, existingTranslate);
            }

            var scale = new ScaleTransform(1.0, 1.0);
            var translate = new TranslateTransform(0, 0);
            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(translate);
            tile.RenderTransformOrigin = new Point(0.5, 0.5);
            tile.RenderTransform = group;
            return (scale, translate);
        }

        private void ActionTile_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Border tile)
                return;

            tile.Loaded -= ActionTile_Loaded;
            var (scale, translate) = EnsureTransforms(tile);
            int index = Math.Max(0, ActionsItemsControl.Items.IndexOf(tile.DataContext));

            if (ActionsItemsControl.Items.Count > TileAnimationThreshold)
            {
                tile.Opacity = 1;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
                translate.Y = 0;
                return;
            }

            var delay = TimeSpan.FromMilliseconds(index * 12);

            tile.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(140))
            {
                BeginTime = delay
            });

            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            var scaleAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(180))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim.Clone());
        }

        private void ActionTile_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border tile)
                return;

            var (scale, translate) = EnsureTransforms(tile);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.015, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = ease
            });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.015, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = ease
            });
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-2, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = ease
            });

            tile.BorderBrush = new SolidColorBrush(Color.FromArgb(0x24, 0x4C, 0x8D, 0xFF));
        }

        private void ActionTile_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border tile)
                return;

            ResetTileState(tile, 200);
        }

        private async void ActionTile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border tile || tile.DataContext is not ActionDefinition action || ViewModel == null)
                return;

            var stash = GetStashViewModel();
            if (stash == null || (stash.StashedItems.Count == 0 && stash.Stacks.Count == 0))
                return;

            var filePaths = stash.GetAllPaths().ToList();
            FlashTile(tile);

            try
            {
                var batchResult = await action.Service.ExecuteBatchAsync(filePaths);
                if (batchResult.Success)
                {
                    if (action.Service is QuickMoveActionService)
                        stash.ClearAllCommand.Execute(null);

                    ToastNotification.Show($"OK {action.Title}: {batchResult.Message}");
                    return;
                }
            }
            catch
            {
            }

            var successCount = 0;
            var failCount = 0;
            var lastMessage = string.Empty;

            foreach (var path in filePaths)
            {
                try
                {
                    var result = await ViewModel.ExecuteActionAsync(action, path);
                    if (result.Success)
                    {
                        successCount++;
                        lastMessage = result.Message;
                    }
                    else
                    {
                        failCount++;
                        lastMessage = result.Message;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    lastMessage = ex.Message;
                }
            }

            if (failCount == 0)
                ToastNotification.Show($"OK {action.Title}: {successCount} file(s) processed\n{lastMessage}");
            else
                ToastNotification.Show($"WARN {action.Title}: {successCount} succeeded, {failCount} failed\n{lastMessage}");
        }

        private void ActionTile_DragOver(object sender, DragEventArgs e)
        {
            if (sender is Border tile && tile.DataContext is ActionDefinition)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;

                    var (scale, translate) = EnsureTransforms(tile);
                    var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.025, TimeSpan.FromMilliseconds(100))
                    {
                        EasingFunction = ease
                    });
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.025, TimeSpan.FromMilliseconds(100))
                    {
                        EasingFunction = ease
                    });
                    translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-3, TimeSpan.FromMilliseconds(100))
                    {
                        EasingFunction = ease
                    });
                    tile.BorderBrush = new SolidColorBrush(Color.FromArgb(0x4A, 0x4C, 0x8D, 0xFF));
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
        }

        private void ActionTile_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border tile)
                ResetTileState(tile, 180);
        }

        private async void ActionTile_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border tile || tile.DataContext is not ActionDefinition action || ViewModel == null)
                return;

            FlashTile(tile);

            var successCount = 0;
            var failCount = 0;
            var lastMessage = string.Empty;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    try
                    {
                        var result = await ViewModel.ExecuteActionAsync(action, file);
                        if (result.Success)
                        {
                            successCount++;
                            lastMessage = result.Message;
                        }
                        else
                        {
                            failCount++;
                            lastMessage = result.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        lastMessage = ex.Message;
                    }
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.Data.GetData(DataFormats.Text);
                try
                {
                    var result = await ViewModel.ExecuteActionAsync(action, text.Trim());
                    if (result.Success)
                    {
                        successCount++;
                        lastMessage = result.Message;
                    }
                    else
                    {
                        failCount++;
                        lastMessage = result.Message;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    lastMessage = ex.Message;
                }
            }

            e.Handled = true;

            if (successCount + failCount == 1)
            {
                if (failCount > 0)
                    ToastNotification.Show($"FAIL {action.Title}: {lastMessage}");
                else
                    ToastNotification.Show($"OK {action.Title}: {lastMessage}");
            }
            else
            {
                if (failCount == 0)
                    ToastNotification.Show($"OK {action.Title}: {successCount} file(s) processed!");
                else
                    ToastNotification.Show($"WARN {action.Title}: {successCount} succeeded, {failCount} failed");
            }
        }

        private static void ResetTileState(Border tile, int durationMs)
        {
            var (scale, translate) = EnsureTransforms(tile);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = ease
            });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = ease
            });
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = ease
            });

            tile.BorderBrush = DefaultTileBorderBrush;
        }

        private static void FlashTile(Border tile)
        {
            var (scale, translate) = EnsureTransforms(tile);

            var flashDown = new DoubleAnimation(0.985, TimeSpan.FromMilliseconds(70))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var press = new DoubleAnimation(0, TimeSpan.FromMilliseconds(70))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            flashDown.Completed += (_, _) =>
            {
                var bounceBack = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var release = new DoubleAnimation(-1, TimeSpan.FromMilliseconds(150))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, bounceBack);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, bounceBack.Clone());
                translate.BeginAnimation(TranslateTransform.YProperty, release);
            };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, flashDown);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, flashDown.Clone());
            translate.BeginAnimation(TranslateTransform.YProperty, press);
        }
    }
}
