//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml.Data;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Navigation;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
//using Windows.Storage;
//using ufm.Pages.Settings;
//using GeneralSettingsPage = ufm.GeneralSettingsPage;

//// To learn more about WinUI, the WinUI project structure,
//// and more about our project templates, see: http://aka.ms/winui-project-info.

//namespace ufm
//{
//    /// <summary>
//    /// An empty window that can be used on its own or navigated to within a Frame.
//    /// </summary>
//    public sealed partial class SettingWindow : Window
//    {
//        private MainWindow _parentWindow;

//        public SettingWindow(MainWindow parentWindow)
//        {
//            this.InitializeComponent();
//            _parentWindow = parentWindow;
//            this.Closed += SettingWindow_Closed;
//            contentFrame.Navigate(typeof(GeneralSettingsPage),_parentWindow);
//        }

//        private void SettingWindow_Closed(object sender, WindowEventArgs e)
//        {
//            _parentWindow.EnableParentWindow();
//        }


//        private void NvSample_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
//        {
//            if (args.SelectedItem is NavigationViewItem item)
//            {
//                Type pageType = item.Tag switch
//                {
//                    "GeneralSettingsPage" => typeof(GeneralSettingsPage),
//                    "AdvancedSettingsPage" => typeof(AdvancedSettingsPage),
//                    "SecuritySettingsPage" => typeof(SecuritySettingsPage),
//                    "PluginsSettingsPage" => typeof(PluginsSettingsPage),
//                    _ => throw new ArgumentOutOfRangeException()
//                };

//                // Добавляем передачу родительского окна
//                contentFrame.Navigate(pageType, _parentWindow);
//            }
//        }


//        //private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
//        //{
//        //    // Диалог подтверждения
//        //    var confirmDialog = new ContentDialog
//        //    {
//        //        XamlRoot = this.Content.XamlRoot,
//        //        Title = "Reset Settings",
//        //        Content = "Are you sure you want to reset all settings to default values?",
//        //        PrimaryButtonText = "Reset",
//        //        SecondaryButtonText = "Cancel"
//        //    };

//        //    if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
//        //    {
//        //        try
//        //        {
//        //            // Сброс настроек
//        //            var localSettings = ApplicationData.Current.LocalSettings;
//        //            localSettings.Values.Clear();

//        //            foreach (string containerKey in localSettings.Containers.Keys.ToList())
//        //            {
//        //                localSettings.DeleteContainer(containerKey);
//        //            }

//        //            // Успешное сообщение
//        //            var successDialog = new ContentDialog
//        //            {
//        //                XamlRoot = this.Content.XamlRoot,
//        //                Title = "Success",
//        //                Content = "Settings reset successfully",
//        //                PrimaryButtonText = "OK"
//        //            };
//        //            await successDialog.ShowAsync();
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            // Сообщение об ошибке
//        //            var errorDialog = new ContentDialog
//        //            {
//        //                XamlRoot = this.Content.XamlRoot,
//        //                Title = "Error",
//        //                Content = $"Reset failed: {ex.Message}",
//        //                PrimaryButtonText = "OK"
//        //            };
//        //            await errorDialog.ShowAsync();
//        //        }
//        //    }
//        //}
//        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
//        {
//            var confirmDialog = new ContentDialog
//            {
//                XamlRoot = this.Content.XamlRoot,
//                Title = "Reset Settings",
//                Content = "Are you sure you want to reset all settings to default values?",
//                PrimaryButtonText = "Reset",
//                SecondaryButtonText = "Cancel"
//            };

//            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
//            {
//                try
//                {
//                    // Используем централизованный сброс
//                    App.SettingsManager.ResetAllSettings();

//                    var successDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Success",
//                        Content = "Settings reset successfully",
//                        PrimaryButtonText = "OK"
//                    };
//                    await successDialog.ShowAsync();
//                }
//                catch (Exception ex)
//                {
//                    var errorDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Error",
//                        Content = $"Reset failed: {ex.Message}",
//                        PrimaryButtonText = "OK"
//                    };
//                    await errorDialog.ShowAsync();
//                }
//            }
//        }
//    }
//}

//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using SettingManager;
//using System;
//using System.Collections.Generic;
//using ufm.Pages.Settings;
//using Windows.Storage;
//using Windows.Storage.Pickers;
//using WinRT.Interop;

