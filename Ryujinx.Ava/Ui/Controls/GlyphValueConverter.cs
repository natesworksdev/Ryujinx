using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.Ui.Controls
{
    public class GlyphValueConverter : MarkupExtension
    {
        private string _key;

        private static Dictionary<ViewMode, string> _glyphs = new Dictionary<ViewMode, string>
        {
            {ViewMode.List, char.ConvertFromUtf32((int)Symbol.List).ToString()},
            {ViewMode.Grid, char.ConvertFromUtf32((int)Symbol.ViewAll).ToString()},
            {ViewMode.Ui, char.ConvertFromUtf32((int)Symbol.FolderFilled).ToString()},
            {ViewMode.Input, char.ConvertFromUtf32((int)Symbol.GamesFilled).ToString()},
            {ViewMode.Settings, char.ConvertFromUtf32((int)Symbol.Settings).ToString()},
            {ViewMode.Chip, char.ConvertFromUtf32((int)Symbol.CodeFilled).ToString()},
            {ViewMode.Image, char.ConvertFromUtf32((int)Symbol.Image).ToString()},
            {ViewMode.Speaker, char.ConvertFromUtf32((int)Symbol.Audio).ToString()},
            {ViewMode.Network, char.ConvertFromUtf32((int)Symbol.World).ToString()},
            {ViewMode.Page, char.ConvertFromUtf32((int)Symbol.DocumentFilled).ToString()},
        };

        public GlyphValueConverter(string key)
        {
            _key = key;
        }

        public string this[string key]
        {
            get
            {
                if(_glyphs.TryGetValue(Enum.Parse<ViewMode>(key), out var val))
                {
                    return val;
                }

                return string.Empty;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            Avalonia.Markup.Xaml.MarkupExtensions.ReflectionBindingExtension binding = new($"[{_key}]")
            {
                Mode = BindingMode.OneWay,
                Source = this
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}