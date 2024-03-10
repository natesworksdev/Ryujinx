using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Globalization;
using System.IO;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class BitmapArrayValueConverter : IValueConverter
    {
        public static readonly BitmapArrayValueConverter Instance = new();

        private MemoryCache cache;
        private readonly int MaxCacheSize = 24;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            cache = new MemoryCache(new MemoryCacheOptions());
            var options = new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromSeconds(10), Size = 1 };

            if (value is byte[] buffer && targetType == typeof(IImage))
            {
                cache.TryGetValue(buffer, out bool result);

                if (result == false)
                {
                    MemoryStream mem = new(buffer);
                    var bitmap = new Bitmap(mem).CreateScaledBitmap(new PixelSize(256, 256));
                    cache.Set(buffer, bitmap, options);
                    return bitmap;
                }
                else
                {
                    return cache.Get(buffer);
                }
            }

            if (cache.Count >= MaxCacheSize)
            {
                cache.Compact(50);
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
