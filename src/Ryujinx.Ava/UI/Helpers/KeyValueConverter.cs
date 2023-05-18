using Avalonia.Data.Converters;
using DynamicData.Kernel;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class KeyValueConverter : IValueConverter
    {
        public static KeyValueConverter Instance = new();
        internal static readonly Dictionary<Key, LocaleKeys> KeysMap = new()
        {
            { Key.Unknown, LocaleKeys.KeyUnknown },
            { Key.ShiftLeft, LocaleKeys.KeyShiftLeft },
            { Key.ShiftRight, LocaleKeys.KeyShiftRight },
            { Key.ControlLeft, LocaleKeys.KeyControlLeft },
            { Key.ControlRight, LocaleKeys.KeyControlRight },
            { Key.AltLeft, OperatingSystem.IsMacOS() ? LocaleKeys.KeyOptLeft : LocaleKeys.KeyAltLeft },
            { Key.AltRight, OperatingSystem.IsMacOS() ? LocaleKeys.KeyOptRight : LocaleKeys.KeyAltRight },
            { Key.WinLeft, OperatingSystem.IsMacOS() ? LocaleKeys.KeyCmdLeft : LocaleKeys.KeyWinLeft },
            { Key.WinRight, OperatingSystem.IsMacOS() ? LocaleKeys.KeyCmdRight : LocaleKeys.KeyWinRight },
            { Key.Up, LocaleKeys.KeyUp },
            { Key.Down, LocaleKeys.KeyDown },
            { Key.Left, LocaleKeys.KeyLeft },
            { Key.Right, LocaleKeys.KeyRight },
            { Key.Enter, LocaleKeys.KeyEnter },
            { Key.Escape, LocaleKeys.KeyEscape },
            { Key.Space, LocaleKeys.KeySpace },
            { Key.Tab, LocaleKeys.KeyTab },
            { Key.BackSpace, LocaleKeys.KeyBackSpace },
            { Key.Insert, LocaleKeys.KeyInsert },
            { Key.Delete, LocaleKeys.KeyDelete },
            { Key.PageUp, LocaleKeys.KeyPageUp },
            { Key.PageDown, LocaleKeys.KeyPageDown },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string keyString = "";

            if (value != null)
            {
                if (value is Key key)
                {
                    if (KeysMap.TryGetValue(key, out LocaleKeys localeKey))
                    {
                        keyString = LocaleManager.Instance[localeKey];
                    }
                    else
                    {
                        keyString = key.ToString();
                    }
                }
                else if (value is GamepadInputId gamepadInputId)
                {
                    keyString = value.ToString();
                }
                else if (value is StickInputId stickInputId)
                {   keyString = value.ToString();
                }
            }

            return keyString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object key = null;

            if (value != null)
            {
                if (targetType == typeof(Key))
                {
                    var optionalKey = KeysMap.FirstOrOptional(x => LocaleManager.Instance[x.Value] == value.ToString());
                    if (optionalKey.HasValue)
                    {
                        key = optionalKey.Value;
                    }
                    else
                    {
                        key = Enum.Parse<Key>(value.ToString());
                    }
                }
                else if (targetType == typeof(GamepadInputId))
                {
                    key = Enum.Parse<GamepadInputId>(value.ToString());
                }
                else if (targetType == typeof(StickInputId))
                {
                    key = Enum.Parse<StickInputId>(value.ToString());
                }
            }

            return key;
        }
    }
}
