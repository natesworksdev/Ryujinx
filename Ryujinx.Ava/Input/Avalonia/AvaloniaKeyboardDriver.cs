using Avalonia.Controls;
using Avalonia.Input;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AvaKey = Avalonia.Input.Key;
using TextInputEventArgs = OpenTK.Windowing.Common.TextInputEventArgs;

namespace Ryujinx.Input.Avalonia
{
    public class AvaloniaKeyboardDriver : IGamepadDriver
    {
        private static readonly string[] _keyboardIdentifers = new string[1] {"0"};
        private readonly Control _control;
        private readonly HashSet<AvaKey> _pressedKeys;

        public event EventHandler<KeyEventArgs> KeyPressed;
        public event EventHandler<KeyEventArgs> KeyRelease;
        public event EventHandler<OpenTK.Windowing.Common.TextInputEventArgs> TextInput;

        public string DriverName => "Avalonia";

        public ReadOnlySpan<string> GamepadsIds => _keyboardIdentifers;

        public AvaloniaKeyboardDriver(Control control)
        {
            _control = control;
            _pressedKeys = new HashSet<AvaKey>();

            _control.KeyDown += OnKeyPress;
            _control.KeyUp += OnKeyRelease;
            _control.TextInput += Control_TextInput;
        }

        private void Control_TextInput(object? sender, global::Avalonia.Input.TextInputEventArgs e)
        {
            TextInput?.Invoke(this, new TextInputEventArgs(e.Text.First()));
        }

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

        public void Dispose()
        {
            Dispose(true);
        }

        public IGamepad GetGamepad(string id)
        {
            if (!_keyboardIdentifers[0].Equals(id))
            {
                return null;
            }

            return new AvaloniaKeyboard(this, _keyboardIdentifers[0], "All keyboards");
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            TextInput?.Invoke(this, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _control.KeyUp -= OnKeyPress;
                _control.KeyDown -= OnKeyRelease;
            }
        }

        protected void OnKeyPress(object sender, KeyEventArgs args)
        {
            AvaKey key = args.Key;

            _pressedKeys.Add(args.Key);

            KeyPressed?.Invoke(this, args);
        }

        protected void OnKeyRelease(object sender, KeyEventArgs args)
        {
            _pressedKeys.Remove(args.Key);

            if (args.Key == AvaKey.Return && sender is MainWindow window)
            {
                window.ViewModel.ToggleFullscreen();
            }

            KeyRelease?.Invoke(this, args);
        }

        internal bool IsPressed(Key key)
        {
            if (key == Key.Unbound || key == Key.Unknown)
            {
                return false;
            }

            AvaKey nativeKey = AvaloniaMappingHelper.ToAvaKey(key);

            return _pressedKeys.Contains(nativeKey);
        }

        public void ResetKeys()
        {
            _pressedKeys.Clear();
        }
    }
}