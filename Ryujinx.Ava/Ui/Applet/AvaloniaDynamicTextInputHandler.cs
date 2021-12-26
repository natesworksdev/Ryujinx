using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using OpenTK.Windowing.Common;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.HLE.Ui;
using Ryujinx.Input.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Applet
{
    class AvaloniaDynamicTextInputHandler : IDynamicTextInputHandler
    {
        private MainWindow _parent;
        private OffscreenTextBox _hiddenTextBox;
        private bool _canProcessInput;
        private long _lastInputTimestamp;
        private IDisposable _textChangedSubscription;
        private IDisposable _selectionStartChangedSubscription;
        private IDisposable _selectionEndtextChangedSubscription;

        public AvaloniaDynamicTextInputHandler(MainWindow parent)
        {
            _parent = parent;

            (_parent.InputManager.KeyboardDriver as AvaloniaKeyboardDriver).KeyPressed += AvaloniaDynamicTextInputHandler_KeyPressed;
            (_parent.InputManager.KeyboardDriver as AvaloniaKeyboardDriver).KeyRelease += AvaloniaDynamicTextInputHandler_KeyRelease;
            (_parent.InputManager.KeyboardDriver as AvaloniaKeyboardDriver).TextInput  += AvaloniaDynamicTextInputHandler_TextInput;

            _hiddenTextBox = _parent.HiddenTextBox;

            Dispatcher.UIThread.Post(() =>
            {
                _textChangedSubscription = _hiddenTextBox.GetObservable(TextBox.TextProperty).Subscribe(TextChanged);
                _selectionStartChangedSubscription = _hiddenTextBox.GetObservable(TextBox.SelectionStartProperty).Subscribe(SelectionChanged);
                _selectionEndtextChangedSubscription = _hiddenTextBox.GetObservable(TextBox.SelectionEndProperty).Subscribe(SelectionChanged);
            });
        }

        private void TextChanged(string text)
        {
            TextChangedEvent?.Invoke(text ?? string.Empty,
                _hiddenTextBox.SelectionStart,
                _hiddenTextBox.SelectionEnd, true);
        }

        private void SelectionChanged(int selection)
        {
            if (_hiddenTextBox.SelectionEnd < _hiddenTextBox.SelectionStart)
            {
                _hiddenTextBox.SelectionStart = _hiddenTextBox.SelectionEnd;
            }
            TextChangedEvent?.Invoke(_hiddenTextBox.Text ?? string.Empty,
                _hiddenTextBox.SelectionStart,
                _hiddenTextBox.SelectionEnd, true);
        }

        private void AvaloniaDynamicTextInputHandler_TextInput(object sender,TextInputEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_canProcessInput)
                {
                    _hiddenTextBox.SendText(e.AsString);
                }
            });
        }

        private void AvaloniaDynamicTextInputHandler_KeyRelease(object sender, Avalonia.Input.KeyEventArgs e)
        {
            var key = (Ryujinx.Common.Configuration.Hid.Key)AvaloniaMappingHelper.ToInputKey(e.Key);

            if (!(KeyReleasedEvent?.Invoke(key)).GetValueOrDefault(true))
            {
                return;
            }

            e.RoutedEvent = _hiddenTextBox.GetKeyUpRoutedEvent();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_canProcessInput)
                {
                    _hiddenTextBox.SendKeyUpEvent(e);
                }
            });
        }

        private void AvaloniaDynamicTextInputHandler_KeyPressed(object sender, Avalonia.Input.KeyEventArgs e)
        {
            var key = (Ryujinx.Common.Configuration.Hid.Key)AvaloniaMappingHelper.ToInputKey(e.Key);

            if (!(KeyPressedEvent?.Invoke(key)).GetValueOrDefault(true))
            {
                return;
            }

            e.RoutedEvent = _hiddenTextBox.GetKeyUpRoutedEvent();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_canProcessInput)
                {
                    _hiddenTextBox.SendKeyDownEvent(e);
                    _lastInputTimestamp = DateTime.Now.Ticks;
                }
            });
        }

        public bool TextProcessingEnabled
        {
            get
            {
                return Volatile.Read(ref _canProcessInput);
            }

            set
            {
                Volatile.Write(ref _canProcessInput, value);
            }
        }

        public event DynamicTextChangedHandler TextChangedEvent;
        public event KeyPressedHandler KeyPressedEvent;
        public event KeyReleasedHandler KeyReleasedEvent;

        public void Dispose()
        {
            (_parent.InputManager.KeyboardDriver as AvaloniaKeyboardDriver).KeyPressed -= AvaloniaDynamicTextInputHandler_KeyPressed;
            (_parent.InputManager.KeyboardDriver as AvaloniaKeyboardDriver).KeyRelease -= AvaloniaDynamicTextInputHandler_KeyRelease;
            (_parent.InputManager.KeyboardDriver as AvaloniaKeyboardDriver).TextInput  -= AvaloniaDynamicTextInputHandler_TextInput;
            
            _textChangedSubscription?.Dispose();
            _selectionStartChangedSubscription?.Dispose();
            _selectionEndtextChangedSubscription?.Dispose();

            Dispatcher.UIThread.Post(() =>
            {
                _hiddenTextBox.Clear();
                _parent.GlRenderer.Focus();

                _parent = null;
            });
        }

        public void SetText(string text, int cursorBegin)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _hiddenTextBox.Text = text;
                _hiddenTextBox.CaretIndex = cursorBegin;
            });
        }

        public void SetText(string text, int cursorBegin, int cursorEnd)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _hiddenTextBox.Text = text;
                _hiddenTextBox.SelectionStart = cursorBegin;
                _hiddenTextBox.SelectionEnd = cursorEnd;
            });
        }
    }
}
