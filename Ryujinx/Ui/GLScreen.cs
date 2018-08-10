using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ryujinx.Graphics.Gal;
using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using Ryujinx.UI.Input;
using System;
using System.Threading;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Ryujinx
{
    public class GLScreen : GameWindow
    {
        private const int TouchScreenWidth  = 1280;
        private const int TouchScreenHeight = 720;

        private const int TargetFPS = 60;

        private Switch Ns;

        private IGalRenderer Renderer;

        private KeyboardState? Keyboard = null;

        private MouseState? Mouse = null;

        private Thread RenderThread;

        private bool ResizeEvent;

        private bool TitleEvent;

        private string NewTitle;

        public GLScreen(Switch Ns, IGalRenderer Renderer)
            : base(1280, 720,
            new GraphicsMode(), "Ryujinx", 0,
            DisplayDevice.Default, 3, 3,
            GraphicsContextFlags.ForwardCompatible)
        {
            this.Ns       = Ns;
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
                if (Ns.WaitFifo())
                {
                    Ns.ProcessFrame();
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

                    Ns.Hid.SetTouchPoints(CurrentPoint);
                }
            }

            if (!HasTouch)
            {
                Ns.Hid.SetTouchPoints();
            }

            if (HidEmulatedDevices.Devices.Handheld != -2)
            {
                switch (HidEmulatedDevices.Devices.Handheld)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_HANDHELD,
                            HidControllerLayouts.Handheld_Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Handheld < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Handheld + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_HANDHELD,
                            HidControllerLayouts.Handheld_Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Handheld],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Handheld],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Handheld]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player1 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player1)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_1,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player1 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player1 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_1,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player1],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player1],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player1]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player2 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player2)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_2,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player2 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player2 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_2,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player2],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player2],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player2]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player3 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player3)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_3,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player3 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player3 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_3,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player3],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player3],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player3]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player4 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player4)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_4,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player4 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player4 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_4,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player4],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player4],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player4]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player5 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player5)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_5,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player5 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player5 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_5,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player5],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player5],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player5]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player6 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player6)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_6,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player6 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player6 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_6,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player6],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player6],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player6]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player7 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player7)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_7,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player7 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player7 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_7,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player7],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player7],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player7]);
                        break;
                }
            }

            if (HidEmulatedDevices.Devices.Player8 != -2)
            {
                switch (HidEmulatedDevices.Devices.Player8)
                {
                    case -1:
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_8,
                            HidControllerLayouts.Joined,
                            CurrentButtonsKeyboard,
                            LeftJoystickKeyboard,
                            RightJoystickKeyboard);
                        break;
                    default:
                        if (HidEmulatedDevices.Devices.Player8 < 0)
                            throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.Player8 + ".");
                        Ns.Hid.SetJoyconButton(
                            HidControllerId.CONTROLLER_PLAYER_8,
                            HidControllerLayouts.Joined,
                            CurrentButtonsGamePad[HidEmulatedDevices.Devices.Player8],
                            LeftJoystickGamePad[HidEmulatedDevices.Devices.Player8],
                            RightJoystickGamePad[HidEmulatedDevices.Devices.Player8]);
                        break;
                }

                if (HidEmulatedDevices.Devices.PlayerUnknown != -2)
                {
                    switch (HidEmulatedDevices.Devices.PlayerUnknown)
                    {
                        case -1:
                            Ns.Hid.SetJoyconButton(
                                HidControllerId.CONTROLLER_UNKNOWN,
                                HidControllerLayouts.Joined,
                                CurrentButtonsKeyboard,
                                LeftJoystickKeyboard,
                                RightJoystickKeyboard);
                            break;
                        default:
                            if (HidEmulatedDevices.Devices.PlayerUnknown < 0)
                                throw new ArgumentException("Unknown Emulated Device Code: " + HidEmulatedDevices.Devices.PlayerUnknown + ".");
                            Ns.Hid.SetJoyconButton(
                                HidControllerId.CONTROLLER_UNKNOWN,
                                HidControllerLayouts.Joined,
                                CurrentButtonsGamePad[HidEmulatedDevices.Devices.PlayerUnknown],
                                LeftJoystickGamePad[HidEmulatedDevices.Devices.PlayerUnknown],
                                RightJoystickGamePad[HidEmulatedDevices.Devices.PlayerUnknown]);
                            break;
                    }
                }
            }

            /*Ns.Hid.SetJoyconButton(
                HidControllerId.CONTROLLER_PLAYER_1,
                HidControllerLayouts.Joined,
                CurrentButton,
                LeftJoystick,
                RightJoystick);*/
        }

        private new void RenderFrame()
        {
            Renderer.FrameBuffer.Render();

            Ns.Statistics.RecordSystemFrameTime();

            double HostFps = Ns.Statistics.GetSystemFrameRate();
            double GameFps = Ns.Statistics.GetGameFrameRate();

            NewTitle = $"Ryujinx | Host FPS: {HostFps:0.0} | Game FPS: {GameFps:0.0}";

            TitleEvent = true;

            SwapBuffers();

            Ns.Os.SignalVsync();
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