using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Numerics;
using Size = System.Drawing.Size;

namespace Ryujinx.Input.Avalonia
{
    public class AvaloniaMouseDriver : IGamepadDriver
    {
        private Control _widget;
        private bool _isDisposed;

        public bool[] PressedButtons { get; }
        
        public Vector2 CurrentPosition { get; private set; }
        public Vector2 Scroll{ get; private set; }

        public AvaloniaMouseDriver(Control parent)
        {
            _widget = parent;

            _widget.PointerMoved += Parent_PointerMovedEvent;
            _widget.PointerPressed += Parent_PointerPressEvent;
            _widget.PointerReleased += Parent_PointerReleaseEvent;
            _widget.PointerWheelChanged += Parent_ScrollEvent;

            PressedButtons = new bool[(int)MouseButton.Count];
        }

        private void Parent_ScrollEvent(object o, PointerWheelEventArgs args)
        {
            Scroll = new Vector2((float)args.Delta.X, (float)args.Delta.Y);
        }
        
        private void Parent_PointerReleaseEvent(object o, PointerReleasedEventArgs args)
        {
            if (args.InitialPressMouseButton != global::Avalonia.Input.MouseButton.None)
            {
                PressedButtons[(int) (args.InitialPressMouseButton - 1)] = false;
            }
        }
        
        private void Parent_PointerPressEvent(object o, PointerPressedEventArgs args)
        {
            var pointerProperties = args.GetCurrentPoint(_widget).Properties;

            if (pointerProperties.PointerUpdateKind > PointerUpdateKind.XButton2Pressed)
            {
                PressedButtons[(int)(pointerProperties.PointerUpdateKind)] = true;
            }
        }

        private void Parent_PointerMovedEvent(object o, PointerEventArgs args)
        {
            var position = args.GetPosition(_widget);
            
            CurrentPosition = new Vector2((float) position.X, (float) position.Y);
        }

        public void SetMousePressed(MouseButton button)
        {
            PressedButtons[(int) button] = true;
        }
        
        public void SetMouseReleased(MouseButton button)
        {
            PressedButtons[(int) button] = false;
        }
        
        public void SetPosition(double x, double y)
        {
            CurrentPosition = new Vector2((float) x, (float) y);
        }

        public bool IsButtonPressed(MouseButton button)
        {
            return PressedButtons[(int) button];
        }

        public Size GetClientSize()
        {
            Size size = new();
            
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                size.Width = (int)_widget.Bounds.Width;
                size.Height = (int)_widget.Bounds.Height;
            }).Wait();

            return size;
        }

        public string DriverName => "Avalonia";
        
        public event Action<string> OnGamepadConnected
        {
            add    { }
            remove { }
        }

        public event Action<string> OnGamepadDisconnected
        {
            add    { }
            remove { }
        }

        public ReadOnlySpan<string> GamepadsIds => new[] {"0"};
        
        public IGamepad GetGamepad(string id)
        {
            return new AvaloniaMouse(this);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _widget.PointerMoved -= Parent_PointerMovedEvent;
            _widget.PointerPressed -= Parent_PointerPressEvent;
            _widget.PointerReleased -= Parent_PointerReleaseEvent;
            _widget.PointerWheelChanged -= Parent_ScrollEvent;

            _widget = null;
        }
    }
}