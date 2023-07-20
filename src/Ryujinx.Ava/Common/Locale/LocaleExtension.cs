using Avalonia.Markup.Xaml;
using System;

namespace Ryujinx.Ava.Common.Locale
{
    internal class LocaleExtension : MarkupExtension
    {
        public LocaleExtension(LocaleKeys key)
        {
            Key = key;
        }

        public LocaleKeys Key { get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return LocaleManager.Instance[Key];
        }
    }
}
