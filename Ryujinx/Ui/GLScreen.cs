using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ryujinx.Graphics.Gal;
using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using System;

namespace Ryujinx
{
    public class GLScreen : GameWindow
    {
        private const int TouchScreenWidth  = 1280;
        private const int TouchScreenHeight = 720;

        private const float TouchScreenRatioX = (float)TouchScreenWidth  / TouchScreenHeight;
        private const float TouchScreenRatioY = (float)TouchScreenHeight / TouchScreenWidth;

        private Switch Ns;

        private IGalRenderer Renderer;

        private KeyboardState? Keyboard = null;

        private MouseState? Mouse = null;

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

        protected override void OnLoad(EventArgs e)
        {
            VSync = VSyncMode.On;

            Renderer.FrameBuffer.SetWindowSize(Width, Height);
        }

        private ButtonState getGamePadButtonFromString(GamePadState gamePad, string str) //Please make this prettier if you can.
        {
            ButtonState result = gamePad.Buttons.A;

            switch (str)
            {
                case "A":
                    result = gamePad.Buttons.A;
                    break;
                case "B":
                    result = gamePad.Buttons.B;
                    break;
                case "X":
                    result = gamePad.Buttons.X;
                    break;
                case "Y":
                    result = gamePad.Buttons.Y;
                    break;
                case "LStick":
                    result = gamePad.Buttons.LeftStick;
                    break;
                case "RStick":
                    result = gamePad.Buttons.RightStick;
                    break;
                case "LShoulder":
                    result = gamePad.Buttons.LeftShoulder;
                    break;
                case "RShoulder":
                    result = gamePad.Buttons.RightShoulder;
                    break;
                case "DPadUp":
                    result = gamePad.DPad.Up;
                    break;
                case "DPadDown":
                    result = gamePad.DPad.Down;
                    break;
                case "DPadLeft":
                    result = gamePad.DPad.Left;
                    break;
                case "DPadRight":
                    result = gamePad.DPad.Right;
                    break;
                case "Start":
                    result = gamePad.Buttons.Start;
                    break;
                case "Back":
                    result = gamePad.Buttons.Back;
                    break;
                default:
                    Console.Error.WriteLine("Invalid Button Mapping \"" + str + "\"!  Defaulting to Button A.");
                    break;
            }

            return result;
        }

        private float getGamePadTriggerFromString(GamePadState gamePad, string str)
        {
            float result = 0;

            switch (str)
            {
                case "LTrigger":
                    result = gamePad.Triggers.Left;
                    break;
                case "RTrigger":
                    result = gamePad.Triggers.Right;
                    break;
                default:
                    Console.Error.WriteLine("Invalid Trigger Mapping \"" + str + "\"!  Defaulting to 0.");
                    break;
            }

            return result;
        }

