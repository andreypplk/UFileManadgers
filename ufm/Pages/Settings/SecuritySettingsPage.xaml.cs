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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ufm.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SecuritySettingsPage : Page
    {
        private MainWindow _mainWindow;

        public SecuritySettingsPage()
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
