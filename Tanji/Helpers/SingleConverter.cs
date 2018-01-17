using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;

namespace Tanji.Helpers
{
    public abstract class SingleConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
        public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
    }
}