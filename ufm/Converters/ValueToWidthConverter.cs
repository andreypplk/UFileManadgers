using System;
using Microsoft.UI.Xaml.Data;

namespace ufm;

public partial class ValueToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // value = ActualWidth контейнера (ProgressBarRoot)
        // parameter = Value прогресс-бара (UsedProcentValue)
        if (value is double actualWidth && parameter is double percent)
        {
            return (percent / 100.0) * actualWidth;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}