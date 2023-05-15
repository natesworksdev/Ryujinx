using System;
using System.Buffers.Text;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace Ryujinx.Common.Utilities
{
    public static class ValueFormatUtils
    {
        private static readonly string[] FILE_SIZE_UNITS_BASE10 = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        private static readonly string[] FILE_SIZE_UNITS_BASE2 = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };

        /// <summary>
        /// Can be used with <see cref="FormatFileSize"/>.
        /// </summary>
        public enum FileSizeUnits
        {
            Auto = -1,
            Bytes = 0,
            Kilobytes = 1,
            Kibibytes = 1,
            Megabytes = 2,
            Mebibytes = 2,
            Gigabytes = 3,
            Gibibytes = 3,
            Terabytes = 4,
            Tebibytes = 4,
            Petabytes = 5,
            Pebibytes = 5,
            Exabytes = 6,
            Exbibytes = 6,
        }

        /// <summary>
        /// This method formats a <see cref="TimeSpan"/> so it can be displayed in the UI.
        /// Only used as a canonical way to format the LastPlayed value.
        /// </summary>
        /// <param name="timeSpan">The <see cref="TimeSpan"/> to be formatted.</param>
        /// <returns>A formatted string that can be displayed in the UI.</returns>
        public static string FormatTimeSpan(TimeSpan? timeSpan)
        {
            if (!timeSpan.HasValue || timeSpan.Value.TotalSeconds < 1)
            {
                // Game was never played
                return TimeSpan.Zero.ToString("c", CultureInfo.InvariantCulture);
            }

            if (timeSpan.Value.TotalDays < 1)
            {
                // Game was played for less than a day
                return timeSpan.Value.ToString("c", CultureInfo.InvariantCulture);
            }

            // Game was played for more than a day
            TimeSpan onlyTime = timeSpan.Value.Subtract(TimeSpan.FromDays(timeSpan.Value.Days));
            string onlyTimeString = onlyTime.ToString("c", CultureInfo.InvariantCulture);
            return $"{timeSpan.Value.Days}d, {onlyTimeString}";
        }

        /// <summary>
        /// This method parses a string into a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="timeSpanString">A string representing a <see cref="TimeSpan"/>.</param>
        /// <returns>A <see cref="TimeSpan"/> object. If the input string couldn't been parsed, <see cref="TimeSpan.Zero"/> is returned.</returns>
        public static TimeSpan ParseTimeSpan(string timeSpanString)
        {
            var returnTimeSpan = TimeSpan.Zero;

            var valueSplit = timeSpanString.Split(", ");
            if (valueSplit.Length > 1)
            {
                var dayPart = valueSplit[0].Split("d")[0];
                if (int.TryParse(dayPart, out int days))
                {
                    returnTimeSpan = returnTimeSpan.Add(TimeSpan.FromDays(days));
                }
            }

            if (TimeSpan.TryParse(valueSplit.Last(), out TimeSpan parsedTimeSpan))
            {
                returnTimeSpan = returnTimeSpan.Add(parsedTimeSpan);
            }

            return returnTimeSpan;
        }

        /// <summary>
        /// This method formats a <see cref="DateTime"/> so it can be displayed in the UI.
        /// Only used as a canonical way to format the TimePlayed value.
        /// </summary>
        /// <param name="utcDateTime">The <see cref="DateTime"/> to be formatted. This is expected to be UTC-based.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> that's used in formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>. Do not use outside of value converters.</param>
        /// <returns>A formatted string that can be displayed in the UI.</returns>
        public static string FormatDateTime(DateTime? utcDateTime, CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            if (!utcDateTime.HasValue)
            {
                // TODO: maybe put localized string here instead of just "Never"
                return "Never";
            }

            return utcDateTime.Value.ToLocalTime().ToString(culture);
        }

        /// <summary>
        /// This method parses a string that was used in the UI into a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTimeString">The string representing a <see cref="DateTime"/>.</param>
        /// <returns>A <see cref="DateTime"/> object. If the input string couldn't be parsed, <see cref="DateTime.UnixEpoch"/> is returned.</returns>
        public static DateTime ParseDateTime(string dateTimeString)
        {
            if (!DateTime.TryParse(dateTimeString, CultureInfo.CurrentCulture, out DateTime parsedDateTime))
            {
                return DateTime.UnixEpoch;
            }

            return parsedDateTime;
        }

        /// <summary>
        /// Formats a number of bytes to a human-readable string with adjustable units.
        /// </summary>
        /// <param name="size">The file size in bytes.</param>
        /// <param name="base10">
        /// Specifies what rules to use when formatting the size value.
        /// If true, uses base 10 units like KB, MB, GB and makes 1 KB == 1000 B.
        /// If false, uses base 2 units like KiB, MiB, GiB and makes 1 KiB == 1024 B.
        /// </param>
        /// <param name="forceUnit">Formats the passed size value as this unit, bypassing the automatic unit choice.</param>
        /// <returns>A human-readable file size string.</returns>
        public static string FormatFileSize(long size, bool base10 = false, FileSizeUnits forceUnit = FileSizeUnits.Auto)
        {
            string[] units = base10 ? FILE_SIZE_UNITS_BASE10 : FILE_SIZE_UNITS_BASE2;
            double x = base10 ? 1000 : 1024;

            if (size <= 0)
            {
                return $"0 {units[0]}";
            }

            int unitIndex;
            if (forceUnit == FileSizeUnits.Auto)
            {
                unitIndex = Convert.ToInt32(Math.Floor(Math.Log(size, x)));
            }
            else
            {
                unitIndex = (int)forceUnit;
            }

            double sizeRounded = Math.Round(size / Math.Pow(x, unitIndex), 1);
            string sizeFormatted = sizeRounded.ToString(CultureInfo.InvariantCulture);

            return $"{sizeFormatted} {units[unitIndex]}";
        }

        /// <summary>
        /// Parses a previously-formatted value and returns a number of bytes. Mainly used for sorting purposes.
        /// </summary>
        /// <param name="sizeString">A string representing a file size formatted with <see cref="FormatFileSize"/>.</param>
        /// <param name="base10">See <see cref="FormatFileSize"/>. Will cause inaccurate results if sizeString was formatted using a different base.</param>
        /// <returns>A <see cref="long"/> containing a number of bytes.</returns>
        public static long ParseFileSize(string sizeString, bool base10 = false)
        {
            string[] units = base10 ? FILE_SIZE_UNITS_BASE10 : FILE_SIZE_UNITS_BASE2;
            double x = base10 ? 1000 : 1024;

            // Enumerating over the units backwards because otherwise, sizeString.EndsWith("B") would exit the loop in the first iteration.
            for (int i = units.Length - 1; i >= 0; i--)
            {
                string unit = units[i];
                if (!sizeString.EndsWith(unit))
                {
                    continue;
                }

                string numberString = sizeString.Split(" ")[0];
                if (!double.TryParse(numberString, CultureInfo.InvariantCulture, out double number))
                {
                    break;
                }
                
                number *= Math.Pow(x, i);
                return Convert.ToInt64(number);
            }

            return 0;
        }
    }
}
