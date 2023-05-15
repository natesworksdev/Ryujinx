using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Ryujinx.Common.Utilities;
using System;
using System.Globalization;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class FileSizeConverter : MarkupExtension, IValueConverter
    {
        private static readonly FileSizeConverter _instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // TODO: If a base 2/base 10 switch is ever implemented, investigate how passing it into this method would work
            if (value == null)
            {
                return "0 B";
            }

            if (value is long fileSize)
            {
                return ValueFormatUtils.FormatFileSize(fileSize);
            }

            throw new NotSupportedException();
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