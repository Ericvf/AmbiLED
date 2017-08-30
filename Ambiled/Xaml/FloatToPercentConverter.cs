using System;
using System.Globalization;
using System.Windows.Data;

namespace Ambiled.Xaml
{
    public sealed class FloatToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float)
                return string.Format(@"{0} %", (float)value * 100);

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
