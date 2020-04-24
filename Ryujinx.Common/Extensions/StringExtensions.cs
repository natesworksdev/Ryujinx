using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Common.Extensions
{
    public static class StringExtensions
    {
        [return: NotNull]
        public static string RemoveFirst([NotNull] this string str, [NotNull] string toRemove)
        {
            var index = str.IndexOf(toRemove);
            if (index == -1)
            {
                return str;
            }
            return str.Remove(index);
        }

        [return: NotNull]
        public static string RemoveFirst([NotNull] this string str, char toRemove)
        {
            var index = str.IndexOf(toRemove);
            if (index == -1)
            {
                return str;
            }
            return str.Remove(index);
        }
    }
}
