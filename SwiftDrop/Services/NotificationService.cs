using System;
using System.Windows;
using SwiftDrop.Models;

namespace SwiftDrop.Services
{
    public static class NotificationService
    {
        public static void Show(ActionResult result)
        {
            var icon = result.Success ? "OK" : "Error";
            MessageBox.Show(result.Message, $"SwiftDrop - {icon}", 
                MessageBoxButton.OK, 
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
    }
}