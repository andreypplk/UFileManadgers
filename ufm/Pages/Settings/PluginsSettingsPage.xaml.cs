using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ufm
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PluginsSettingsPage : Page
    {
        private MainWindow _mainWindow;

        public PluginsSettingsPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // Получаем MainWindow из параметра навигации
            if (e.Parameter is MainWindow mainWindow)
            {
                _mainWindow = mainWindow;
            }
            else
            {
                throw new ArgumentNullException(nameof(e.Parameter), "MainWindow parameter is required.");
            }

            base.OnNavigatedTo(e);
        }
    }
}
