using System;
using System.Globalization;

namespace Tanji.Helpers.Converters
{
    public class BooleanOrConverter : MultiConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return false;
            }

            var left = ((values[0] as bool?) ?? false);
            var right = ((values[1] as bool?) ?? false);
            return (left | right);
        }
        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}