//namespace ufm
//{
//    public sealed partial class SettingWindow : Window
//    {
//        private MainWindow _parentWindow;
//        private bool _isPortableModeInitializing = false;

//        public SettingWindow(MainWindow parentWindow)
//        {
//            this.InitializeComponent();
//            _parentWindow = parentWindow;
//            this.Closed += SettingWindow_Closed;
//            contentFrame.Navigate(typeof(GeneralSettingsPage), _parentWindow);
//            LoadPortableModeSetting();
//        }

//        private void SettingWindow_Closed(object sender, WindowEventArgs e)
//        {
//            _parentWindow.EnableParentWindow();
//        }

//        private void NvSample_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
//        {
//            if (args.SelectedItem is NavigationViewItem item)
//            {
//                Type pageType = item.Tag switch
//                {
//                    "GeneralSettingsPage" => typeof(GeneralSettingsPage),
//                    "AdvancedSettingsPage" => typeof(AdvancedSettingsPage),
//                    "SecuritySettingsPage" => typeof(SecuritySettingsPage),
//                    "PluginsSettingsPage" => typeof(PluginsSettingsPage),
//                    _ => throw new ArgumentOutOfRangeException()
//                };

//                contentFrame.Navigate(pageType, _parentWindow);
//            }
//        }

//        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
//        {
//            var confirmDialog = new ContentDialog
//            {
//                XamlRoot = this.Content.XamlRoot,
//                Title = "Reset Settings",
//                Content = "Are you sure you want to reset all settings to default values?",
//                PrimaryButtonText = "Reset",
//                SecondaryButtonText = "Cancel"
//            };

//            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
//            {
//                try
//                {
//                    App.SettingsManager.ResetAllSettings();

//                    var successDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Success",
//                        Content = "Settings reset successfully",
//                        PrimaryButtonText = "OK"
//                    };
//                    await successDialog.ShowAsync();
//                }
//                catch (Exception ex)
//                {
//                    var errorDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Error",
//                        Content = $"Reset failed: {ex.Message}",
//                        PrimaryButtonText = "OK"
//                    };
//                    await errorDialog.ShowAsync();
//                }
//            }
//        }

//        private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
//        {
//            var savePicker = new FileSavePicker();
//            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
//            savePicker.FileTypeChoices.Add("JSON files", new List<string>() { ".json" });
//            savePicker.SuggestedFileName = "settings_backup.json";

//            var hwnd = WindowNative.GetWindowHandle(_parentWindow);
//            InitializeWithWindow.Initialize(savePicker, hwnd);

//            StorageFile file = await savePicker.PickSaveFileAsync();
//            if (file != null)
//            {
//                try
//                {
//                    App.SettingsManager.ExportSettings(file.Path);
//                    var dialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Success",
//                        Content = "Settings exported successfully.",
//                        PrimaryButtonText = "OK"
//                    };
//                    await dialog.ShowAsync();
//                }
//                catch (Exception ex)
//                {
//                    var errorDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Error",
//                        Content = $"Export failed: {ex.Message}",
//                        PrimaryButtonText = "OK"
//                    };
//                    await errorDialog.ShowAsync();
//                }
//            }
//        }

//        private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
//        {
//            var openPicker = new FileOpenPicker();
//            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
//            openPicker.FileTypeFilter.Add(".json");

//            var hwnd = WindowNative.GetWindowHandle(_parentWindow);
//            InitializeWithWindow.Initialize(openPicker, hwnd);

//            StorageFile file = await openPicker.PickSingleFileAsync();
//            if (file != null)
//            {
//                try
//                {
//                    var confirmDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Import Settings",
//                        Content = "This will replace all current settings. The application will restart to apply changes. Continue?",
//                        PrimaryButtonText = "Import",
//                        SecondaryButtonText = "Cancel"
//                    };

//                    if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
//                        return;

//                    App.SettingsManager.ImportSettings(file.Path);

//                    var successDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Success",
//                        Content = "Settings imported successfully. The application will now restart.",
//                        PrimaryButtonText = "OK"
//                    };
//                    await successDialog.ShowAsync();

//                    // Перезапуск приложения
//                    Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
//                }
//                catch (Exception ex)
//                {
//                    var errorDialog = new ContentDialog
//                    {
//                        XamlRoot = this.Content.XamlRoot,
//                        Title = "Error",
//                        Content = $"Import failed: {ex.Message}",
//                        PrimaryButtonText = "OK"
//                    };
//                    await errorDialog.ShowAsync();
//                }
//            }
//        }

