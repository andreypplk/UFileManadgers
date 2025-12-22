using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using ufm.Pages.Settings;
using GeneralSettingsPage = ufm.GeneralSettingsPage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ufm
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingWindow : Window
    {
        private MainWindow _parentWindow;

        public SettingWindow(MainWindow parentWindow)
        {
            this.InitializeComponent();
            _parentWindow = parentWindow;
            this.Closed += SettingWindow_Closed;
            contentFrame.Navigate(typeof(GeneralSettingsPage),_parentWindow);
        }

        private void SettingWindow_Closed(object sender, WindowEventArgs e)
        {
            _parentWindow.EnableParentWindow();
        }


        private void NvSample_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                Type pageType = item.Tag switch
                {
                    "GeneralSettingsPage" => typeof(GeneralSettingsPage),
                    "AdvancedSettingsPage" => typeof(AdvancedSettingsPage),
                    "SecuritySettingsPage" => typeof(SecuritySettingsPage),
                    "PluginsSettingsPage" => typeof(PluginsSettingsPage),
                    _ => throw new ArgumentOutOfRangeException()
                };

                // Добавляем передачу родительского окна
                contentFrame.Navigate(pageType, _parentWindow);
            }
        }


        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Диалог подтверждения
            var confirmDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Reset Settings",
                Content = "Are you sure you want to reset all settings to default values?",
                PrimaryButtonText = "Reset",
                SecondaryButtonText = "Cancel"
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    // Сброс настроек
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values.Clear();

                    foreach (string containerKey in localSettings.Containers.Keys.ToList())
                    {
                        localSettings.DeleteContainer(containerKey);
                    }

                    // Успешное сообщение
                    var successDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Success",
                        Content = "Settings reset successfully",
                        PrimaryButtonText = "OK"
                    };
                    await successDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    // Сообщение об ошибке
                    var errorDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Error",
                        Content = $"Reset failed: {ex.Message}",
                        PrimaryButtonText = "OK"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }
}




