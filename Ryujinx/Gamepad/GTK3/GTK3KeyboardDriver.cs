using Gdk;
using Gtk;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using GtkKey = Gdk.Key;

namespace Ryujinx.Gamepad.GTK3
{
    public class GTK3KeyboardDriver : IGamepadDriver
    {
        private readonly Widget _widget;
        private HashSet<GtkKey> _pressedKeys;


        public GTK3KeyboardDriver(Widget widget)
        {
            _widget = widget;
            _pressedKeys = new HashSet<GtkKey>();

            _widget.KeyPressEvent += OnKeyPress;
            _widget.KeyReleaseEvent += OnKeyRelease;
        }

        public string DriverName => "GTK3";

        private static readonly string[] _keyboardIdentifers = new string[1] { "0" };

        public ReadOnlySpan<string> GamepadsIds => _keyboardIdentifers;

        public event Action<string> OnGamepadConnected;
        public event Action<string> OnGamepadDisconnected;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _widget.KeyPressEvent -= OnKeyPress;
                _widget.KeyReleaseEvent -= OnKeyRelease;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [GLib.ConnectBefore]
        protected void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            //Keymap.Default.TranslateKeyboardState()

            GtkKey key = (GtkKey)Keyval.ToLower((uint)args.Event.Key);

            Logger.Error?.Print(LogClass.Application, key.ToString());

            _pressedKeys.Add(key);
        }

        [GLib.ConnectBefore]
        protected void OnKeyRelease(object sender, KeyReleaseEventArgs args)
        {
            GtkKey key = (GtkKey)Keyval.ToLower((uint)args.Event.Key);

            Logger.Error?.Print(LogClass.Application, key.ToString());

            _pressedKeys.Remove(key);
        }

        internal bool IsPressed(Key key)
        {
            if (key == Key.Unbound || key == Key.Unknown)
            {
                return false;
            }

            GtkKey nativeKey = GTK3MappingHelper.ToGtkKey(key);

            //Logger.Error?.Print(LogClass.Application, nativeKey.ToString());

            return _pressedKeys.Contains(nativeKey);
        }

        public IGamepad GetGamepad(string id)
        {
            if (!_keyboardIdentifers[0].Equals(id))
            {
                return null;
            }

            return new GTK3Keyboard(this, _keyboardIdentifers[0], "All keyboards");
        }
    }
}
