using OpenTK;
using OpenTK.Input;
using Ryujinx.HLE.Input;
using System;

namespace Ryujinx.UI.Input
{
    public enum ControllerInputId
    {
        Invalid,

        LStick,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        Back,
        LShoulder,

        RStick,
        A,
        B,
        X,
        Y,
        Start,
        RShoulder,

        LTrigger,
        RTrigger,

        LJoystick,
        RJoystick
    }

    public struct JoyConControllerLeft
    {
        public ControllerInputId Stick;
        public ControllerInputId StickButton;
        public ControllerInputId DPadUp;
        public ControllerInputId DPadDown;
        public ControllerInputId DPadLeft;
        public ControllerInputId DPadRight;
        public ControllerInputId ButtonMinus;
        public ControllerInputId ButtonL;
        public ControllerInputId ButtonZl;
    }

    public struct JoyConControllerRight
    {
        public ControllerInputId Stick;
        public ControllerInputId StickButton;
        public ControllerInputId ButtonA;
        public ControllerInputId ButtonB;
        public ControllerInputId ButtonX;
        public ControllerInputId ButtonY;
        public ControllerInputId ButtonPlus;
        public ControllerInputId ButtonR;
        public ControllerInputId ButtonZr;
    }

    public class JoyConController
    {
        public bool  Enabled          { private set; get; }
        public int   Index            { private set; get; }
        public float Deadzone         { private set; get; }
        public float TriggerThreshold { private set; get; }

        public JoyConControllerLeft  Left  { private set; get; }
        public JoyConControllerRight Right { private set; get; }

        public JoyConController(
            bool                  enabled,
            int                   index,
            float                 deadzone,
            float                 triggerThreshold,
            JoyConControllerLeft  left,
            JoyConControllerRight right)
        {
            this.Enabled          = enabled;
            this.Index            = index;
            this.Deadzone         = deadzone;
            this.TriggerThreshold = triggerThreshold;
            this.Left             = left;
            this.Right            = right;

            //Unmapped controllers are problematic, skip them
            if (GamePad.GetName(index) == "Unmapped Controller")
            {
                this.Enabled = false;
            }
        }

        public HidControllerButtons GetButtons()
        {
            if (!Enabled)
            {
                return 0;
            }

            GamePadState gpState = GamePad.GetState(Index);

            HidControllerButtons buttons = 0;

            if (IsPressed(gpState, Left.DPadUp))       buttons |= HidControllerButtons.KeyDup;
            if (IsPressed(gpState, Left.DPadDown))     buttons |= HidControllerButtons.KeyDdown;
            if (IsPressed(gpState, Left.DPadLeft))     buttons |= HidControllerButtons.KeyDleft;
            if (IsPressed(gpState, Left.DPadRight))    buttons |= HidControllerButtons.KeyDright;
            if (IsPressed(gpState, Left.StickButton))  buttons |= HidControllerButtons.KeyLstick;
            if (IsPressed(gpState, Left.ButtonMinus))  buttons |= HidControllerButtons.KeyMinus;
            if (IsPressed(gpState, Left.ButtonL))      buttons |= HidControllerButtons.KeyL;
            if (IsPressed(gpState, Left.ButtonZl))     buttons |= HidControllerButtons.KeyZl;

            if (IsPressed(gpState, Right.ButtonA))     buttons |= HidControllerButtons.KeyA;
            if (IsPressed(gpState, Right.ButtonB))     buttons |= HidControllerButtons.KeyB;
            if (IsPressed(gpState, Right.ButtonX))     buttons |= HidControllerButtons.KeyX;
            if (IsPressed(gpState, Right.ButtonY))     buttons |= HidControllerButtons.KeyY;
            if (IsPressed(gpState, Right.StickButton)) buttons |= HidControllerButtons.KeyRstick;
            if (IsPressed(gpState, Right.ButtonPlus))  buttons |= HidControllerButtons.KeyPlus;
            if (IsPressed(gpState, Right.ButtonR))     buttons |= HidControllerButtons.KeyR;
            if (IsPressed(gpState, Right.ButtonZr))    buttons |= HidControllerButtons.KeyZr;

            return buttons;
        }

        public (short, short) GetLeftStick()
        {
            if (!Enabled)
            {
                return (0, 0);
            }

            return GetStick(Left.Stick);
        }

        public (short, short) GetRightStick()
        {
            if (!Enabled)
            {
                return (0, 0);
            }

            return GetStick(Right.Stick);
        }

        private (short, short) GetStick(ControllerInputId joystick)
        {
            GamePadState gpState = GamePad.GetState(Index);

            switch (joystick)
            {
                case ControllerInputId.LJoystick:
                    return ApplyDeadzone(gpState.ThumbSticks.Left);

                case ControllerInputId.RJoystick:
                    return ApplyDeadzone(gpState.ThumbSticks.Right);

                default:
                    return (0, 0);
            }
        }

        private (short, short) ApplyDeadzone(Vector2 axis)
        {
            return (ClampAxis(MathF.Abs(axis.X) > Deadzone ? axis.X : 0f),
                    ClampAxis(MathF.Abs(axis.Y) > Deadzone ? axis.Y : 0f));
        }

        private static short ClampAxis(float value)
        {
            if (value <= -short.MaxValue)
            {
                return -short.MaxValue;
            }
            else
            {
                return (short)(value * short.MaxValue);
            }
        }

        private bool IsPressed(GamePadState gpState, ControllerInputId button)
        {
            switch (button)
            {
                case ControllerInputId.A:         return gpState.Buttons.A             == ButtonState.Pressed;
                case ControllerInputId.B:         return gpState.Buttons.B             == ButtonState.Pressed;
                case ControllerInputId.X:         return gpState.Buttons.X             == ButtonState.Pressed;
                case ControllerInputId.Y:         return gpState.Buttons.Y             == ButtonState.Pressed;
                case ControllerInputId.LStick:    return gpState.Buttons.LeftStick     == ButtonState.Pressed;
                case ControllerInputId.RStick:    return gpState.Buttons.RightStick    == ButtonState.Pressed;
                case ControllerInputId.LShoulder: return gpState.Buttons.LeftShoulder  == ButtonState.Pressed;
                case ControllerInputId.RShoulder: return gpState.Buttons.RightShoulder == ButtonState.Pressed;
                case ControllerInputId.DPadUp:    return gpState.DPad.Up               == ButtonState.Pressed;
                case ControllerInputId.DPadDown:  return gpState.DPad.Down             == ButtonState.Pressed;
                case ControllerInputId.DPadLeft:  return gpState.DPad.Left             == ButtonState.Pressed;
                case ControllerInputId.DPadRight: return gpState.DPad.Right            == ButtonState.Pressed;
                case ControllerInputId.Start:     return gpState.Buttons.Start         == ButtonState.Pressed;
                case ControllerInputId.Back:      return gpState.Buttons.Back          == ButtonState.Pressed;

                case ControllerInputId.LTrigger: return gpState.Triggers.Left  >= TriggerThreshold;
                case ControllerInputId.RTrigger: return gpState.Triggers.Right >= TriggerThreshold;

                //Using thumbsticks as buttons is not common, but it would be nice not to ignore them
                case ControllerInputId.LJoystick:
                    return gpState.ThumbSticks.Left.X >= Deadzone ||
                           gpState.ThumbSticks.Left.Y >= Deadzone;

                case ControllerInputId.RJoystick:
                    return gpState.ThumbSticks.Right.X >= Deadzone ||
                           gpState.ThumbSticks.Right.Y >= Deadzone;

                default:
                    return false;
            }
        }
    }
}
