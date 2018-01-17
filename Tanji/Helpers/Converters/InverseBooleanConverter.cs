using System;
using System.Globalization;

namespace Tanji.Helpers.Converters
{
    public class InverseBooleanConverter : SingleConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }
    }
}