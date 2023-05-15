using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Utilities;
using System;
using System.Globalization;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class NullableTimeSpanConverter : MarkupExtension, IValueConverter
    {
        private static readonly NullableTimeSpanConverter _instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ValueFormatUtils.FormatTimeSpan((TimeSpan?)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance;
        }
    }
}