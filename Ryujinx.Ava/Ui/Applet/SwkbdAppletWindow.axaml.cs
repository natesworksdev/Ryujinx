using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Ryujinx.Ava.Ui.Windows;
using System;

namespace Ryujinx.Ava.Ui.Controls
{
    public class SwkbdAppletWindow : StyleableWindow
    {
        private Predicate<int> _checkLength;
        private int _inputMax;
        private int _inputMin;
        private string _placeholder;

        public SwkbdAppletWindow(string mainText, string secondaryText, string placeholder)
        {
            MainText = mainText;
            SecondaryText = secondaryText;
            DataContext = this;
            _placeholder = placeholder;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            SetInputLengthValidation(0, int.MaxValue); // Disable by default.
        }

        public SwkbdAppletWindow()
        {
            DataContext = this;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public string Message { get; set; } = "";
        public string MainText { get; set; } = "";
        public string SecondaryText { get; set; } = "";
        public bool IsOkPressed { get; set; }

        public TextBlock Error { get; private set; }
        public TextBox Input { get; set; }
        public Button OkButton { get; set; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Error = this.FindControl<TextBlock>("Error");
            OkButton = this.FindControl<Button>("OkButton");
            Input = this.FindControl<TextBox>("Input");

            Input.Watermark = _placeholder;

            Input.AddHandler(TextInputEvent, Message_TextInput, RoutingStrategies.Tunnel, true);
        }


        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            IsOkPressed = true;

            Close();
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void SetInputLengthValidation(int min, int max)
        {
            _inputMin = Math.Min(min, max);
            _inputMax = Math.Max(min, max);

            Error.IsVisible = false;
            Error.FontStyle = FontStyle.Italic;

            if (_inputMin <= 0 && _inputMax == int.MaxValue) // Disable.
            {
                Error.IsVisible = false;

                _checkLength = length => true;
            }
            else if (_inputMin > 0 && _inputMax == int.MaxValue)
            {
                Error.IsVisible = true;
                Error.Text = $"Must be at least {_inputMin} characters long";

                _checkLength = length => _inputMin <= length;
            }
            else
            {
                Error.IsVisible = true;
                Error.Text = $"Must be {_inputMin}-{_inputMax} characters long";

                _checkLength = length => _inputMin <= length && length <= _inputMax;
            }

            Message_TextInput(this, new TextInputEventArgs());
        }

        private void Message_TextInput(object sender, TextInputEventArgs e)
        {
            OkButton.IsEnabled = _checkLength(Message.Length);
        }

        private void Message_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && OkButton.IsEnabled)
            {
                IsOkPressed = true;

                Close();
            }
            else
            {
                OkButton.IsEnabled = _checkLength(Message.Length);
            }
        }
    }
}