        private Vector2 getJoystickAxisFromString(GamePadState gamePad, string str)
        {
            Vector2 result = new Vector2(0, 0);

            switch (str)
            {
                case "LJoystick":
                    result = gamePad.ThumbSticks.Left;
                    break;
                case "RJoystick":
                    result = gamePad.ThumbSticks.Right;
                    break;
                default:
                    Console.Error.WriteLine("Invalid Joystick Axis \"" + str + "\"!  Defaulting the Vector2 to 0, 0.");
                    break;
            }

            return result;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HidControllerButtons CurrentButton = 0;
            HidJoystickPosition LeftJoystick;
            HidJoystickPosition RightJoystick;

            int LeftJoystickDX = 0;
            int LeftJoystickDY = 0;
            int RightJoystickDX = 0;
            int RightJoystickDY = 0;
            float deadzone = Config.GamePad_Deadzone;

            //Keyboard Input
            if (Keyboard.HasValue)
            {
                KeyboardState Keyboard = this.Keyboard.Value;

                if (Keyboard[Key.Escape]) this.Exit();

                //LeftJoystick
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickUp])    LeftJoystickDY = short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickDown])  LeftJoystickDY = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickLeft])  LeftJoystickDX = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickRight]) LeftJoystickDX = short.MaxValue;

                //LeftButtons
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickButton]) CurrentButton |= HidControllerButtons.KEY_LSTICK;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadUp])      CurrentButton |= HidControllerButtons.KEY_DUP;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadDown])    CurrentButton |= HidControllerButtons.KEY_DDOWN;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadLeft])    CurrentButton |= HidControllerButtons.KEY_DLEFT;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadRight])   CurrentButton |= HidControllerButtons.KEY_DRIGHT;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.ButtonMinus]) CurrentButton |= HidControllerButtons.KEY_MINUS;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.ButtonL])     CurrentButton |= HidControllerButtons.KEY_L;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.ButtonZL])    CurrentButton |= HidControllerButtons.KEY_ZL;

                //RightJoystick
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickUp])    RightJoystickDY = short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickDown])  RightJoystickDY = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickLeft])  RightJoystickDX = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickRight]) RightJoystickDX = short.MaxValue;

                //RightButtons
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickButton]) CurrentButton |= HidControllerButtons.KEY_RSTICK;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonA])     CurrentButton |= HidControllerButtons.KEY_A;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonB])     CurrentButton |= HidControllerButtons.KEY_B;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonX])     CurrentButton |= HidControllerButtons.KEY_X;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonY])     CurrentButton |= HidControllerButtons.KEY_Y;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonPlus])  CurrentButton |= HidControllerButtons.KEY_PLUS;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonR])     CurrentButton |= HidControllerButtons.KEY_R;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonZR])    CurrentButton |= HidControllerButtons.KEY_ZR;
            }

            //Controller Input
            if (Config.GamePad_Enable)
            {
                GamePadState gamePad = GamePad.GetState(Config.GamePad_Index);

                //RightButtons
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadButton_A)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_A;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadButton_B)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_B;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadButton_X)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_X;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadButton_Y)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_Y;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadStick_Button)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_RSTICK;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadButton_Plus)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_PLUS;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadButton_R)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_R;
                if (getGamePadTriggerFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadTrigger_ZR)
                                    >= 0.5) CurrentButton |= HidControllerButtons.KEY_ZR;

                //LeftButtons
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadDPad_Up)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_DUP;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadDPad_Down)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_DDOWN;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadDPad_Left)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_DLEFT;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadDPad_Right)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_DRIGHT;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadStick_Button)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_LSTICK;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadButton_Minus)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_MINUS;
                if (getGamePadButtonFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadButton_L)
                    == ButtonState.Pressed) CurrentButton |= HidControllerButtons.KEY_L;
                if (getGamePadTriggerFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadTrigger_ZL)
                                    >= 0.5) CurrentButton |= HidControllerButtons.KEY_ZL;

                //RightJoystick
                if (getJoystickAxisFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadJoystick_R).X >= deadzone
                 || getJoystickAxisFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadJoystick_R).X <= -deadzone)
                    RightJoystickDY = (int)(-getJoystickAxisFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadJoystick_R).X * short.MaxValue);

                if (getJoystickAxisFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadJoystick_R).Y >= deadzone
                 || getJoystickAxisFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadJoystick_R).Y <= -deadzone)
                    RightJoystickDX = (int)(-getJoystickAxisFromString(gamePad, Config.Controls_Right_FakeJoycon_GamePadJoystick_R).Y * short.MaxValue);

                //LeftJoystick
                if (getJoystickAxisFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadJoystick_L).X >= deadzone
                 || getJoystickAxisFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadJoystick_L).X <= -deadzone)
                    LeftJoystickDX = (int)(getJoystickAxisFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadJoystick_L).X * short.MaxValue);

                if (getJoystickAxisFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadJoystick_L).Y >= deadzone
                 || getJoystickAxisFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadJoystick_L).Y <= -deadzone)
                    LeftJoystickDY = (int)(getJoystickAxisFromString(gamePad, Config.Controls_Left_FakeJoycon_GamePadJoystick_L).Y * short.MaxValue);
            }

            LeftJoystick = new HidJoystickPosition
            {
                DX = LeftJoystickDX,
                DY = LeftJoystickDY
            };

            RightJoystick = new HidJoystickPosition
            {
                DX = RightJoystickDX,
                DY = RightJoystickDY
            };

            bool HasTouch = false;

            //Get screen touch position from left mouse click
            //OpenTK always captures mouse events, even if out of focus, so check if window is focused.
            if (Focused && Mouse?.LeftButton == ButtonState.Pressed)
            {
                MouseState Mouse = this.Mouse.Value;

                int ScrnWidth  = Width;
                int ScrnHeight = Height;

                if (Width > Height * TouchScreenRatioX)
                {
                    ScrnWidth = (int)(Height * TouchScreenRatioX);
                }
                else
                {
                    ScrnHeight = (int)(Width * TouchScreenRatioY);
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

                    int MX = (int)(((float)ScrnMouseX / ScrnWidth)  * TouchScreenWidth);
                    int MY = (int)(((float)ScrnMouseY / ScrnHeight) * TouchScreenHeight);

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

            Ns.Hid.SetJoyconButton(
                HidControllerId.CONTROLLER_HANDHELD,
                HidControllerLayouts.Handheld_Joined,
                CurrentButton,
                LeftJoystick,
                RightJoystick);

            Ns.Hid.SetJoyconButton(
                HidControllerId.CONTROLLER_HANDHELD,
                HidControllerLayouts.Main,
                CurrentButton,
                LeftJoystick,
                RightJoystick);

            Ns.ProcessFrame();

            Renderer.RunActions();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            Renderer.FrameBuffer.Render();

            Ns.Statistics.RecordSystemFrameTime();

            double HostFps = Ns.Statistics.GetSystemFrameRate();
            double GameFps = Ns.Statistics.GetGameFrameRate();

            Title = $"Ryujinx | Host FPS: {HostFps:0.0} | Game FPS: {GameFps:0.0}";

            SwapBuffers();

            Ns.Os.SignalVsync();
        }

        protected override void OnResize(EventArgs e)
        {
            Renderer.FrameBuffer.SetWindowSize(Width, Height);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
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