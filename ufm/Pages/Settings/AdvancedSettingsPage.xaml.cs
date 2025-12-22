using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Core_FileManagement;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.InteropServices;

namespace ufm
{
    public sealed partial class AdvancedSettingsPage : Page
    {
        //private MainWindow _mainWindow;

        public AdvancedSettingsPage()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("AdvancedSettingsPage loaded");

                // Загружаем состояние настройки навигации
                LoadNavigationBackItemSetting();
                LoadExpandedTreeSelected();
                LoadExpanderNodesSFStartsSetting();
                LoadExpanderNodesMyPcStartsSetting();
                LoadSingleClickOpenSetting();

                // Загружаем остальные настройки
                //ShowFileExtensionsCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowFileExtensions", false);
                //ShowHiddenFilesCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowHiddenFiles", false);
                //HideSystemFilesCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("HideSystemFiles", true);
                //ShowRibbonCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowRibbon", true);
                //SingleInstanceCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("SingleInstance", false);
                //ShowPreviewPaneCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowPreviewPane", false);
                //ShowQuickAccessCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowQuickAccess", true);
                //ShowNotificationsCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowNotifications", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnLoaded error: {ex}");
            }
        }
        private void LoadNavigationBackItemSetting()
        {
            try
            {
                // Загружаем состояние настройки навигации
                ShowNavigationBackItemCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ShowNavigationBackItem", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadNavigationBackItemSetting error: {ex}");
            }
        }
        private void LoadExpandedTreeSelected()
        {
            try
            {
                // Загружаем состояние настройки навигации
                ShowExpandedTreeSelectedCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ExpandedTreeSelected", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadNavigationBackItemSetting error: {ex}");
            }
        }
        private void LoadExpanderNodesSFStartsSetting() // Новый метод
        {
            try
            {
                // Загружаем состояние настройки раскрытия узлов специальных папок при старте
                ExpanderNodesSFStartsCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ExpanderNodesSFStarts", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadExpanderNodesSFStartsSetting error: {ex}");
            }
        }
        private void LoadExpanderNodesMyPcStartsSetting() // Новый метод
        {
            try
            {
                // Загружаем состояние настройки раскрытия узлов специальных папок при старте
                ExpanderNodesMyPcStartsCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("ExpanderNodesMyPcStarts", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadExpanderNodesMyPcStartsSetting error: {ex}");
            }
        }
        private void LoadSingleClickOpenSetting()
        {
            try
            {
                SingleClickOpenCheckBox.IsChecked = App.SettingsManager.GetSetting<bool>("SingleClickOpen", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSingleClickOpenSetting error: {ex}");
            }
        }

        private void ShowNavigationBackItemCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool showBackNavigation = ShowNavigationBackItemCheckBox.IsChecked ?? false;

                // Сохраняем настройку
                App.SettingsManager.SaveSetting("ShowNavigationBackItem", showBackNavigation);

                Debug.WriteLine($"Navigation back item setting changed: {showBackNavigation}");

                // Прямой вызов через медиатор - ВСЕ ОСТАЛЬНЫЕ КЛАССЫ НЕ НУЖНЫ!
                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowNavigationBackItemCheckBox_Changed error: {ex}");
            }
        }

        private void ShowExpandedTreeSelectedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool boolExpandedTreeSelected = ShowExpandedTreeSelectedCheckBox.IsChecked ?? false;

                // Сохраняем настройку
                App.SettingsManager.SaveSetting("ExpandedTreeSelected", boolExpandedTreeSelected);

                // Прямой вызов через медиатор - ВСЕ ОСТАЛЬНЫЕ КЛАССЫ НЕ НУЖНЫ!
                NavigationSettingsMediator.NotifySettingsChanged(boolExpandedTreeSelected);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowExpandedTreeSelectedCheckBox_Checked error: {ex}");
            }
        }

        private void ExpanderNodesSFStartsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool ExpNode = ExpanderNodesSFStartsCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ExpanderNodesSFStarts", ExpNode);
                Debug.WriteLine($"Expander nodes on start setting changed: {ExpNode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExpanderNodesSFStartsCheckBox_Changed error: {ex}");
            }
        }
        private void ExpanderNodesMyPcStartsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool ExpNode = ExpanderNodesMyPcStartsCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ExpanderNodesMyPcStarts", ExpNode);
                Debug.WriteLine($"Expander nodes on start setting changed: {ExpNode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExpanderNodesMyPcStartsCheckBox_Changed error: {ex}");
            }
        }
        private void SingleClickOpenCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool boolsingleClickOpen = SingleClickOpenCheckBox.IsChecked ?? false;

                // Сохраняем настройку
                App.SettingsManager.SaveSetting("SingleClickOpen", boolsingleClickOpen);

                Debug.WriteLine($"Single click open setting changed: {boolsingleClickOpen}");

                // Если нужно уведомить другие части приложения
                NavigationSettingsMediator.NotifySettingsChanged(boolsingleClickOpen);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SingleClickOpenCheckBox_Changed error: {ex}");
            }
        }
        private void ShowHiddenFilesCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool showHidden = ShowHiddenFilesCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ShowHiddenFiles", showHidden);
                Debug.WriteLine($"Hidden files setting changed: {showHidden}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowHiddenFilesCheckBox_Changed error: {ex}");
            }
        }

        private void HideSystemFilesCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool hideSystemFiles = HideSystemFilesCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("HideSystemFiles", hideSystemFiles);
                Debug.WriteLine($"System files setting changed: {hideSystemFiles}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HideSystemFilesCheckBox_Changed error: {ex}");
            }
        }

        private void ShowRibbonCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool showRibbon = ShowRibbonCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ShowRibbon", showRibbon);
                Debug.WriteLine($"Ribbon setting changed: {showRibbon}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowRibbonCheckBox_Changed error: {ex}");
            }
        }

        private void SingleInstanceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool singleInstance = SingleInstanceCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("SingleInstance", singleInstance);
                Debug.WriteLine($"Single instance setting changed: {singleInstance}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SingleInstanceCheckBox_Changed error: {ex}");
            }
        }

        private void ShowPreviewPaneCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool showPreview = ShowPreviewPaneCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ShowPreviewPane", showPreview);
                Debug.WriteLine($"Preview pane setting changed: {showPreview}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowPreviewPaneCheckBox_Changed error: {ex}");
            }
        }

        private void ShowQuickAccessCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool showQuickAccess = ShowQuickAccessCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ShowQuickAccess", showQuickAccess);
                Debug.WriteLine($"Quick access setting changed: {showQuickAccess}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowQuickAccessCheckBox_Changed error: {ex}");
            }
        }

        private void ShowNotificationsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                bool showNotifications = ShowNotificationsCheckBox.IsChecked ?? false;
                App.SettingsManager.SaveSetting("ShowNotifications", showNotifications);
                Debug.WriteLine($"Notifications setting changed: {showNotifications}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowNotificationsCheckBox_Changed error: {ex}");
            }
        }


        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedTo error: {ex}");
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                base.OnNavigatingFrom(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatingFrom error: {ex}");
            }
        }

    
    }
}
