using System;
using System.Linq;
using System.Collections;
using System.Globalization;

namespace Tanji.Helpers.Converters
{
    public class EnumerableToStringConverter : SingleConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumerable = (value as IEnumerable);
            if (enumerable != null)
            {
                return string.Join(parameter?.ToString() ?? "", enumerable
                    .Cast<object>()
                    .Select(o => o.ToString()));
            }
            return null;
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}