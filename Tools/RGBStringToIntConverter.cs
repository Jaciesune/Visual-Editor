using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VE.Tools
{
    public class RgbStringToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? "0";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value as string, out int result))
                return Math.Clamp(result, 0, 255);
            return 0;
        }
    }
}
