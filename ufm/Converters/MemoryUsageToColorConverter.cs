using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ufm
{
    public partial class MemoryUsageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double usagePercent)
            {
                return usagePercent switch
                {
                    > 85 => new SolidColorBrush(Colors.Red),
                    > 70 => new SolidColorBrush(Colors.Orange),
                    _ => new SolidColorBrush(Colors.Green)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}