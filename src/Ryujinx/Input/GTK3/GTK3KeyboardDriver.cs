using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using GtkKey = Gdk.Key;

namespace Ryujinx.Input.GTK3
{
    public class Gtk3KeyboardDriver : IGamepadDriver
    {
        private readonly Widget _widget;
        private readonly HashSet<GtkKey> _pressedKeys;

        public Gtk3KeyboardDriver(Widget widget)
        {
            _widget = widget;
            _pressedKeys = new HashSet<GtkKey>();

            _widget.KeyPressEvent += OnKeyPress;
            _widget.KeyReleaseEvent += OnKeyRelease;
        }

        public string DriverName => "GTK3";

        private static readonly string[] _keyboardIdentifers = new string[1] { "0" };

        public ReadOnlySpan<string> GamepadsIds => _keyboardIdentifers;

        public event Action<string> OnGamepadConnected
        {
            add { }
            remove { }
        }

        public event Action<string> OnGamepadDisconnected
        {
            add { }
            remove { }
        }

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
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        [GLib.ConnectBefore]
        protected void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            GtkKey key = (GtkKey)Keyval.ToLower((uint)args.Event.Key);

            _pressedKeys.Add(key);
        }

        [GLib.ConnectBefore]
        protected void OnKeyRelease(object sender, KeyReleaseEventArgs args)
        {
            GtkKey key = (GtkKey)Keyval.ToLower((uint)args.Event.Key);

            _pressedKeys.Remove(key);
        }

        internal bool IsPressed(Key key)
        {
            if (key == Key.Unbound || key == Key.Unknown)
            {
                return false;
            }

            GtkKey nativeKey = Gtk3MappingHelper.ToGtkKey(key);

            return _pressedKeys.Contains(nativeKey);
        }

        public IGamepad GetGamepad(string id)
        {
            if (!_keyboardIdentifers[0].Equals(id))
            {
                return null;
            }

            return new Gtk3Keyboard(this, _keyboardIdentifers[0], "All keyboards");
        }
    }
}
