using Gtk;
using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.Gamepad.GTK3
{
    public class GTK3KeyboardDriver : IGamepadDriver
    {
        private readonly Widget _widget;

        public GTK3KeyboardDriver(Widget widget)
        {
            _widget = widget;

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
            Logger.Error?.Print(LogClass.Application, args.Event.Key.ToString());
        }

        [GLib.ConnectBefore]
        protected void OnKeyRelease(object sender, KeyReleaseEventArgs args)
        {
            Logger.Error?.Print(LogClass.Application, args.Event.Key.ToString());
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
