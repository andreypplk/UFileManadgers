using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace ufm
{
    public partial class CustomProgressBar : ProgressBar
    {
        private FrameworkElement _indicator;

        public CustomProgressBar()
        {
            this.DefaultStyleKey = typeof(CustomProgressBar);
            this.Loaded += CustomProgressBar_Loaded;
            this.ValueChanged += CustomProgressBar_ValueChanged;
        }

        private void CustomProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            this.ApplyTemplate();
            _indicator = this.GetTemplateChild("Indicator") as FrameworkElement;

            if (_indicator != null)
            {
                UpdateIndicator();
                this.SizeChanged += (s, args) => UpdateIndicator();
            }
        }

        private void CustomProgressBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (_indicator == null) return;

            // Обновляем ширину
            if (this.ActualWidth > 0 && this.Value >= 0)
            {
                double newWidth = (this.Value / 100.0) * this.ActualWidth;
                _indicator.Width = newWidth;
            }

            // Обновляем цвет при превышении 80%
            if (_indicator is Border border)
            {
                if (this.Value >= 80.0)
                {
                    border.Background = (SolidColorBrush)Application.Current.Resources["CriticalProgressBrush"];
                }
                else
                {
                    border.Background = (SolidColorBrush)Application.Current.Resources["ProgressBarThumbBrush"];
                }
            }
        }
    }
}