//        private void LoadPortableModeSetting()
//        {
//            _isPortableModeInitializing = true;
//            try
//            {
//                object portableObj = null;
//                ApplicationData.Current.LocalSettings.Values.TryGetValue("IsPortableMode", out portableObj);
//                bool isPortable = portableObj is bool b ? b : (Windows.ApplicationModel.Package.Current == null);
//                PortableModeToggle.IsOn = isPortable;
//                UpdatePortableModeText(); // обновляем текст после загрузки
//            }
//            finally
//            {
//                _isPortableModeInitializing = false;
//            }
//        }

//        private async void PortableModeToggle_Toggled(object sender, RoutedEventArgs e)
//        {
//            if (_isPortableModeInitializing) return;

//            bool isPortable = PortableModeToggle.IsOn;
//            SettingsManager.SetStorageMode(isPortable);

//            UpdatePortableModeText(); // обновляем текст сразу при переключении

//            var dialog = new ContentDialog
//            {
//                XamlRoot = this.Content.XamlRoot,
//                Title = "Restart required",
//                Content = "The storage location will change after you restart the application.",
//                PrimaryButtonText = "OK"
//            };
//            await dialog.ShowAsync();
//        }

//        // Новый метод для изменения текста переключателя
//        private void UpdatePortableModeText()
//        {
//            if (PortableModeToggle.IsOn)
//                PortableModeToggle.Header = "Store settings next to EXE (portable)";
//            else
//                PortableModeToggle.Header = "Store settings in LocalState (standard)";
//        }
//    }
//}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SettingManager;
using System;
using System.Collections.Generic;
using ufm.Pages.Settings;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ufm
{
    public sealed partial class SettingWindow : Window
    {
        private MainWindow _parentWindow;
        private bool _isPortableModeInitializing = false;

        public SettingWindow(MainWindow parentWindow)
        {
            this.InitializeComponent();
            _parentWindow = parentWindow;
            this.Closed += SettingWindow_Closed;
            contentFrame.Navigate(typeof(GeneralSettingsPage), _parentWindow);
            LoadPortableModeSetting();
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

                contentFrame.Navigate(pageType, _parentWindow);
            }
        }

        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
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
                    App.SettingsManager.ResetAllSettings();

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

        private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON files", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "settings_backup.json";

            var hwnd = WindowNative.GetWindowHandle(_parentWindow);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    App.SettingsManager.ExportSettings(file.Path);
                    var dialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Success",
                        Content = "Settings exported successfully.",
                        PrimaryButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Error",
                        Content = $"Export failed: {ex.Message}",
                        PrimaryButtonText = "OK"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".json");

            var hwnd = WindowNative.GetWindowHandle(_parentWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    var confirmDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Import Settings",
                        Content = "This will replace all current settings. The application will restart to apply changes. Continue?",
                        PrimaryButtonText = "Import",
                        SecondaryButtonText = "Cancel"
                    };

                    if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
                        return;

                    App.SettingsManager.ImportSettings(file.Path);

                    var successDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Success",
                        Content = "Settings imported successfully. The application will now restart.",
                        PrimaryButtonText = "OK"
                    };
                    await successDialog.ShowAsync();

                    // Перезапуск приложения
                    Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Error",
                        Content = $"Import failed: {ex.Message}",
                        PrimaryButtonText = "OK"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void LoadPortableModeSetting()
        {
            _isPortableModeInitializing = true;
            try
            {
                // Используем SettingsManager для получения актуального режима
                bool isPortable = SettingsManager.IsPortableMode();
                PortableModeToggle.IsOn = isPortable;
                UpdatePortableModeTooltip();
            }
            finally
            {
                _isPortableModeInitializing = false;
            }
        }

        private async void PortableModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isPortableModeInitializing) return;

            bool isPortable = PortableModeToggle.IsOn;
            SettingsManager.SetStorageMode(isPortable);
            UpdatePortableModeTooltip();

            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Restart required",
                Content = "The storage location will change after you restart the application.",
                PrimaryButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        private void UpdatePortableModeTooltip()
        {
            if (PortableModeToggle.IsOn)
                ToolTipService.SetToolTip(PortableModeToggle, "Store settings next to EXE (portable)");
            else
                ToolTipService.SetToolTip(PortableModeToggle, "Store settings in LocalState (standard)");
        }
    }
}