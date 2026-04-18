using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Core_FileManagement;
using System.Diagnostics;

namespace ufm
{
    public sealed partial class StatusBarPerformanceMetricsUC : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(ExplorerItemViewModel),
                typeof(StatusBarPerformanceMetricsUC),
                new PropertyMetadata(null, OnViewModelChanged));

        public ExplorerItemViewModel ViewModel
        {
            get => (ExplorerItemViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBarPerformanceMetricsUC control)
            {
                control.DataContext = e.NewValue;                                                                                         
            }
        }

        public StatusBarPerformanceMetricsUC()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {                                                                                                                                                                                 
        }
    }
}