using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ryujinx.Graphics.Gal;
using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using System;
using System.Collections.Generic;
using System.Threading;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Ryujinx
{
    public class GLScreen : GameWindow
    {
        private const int TouchScreenWidth  = 1280;
        private const int TouchScreenHeight = 720;

        private const int TargetFPS = 60;

        private Switch Device;

        private IGalRenderer Renderer;

        private KeyboardState? Keyboard = null;

        private MouseState? Mouse = null;

        private Thread RenderThread;

        private bool ResizeEvent;

        private bool TitleEvent;

        private string NewTitle;

        public GLScreen(Switch Device, IGalRenderer Renderer)
            : base(1280, 720,
            new GraphicsMode(), "Ryujinx", 0,
            DisplayDevice.Default, 3, 3,
            GraphicsContextFlags.ForwardCompatible)
        {
            this.Device   = Device;
            this.Renderer = Renderer;

            Location = new Point(
                (DisplayDevice.Default.Width  / 2) - (Width  / 2),
                (DisplayDevice.Default.Height / 2) - (Height / 2));
        }

        private void RenderLoop()
        {
            MakeCurrent();

            Stopwatch Chrono = new Stopwatch();

            Chrono.Start();

            long TicksPerFrame = Stopwatch.Frequency / TargetFPS;

            long Ticks = 0;

            while (Exists && !IsExiting)
            {
                if (Device.WaitFifo())
                {
                    Device.ProcessFrame();
                }

                Renderer.RunActions();

                if (ResizeEvent)
                {
                    ResizeEvent = false;

                    Renderer.FrameBuffer.SetWindowSize(Width, Height);
                }

                Ticks += Chrono.ElapsedTicks;

                Chrono.Restart();

                if (Ticks >= TicksPerFrame)
                {
                    RenderFrame();

                    //Queue max. 1 vsync
                    Ticks = Math.Min(Ticks - TicksPerFrame, TicksPerFrame);
                }
            }
        }

        public void MainLoop()
        {
            VSync = VSyncMode.Off;

            Visible = true;

            Renderer.FrameBuffer.SetWindowSize(Width, Height);

            Context.MakeCurrent(null);

            //OpenTK doesn't like sleeps in its thread, to avoid this a renderer thread is created
            RenderThread = new Thread(RenderLoop);

            RenderThread.Start();

            while (Exists && !IsExiting)
            {
                ProcessEvents();

                if (!IsExiting)
                {
                    UpdateFrame();

                    if (TitleEvent)
                    {
                        TitleEvent = false;

                        Title = NewTitle;
                    }
                }

                //Polling becomes expensive if it's not slept
                Thread.Sleep(1);
            }
        }

        private new void UpdateFrame()
        {
            HidControllerButtons   CurrentButtonsKeyboard  = new HidControllerButtons();
            HidControllerButtons[] CurrentButtonsGamePad   = new HidControllerButtons[Config.JoyConControllers.Length];
            HidJoystickPosition    LeftJoystickKeyboard;
            HidJoystickPosition    RightJoystickKeyboard;
            HidJoystickPosition[]  LeftJoystickGamePad  = new HidJoystickPosition[Config.JoyConControllers.Length];
            HidJoystickPosition[]  RightJoystickGamePad = new HidJoystickPosition[Config.JoyConControllers.Length];

            int LeftJoystickDXKeyboard  = 0;
            int LeftJoystickDYKeyboard  = 0;
            int RightJoystickDXKeyboard = 0;
            int RightJoystickDYKeyboard = 0;

            int[] LeftJoystickDXGamePad  = new int[Config.JoyConControllers.Length];
            int[] LeftJoystickDYGamePad  = new int[Config.JoyConControllers.Length];
            int[] RightJoystickDXGamePad = new int[Config.JoyConControllers.Length];
            int[] RightJoystickDYGamePad = new int[Config.JoyConControllers.Length];

            //Keyboard Input
            if (Keyboard.HasValue)
            {
                KeyboardState Keyboard = this.Keyboard.Value;

                CurrentButtonsKeyboard = Config.JoyConKeyboard.GetButtons(Keyboard);

                (LeftJoystickDXKeyboard, LeftJoystickDYKeyboard)   = Config.JoyConKeyboard.GetLeftStick(Keyboard);
                (RightJoystickDXKeyboard, RightJoystickDYKeyboard) = Config.JoyConKeyboard.GetRightStick(Keyboard);
            }

            //Controller Input
            if (Config.GamePadEnable)
            {
                for (int i = 0; i < CurrentButtonsGamePad.Length; ++i)
                {
                    CurrentButtonsGamePad[i] |= Config.JoyConControllers[i].GetButtons();

                    (LeftJoystickDXGamePad[i], LeftJoystickDYGamePad[i])   = Config.JoyConControllers[i].GetLeftStick();
                    (RightJoystickDXGamePad[i], RightJoystickDYGamePad[i]) = Config.JoyConControllers[i].GetRightStick();

                    LeftJoystickGamePad[i] = new HidJoystickPosition
                    {
                        DX = LeftJoystickDXGamePad[i],
                        DY = LeftJoystickDYGamePad[i]
                    };

                    RightJoystickGamePad[i] = new HidJoystickPosition
                    {
                        DX = RightJoystickDXGamePad[i],
                        DY = RightJoystickDYGamePad[i]
                    };
                }
            }
            
            LeftJoystickKeyboard = new HidJoystickPosition
            {
                DX = LeftJoystickDXKeyboard,
                DY = LeftJoystickDYKeyboard
            };

            RightJoystickKeyboard = new HidJoystickPosition
            {
                DX = RightJoystickDXKeyboard,
                DY = RightJoystickDYKeyboard
            };

            bool HasTouch = false;

            //Get screen touch position from left mouse click
            //OpenTK always captures mouse events, even if out of focus, so check if window is focused.
            if (Focused && Mouse?.LeftButton == ButtonState.Pressed)
            {
                MouseState Mouse = this.Mouse.Value;

                int ScrnWidth  = Width;
                int ScrnHeight = Height;

                if (Width > (Height * TouchScreenWidth) / TouchScreenHeight)
                {
                    ScrnWidth = (Height * TouchScreenWidth) / TouchScreenHeight;
                }
                else
                {
                    ScrnHeight = (Width * TouchScreenHeight) / TouchScreenWidth;
                }

                int StartX = (Width  - ScrnWidth)  >> 1;
                int StartY = (Height - ScrnHeight) >> 1;

                int EndX = StartX + ScrnWidth;
                int EndY = StartY + ScrnHeight;

                if (Mouse.X >= StartX &&
                    Mouse.Y >= StartY &&
                    Mouse.X <  EndX   &&
                    Mouse.Y <  EndY)
                {
                    int ScrnMouseX = Mouse.X - StartX;
                    int ScrnMouseY = Mouse.Y - StartY;

                    int MX = (ScrnMouseX * TouchScreenWidth)  / ScrnWidth;
                    int MY = (ScrnMouseY * TouchScreenHeight) / ScrnHeight;

                    HidTouchPoint CurrentPoint = new HidTouchPoint
                    {
                        X = MX,
                        Y = MY,

                        //Placeholder values till more data is acquired
                        DiameterX = 10,
                        DiameterY = 10,
                        Angle     = 90
                    };

                    HasTouch = true;

                    Device.Hid.SetTouchPoints(CurrentPoint);
                }
            }

            if (!HasTouch)
            {
                Device.Hid.SetTouchPoints();
            }

            foreach (KeyValuePair<HidControllerId, HidEmulatedDevices.HostDevice> entry in HidEmulatedDevices.Devices)
            {
                if (entry.Value != HidEmulatedDevices.HostDevice.None)
                {
                    Device.Hid.SetJoyconButton(
                        entry.Key,
                        (entry.Key == HidControllerId.CONTROLLER_HANDHELD) ? HidControllerLayouts.Handheld_Joined : HidControllerLayouts.Joined,
                        (entry.Value == HidEmulatedDevices.HostDevice.Keyboard) ? CurrentButtonsKeyboard : CurrentButtonsGamePad[GetGamePadIndexFromHostDevice(entry.Value)],
                        (entry.Value == HidEmulatedDevices.HostDevice.Keyboard) ? LeftJoystickKeyboard   : LeftJoystickGamePad  [GetGamePadIndexFromHostDevice(entry.Value)],
                        (entry.Value == HidEmulatedDevices.HostDevice.Keyboard) ? RightJoystickKeyboard  : RightJoystickGamePad [GetGamePadIndexFromHostDevice(entry.Value)]);
                    Device.Hid.SetJoyconButton(
                        entry.Key,
                        HidControllerLayouts.Main,
                        (entry.Value == HidEmulatedDevices.HostDevice.Keyboard) ? CurrentButtonsKeyboard : CurrentButtonsGamePad[GetGamePadIndexFromHostDevice(entry.Value)],
                        (entry.Value == HidEmulatedDevices.HostDevice.Keyboard) ? LeftJoystickKeyboard   : LeftJoystickGamePad  [GetGamePadIndexFromHostDevice(entry.Value)],
                        (entry.Value == HidEmulatedDevices.HostDevice.Keyboard) ? RightJoystickKeyboard  : RightJoystickGamePad [GetGamePadIndexFromHostDevice(entry.Value)]);
                }
            }
        }
        
        private int GetGamePadIndexFromHostDevice(HidEmulatedDevices.HostDevice hostDevice)
        {
            switch (hostDevice)
            {
                case HidEmulatedDevices.HostDevice.GamePad_0: return 0;
                case HidEmulatedDevices.HostDevice.GamePad_1: return 1;
                case HidEmulatedDevices.HostDevice.GamePad_2: return 2;
                case HidEmulatedDevices.HostDevice.GamePad_3: return 3;
                case HidEmulatedDevices.HostDevice.GamePad_4: return 4;
                case HidEmulatedDevices.HostDevice.GamePad_5: return 5;
                case HidEmulatedDevices.HostDevice.GamePad_6: return 6;
                case HidEmulatedDevices.HostDevice.GamePad_7: return 7;
                case HidEmulatedDevices.HostDevice.GamePad_8: return 8;
            }

            return -1;
        }

        private new void RenderFrame()
        {
            Renderer.FrameBuffer.Render();

            Device.Statistics.RecordSystemFrameTime();

            double HostFps = Device.Statistics.GetSystemFrameRate();
            double GameFps = Device.Statistics.GetGameFrameRate();

            NewTitle = $"Ryujinx | Host FPS: {HostFps:0.0} | Game FPS: {GameFps:0.0}";

            TitleEvent = true;

            SwapBuffers();

            Device.System.SignalVsync();
        }

        protected override void OnUnload(EventArgs e)
        {
            RenderThread.Join();

            base.OnUnload(e);
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeEvent = true;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            bool ToggleFullscreen = e.Key == Key.F11 ||
                (e.Modifiers.HasFlag(KeyModifiers.Alt) && e.Key == Key.Enter);

            if (WindowState == WindowState.Fullscreen)
            {
                if (e.Key == Key.Escape || ToggleFullscreen)
                {
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                if (e.Key == Key.Escape)
                {
                    Exit();
                }

                if (ToggleFullscreen)
                {
                    WindowState = WindowState.Fullscreen;
                }
            }

            Keyboard = e.Keyboard;
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            Keyboard = e.Keyboard;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Mouse = e.Mouse;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            Mouse = e.Mouse;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            Mouse = e.Mouse;
        }
    }
}