using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Ryujinx.Ava.Common.Locale;
using System;
using System.Globalization;
using System.IO;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class NullableDateTimeConverter : IValueConverter
    {
        public static NullableDateTimeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return LocaleManager.Instance[LocaleKeys.Never];
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToLocalTime().ToString(culture);
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}