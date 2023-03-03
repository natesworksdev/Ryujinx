using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using Ryujinx.Input;
using System;
using System.Collections.Generic;
using AvaKey = Avalonia.Input.Key;
using Key = Ryujinx.Input.Key;

namespace Ryujinx.Ava.Input
{
    internal class AvaloniaKeyboardDriver : IGamepadDriver
    {
        private static readonly string[] _keyboardIdentifers = new string[1] { "0" };
        private readonly Control _control;
        private readonly Dictionary<AvaKey, KeyModifiers> _pressedKeys;

        public event EventHandler<KeyEventArgs> KeyPressed;
        public event EventHandler<KeyEventArgs> KeyRelease;
        public event EventHandler<string> TextInput;

        public string DriverName => "AvaloniaKeyboardDriver";
        public ReadOnlySpan<string> GamepadsIds => _keyboardIdentifers;

        public AvaloniaKeyboardDriver(Control control)
        {
            _control = control;
            _pressedKeys = new Dictionary<AvaKey, KeyModifiers>();

            _control.KeyDown += OnKeyPress;
            _control.KeyUp += OnKeyRelease;
            _control.TextInput += Control_TextInput;
            _control.AddHandler(InputElement.TextInputEvent, Control_LastChanceTextInput, RoutingStrategies.Bubble);
        }

        private void Control_TextInput(object sender, TextInputEventArgs e)
        {
            TextInput?.Invoke(this, e.Text);
        }

        private void Control_LastChanceTextInput(object sender, TextInputEventArgs e)
        {
            // Swallow event
            e.Handled = true;
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

        public IGamepad GetGamepad(string id)
        {
            if (!_keyboardIdentifers[0].Equals(id))
            {
                return null;
            }

            return new AvaloniaKeyboard(this, _keyboardIdentifers[0], LocaleManager.Instance[LocaleKeys.AllKeyboards]);
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
            _pressedKeys[args.Key] = args.KeyModifiers;

            KeyPressed?.Invoke(this, args);
        }

        protected void OnKeyRelease(object sender, KeyEventArgs args)
        {
            _pressedKeys.Remove(args.Key);

            KeyRelease?.Invoke(this, args);
        }

        internal bool IsPressed(Key key)
        {
            if (key == Key.Unbound || key == Key.Unknown)
            {
                return false;
            }

            AvaloniaKeyboardMappingHelper.TryGetAvaKey(key, out var nativeKey);

            return _pressedKeys.TryGetValue(nativeKey, out _);
        }

        internal bool IsPressed(Key key, KeyModifier modifier)
        {
            if (key == Key.Unbound || key == Key.Unknown)
            {
                return false;
            }

            AvaloniaKeyboardMappingHelper.TryGetAvaKey(key, out var nativeKey);
            bool keyPressed = _pressedKeys.TryGetValue(nativeKey, out var modifiers);

            KeyModifiers avaModifiers = KeyModifiers.None;

            if (modifier.HasFlag(KeyModifier.Alt))
            {
                avaModifiers |= KeyModifiers.Alt;
            }

            if (modifier.HasFlag(KeyModifier.Control))
            {
                avaModifiers |= KeyModifiers.Control;
            }

            if (modifier.HasFlag(KeyModifier.Shift))
            {
                avaModifiers |= KeyModifiers.Shift;
            }

            if (modifier.HasFlag(KeyModifier.Meta))
            {
                avaModifiers |= KeyModifiers.Meta;
            }

            return keyPressed && (modifiers & avaModifiers) == avaModifiers;
        }

        public void ResetKeys()
        {
            _pressedKeys.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}