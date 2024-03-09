using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;
using System.IO.Hashing;
using System.Runtime.Caching;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class BitmapArrayValueConverter : IValueConverter
    {
        public static readonly BitmapArrayValueConverter Instance = new();

        private readonly MemoryCache cache = MemoryCache.Default;
        private readonly CacheItemPolicy policy = new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromSeconds(60) };
        private readonly int MaxCacheSize = 24;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value is byte[] buffer && targetType == typeof(IImage))
            {
                var bufferhash = BufferHash(buffer);
                var retrieved = cache.Contains(bufferhash);

                if (retrieved == false)
                {
                    MemoryStream mem = new(buffer);
                    var bitmap = new Bitmap(mem).CreateScaledBitmap(new PixelSize(256, 256));
                    cache.Add(bufferhash, bitmap, policy);
                    return bitmap;
                }
                else
                {
                    return cache.Get(bufferhash);
                }
            }

            if (cache.GetCount() >= MaxCacheSize)
            {
                cache.Trim(50);
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        private static string BufferHash(byte[] input)
        {
            var hashBytes = Crc32.HashToUInt32(input);
            return hashBytes.ToString();
        }
    }
}
