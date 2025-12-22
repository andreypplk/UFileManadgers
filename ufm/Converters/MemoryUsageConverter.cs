using System;
using Microsoft.UI.Xaml.Data;

namespace ufm
{
    public partial class MemoryUsageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long bytes && bytes > 0)
            {
                double mb = bytes / (1024.0 * 1024.0);
                return $"{mb:0} MB";
            }
            return "0 MB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}