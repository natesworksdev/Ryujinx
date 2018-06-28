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
        
        private bool IsGamePadButtonPressedFromString(GamePadState gamePad, string str)
        {
            if (str == "LTrigger" || str == "RTrigger")
            {
                return GetGamePadTriggerFromString(gamePad, str) >= Config.GamePad_Trigger_Threshold;
            }
            else
            {
                return (GetGamePadButtonFromString(gamePad, str) == ButtonState.Pressed);
            }
        }

        private ButtonState GetGamePadButtonFromString(GamePadState gamePad, string str) //Please make this prettier if you can.
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

        private float GetGamePadTriggerFromString(GamePadState gamePad, string str)
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

        private Vector2 GetJoystickAxisFromString(GamePadState gamePad, string str)
        {
            Vector2 result = new Vector2(0, 0);

            switch (str)
            {
                case "LJoystick":
                    result = gamePad.ThumbSticks.Left;
                    break;
                case "RJoystick":
                    result = new Vector2(-gamePad.ThumbSticks.Right.Y, -gamePad.ThumbSticks.Right.X);
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
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.StickUp])    LeftJoystickDY = short.MaxValue;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.StickDown])  LeftJoystickDY = -short.MaxValue;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.StickLeft])  LeftJoystickDX = -short.MaxValue;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.StickRight]) LeftJoystickDX = short.MaxValue;

                //LeftButtons
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.StickButton]) CurrentButton |= HidControllerButtons.KEY_LSTICK;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.DPadUp])      CurrentButton |= HidControllerButtons.KEY_DUP;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.DPadDown])    CurrentButton |= HidControllerButtons.KEY_DDOWN;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.DPadLeft])    CurrentButton |= HidControllerButtons.KEY_DLEFT;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.DPadRight])   CurrentButton |= HidControllerButtons.KEY_DRIGHT;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.ButtonMinus]) CurrentButton |= HidControllerButtons.KEY_MINUS;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.ButtonL])     CurrentButton |= HidControllerButtons.KEY_L;
                if (Keyboard[(Key)Config.JoyConKeyboard.Left.ButtonZL])    CurrentButton |= HidControllerButtons.KEY_ZL;

                //RightJoystick
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.StickUp])    RightJoystickDY = short.MaxValue;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.StickDown])  RightJoystickDY = -short.MaxValue;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.StickLeft])  RightJoystickDX = -short.MaxValue;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.StickRight]) RightJoystickDX = short.MaxValue;

                //RightButtons
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.StickButton]) CurrentButton |= HidControllerButtons.KEY_RSTICK;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonA])     CurrentButton |= HidControllerButtons.KEY_A;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonB])     CurrentButton |= HidControllerButtons.KEY_B;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonX])     CurrentButton |= HidControllerButtons.KEY_X;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonY])     CurrentButton |= HidControllerButtons.KEY_Y;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonPlus])  CurrentButton |= HidControllerButtons.KEY_PLUS;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonR])     CurrentButton |= HidControllerButtons.KEY_R;
                if (Keyboard[(Key)Config.JoyConKeyboard.Right.ButtonZR])    CurrentButton |= HidControllerButtons.KEY_ZR;
            }

            //Controller Input
            if (Config.GamePad_Enable)
            {
                GamePadState gamePad = GamePad.GetState(Config.GamePad_Index);

                //LeftButtons
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.DPadUp))       CurrentButton |= HidControllerButtons.KEY_DUP;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.DPadDown))     CurrentButton |= HidControllerButtons.KEY_DDOWN;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.DPadLeft))     CurrentButton |= HidControllerButtons.KEY_DLEFT;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.DPadRight))    CurrentButton |= HidControllerButtons.KEY_DRIGHT;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.StickButton))  CurrentButton |= HidControllerButtons.KEY_LSTICK;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.ButtonMinus))  CurrentButton |= HidControllerButtons.KEY_MINUS;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.ButtonL))      CurrentButton |= HidControllerButtons.KEY_L;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Left.ButtonZL))     CurrentButton |= HidControllerButtons.KEY_ZL;

                //RightButtons
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonA))     CurrentButton |= HidControllerButtons.KEY_A;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonB))     CurrentButton |= HidControllerButtons.KEY_B;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonX))     CurrentButton |= HidControllerButtons.KEY_X;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonY))     CurrentButton |= HidControllerButtons.KEY_Y;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.StickButton)) CurrentButton |= HidControllerButtons.KEY_RSTICK;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonPlus))  CurrentButton |= HidControllerButtons.KEY_PLUS;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonR))     CurrentButton |= HidControllerButtons.KEY_R;
                if (IsGamePadButtonPressedFromString(gamePad, Config.JoyConController.Right.ButtonZR))    CurrentButton |= HidControllerButtons.KEY_ZR;

                //LeftJoystick
                if (GetJoystickAxisFromString(gamePad, Config.JoyConController.Left.Stick).X >= deadzone
                 || GetJoystickAxisFromString(gamePad, Config.JoyConController.Left.Stick).X <= -deadzone)
                    LeftJoystickDX = (int)(GetJoystickAxisFromString(gamePad, Config.JoyConController.Left.Stick).X * short.MaxValue);

                if (GetJoystickAxisFromString(gamePad, Config.JoyConController.Left.Stick).Y >= deadzone
                 || GetJoystickAxisFromString(gamePad, Config.JoyConController.Left.Stick).Y <= -deadzone)
                    LeftJoystickDY = (int)(GetJoystickAxisFromString(gamePad, Config.JoyConController.Left.Stick).Y * short.MaxValue);

                //RightJoystick
                if (GetJoystickAxisFromString(gamePad, Config.JoyConController.Right.Stick).X >= deadzone
                 || GetJoystickAxisFromString(gamePad, Config.JoyConController.Right.Stick).X <= -deadzone)
                    RightJoystickDX = (int)(GetJoystickAxisFromString(gamePad, Config.JoyConController.Right.Stick).X * short.MaxValue);

                if (GetJoystickAxisFromString(gamePad, Config.JoyConController.Right.Stick).Y >= deadzone
                 || GetJoystickAxisFromString(gamePad, Config.JoyConController.Right.Stick).Y <= -deadzone)
                    RightJoystickDY = (int)(GetJoystickAxisFromString(gamePad, Config.JoyConController.Right.Stick).Y * short.MaxValue);
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