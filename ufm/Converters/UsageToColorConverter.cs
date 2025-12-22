using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ufm
{
    public partial class UsageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is float usage)
            {
                return usage switch
                {
                    > 80 => new SolidColorBrush(Colors.Red),
                    > 60 => new SolidColorBrush(Colors.Orange),
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