using System;
using System.Globalization;
using System.Windows.Data;

namespace CLP.ADMSUpdatePlugin.Converter
{
    public class ContainsSameAsCurrentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string s && s.Contains("(Same as current value)");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
