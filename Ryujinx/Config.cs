using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using Ryujinx.HLE.Logging;
using Ryujinx.UI.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ryujinx
{
    public static class Config
    {
        public static JoyConKeyboard     JoyConKeyboard    { get; private set; }
        public static JoyConController[] JoyConControllers { get; private set; }
        public static bool               GamePadEnable     { get; private set; }

        public static void Read(Switch Device)
        {
            string IniFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string IniPath = Path.Combine(IniFolder, "Ryujinx.conf");

            IniParser Parser = new IniParser(IniPath);

            GraphicsConfig.ShadersDumpPath = Parser.Value("Graphics_Shaders_Dump_Path");

            Device.Log.SetEnable(LogLevel.Debug,   Convert.ToBoolean(Parser.Value("Logging_Enable_Debug")));
            Device.Log.SetEnable(LogLevel.Stub,    Convert.ToBoolean(Parser.Value("Logging_Enable_Stub")));
            Device.Log.SetEnable(LogLevel.Info,    Convert.ToBoolean(Parser.Value("Logging_Enable_Info")));
            Device.Log.SetEnable(LogLevel.Warning, Convert.ToBoolean(Parser.Value("Logging_Enable_Warn")));
            Device.Log.SetEnable(LogLevel.Error,   Convert.ToBoolean(Parser.Value("Logging_Enable_Error")));

            Device.System.State.DockedMode = Convert.ToBoolean(Parser.Value("Docked_Mode"));

            string[] FilteredLogClasses = Parser.Value("Logging_Filtered_Classes").Split(',', StringSplitOptions.RemoveEmptyEntries);

            GamePadEnable = Boolean.Parse(Parser.Value("GamePad_Enable"));

            EmulatedDevices.Devices = new Dictionary<HidControllerId, HidHostDevice>
            {
                //Device Mappings
                { HidControllerId.CONTROLLER_HANDHELD, ToHostDevice(Parser.Value("Handheld_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_1, ToHostDevice(Parser.Value("Player1_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_2, ToHostDevice(Parser.Value("Player2_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_3, ToHostDevice(Parser.Value("Player3_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_4, ToHostDevice(Parser.Value("Player4_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_5, ToHostDevice(Parser.Value("Player5_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_6, ToHostDevice(Parser.Value("Player6_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_7, ToHostDevice(Parser.Value("Player7_Device")) },
                { HidControllerId.CONTROLLER_PLAYER_8, ToHostDevice(Parser.Value("Player8_Device")) },
                { HidControllerId.CONTROLLER_UNKNOWN,  ToHostDevice(Parser.Value("PlayerUnknown_Device")) }
            };

            Hid.Devices = EmulatedDevices.Devices;

            //When the classes are specified on the list, we only
            //enable the classes that are on the list.
            //So, first disable everything, then enable
            //the classes that the user added to the list.
            if (FilteredLogClasses.Length > 0)
            {
                foreach (LogClass Class in Enum.GetValues(typeof(LogClass)))
                {
                    Device.Log.SetEnable(Class, false);
                }
            }

            foreach (string LogClass in FilteredLogClasses)
            {
                if (!string.IsNullOrEmpty(LogClass.Trim()))
                {
                    foreach (LogClass Class in Enum.GetValues(typeof(LogClass)))
                    {
                        if (Class.ToString().ToLower().Contains(LogClass.Trim().ToLower()))
                        {
                            Device.Log.SetEnable(Class, true);
                        }
                    }
                }
            }

            JoyConKeyboardLeft LeftKeyboardJoycon = new JoyConKeyboardLeft
            {
                StickUp     = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Stick_Up")),
                StickDown   = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Stick_Down")),
                StickLeft   = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Stick_Left")),
                StickRight  = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Stick_Right")),
                StickButton = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Stick_Button")),
                DPadUp      = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_DPad_Up")),
                DPadDown    = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_DPad_Down")),
                DPadLeft    = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_DPad_Left")),
                DPadRight   = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_DPad_Right")),
                ButtonMinus = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Button_Minus")),
                ButtonL     = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Button_L")),
                ButtonZL    = Convert.ToInt16(Parser.Value("Controls_Left_JoyConKeyboard_Button_ZL"))
            };

            JoyConKeyboardRight RightKeyboardJoycon = new JoyConKeyboardRight
            {
                StickUp     = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Stick_Up")),
                StickDown   = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Stick_Down")),
                StickLeft   = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Stick_Left")),
                StickRight  = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Stick_Right")),
                StickButton = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Stick_Button")),
                ButtonA     = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_A")),
                ButtonB     = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_B")),
                ButtonX     = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_X")),
                ButtonY     = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_Y")),
                ButtonPlus  = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_Plus")),
                ButtonR     = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_R")),
                ButtonZR    = Convert.ToInt16(Parser.Value("Controls_Right_JoyConKeyboard_Button_ZR"))
            };

            JoyConKeyboard = new JoyConKeyboard(LeftKeyboardJoycon, RightKeyboardJoycon);

            List<JoyConController> JoyConControllerList = new List<JoyConController>();

            //Populate the Controller List
            for (int i = 0; i < 9; ++i)
            {
                if (Parser.Value("GamePad_Index_" + i) == null) break;

                JoyConControllerLeft LeftJoycon = new JoyConControllerLeft
                {
                    Stick       = ToID(Parser.Value("Controls_Left_JoyConController_Stick_"        + i)),
                    StickButton = ToID(Parser.Value("Controls_Left_JoyConController_Stick_Button_" + i)),
                    DPadUp      = ToID(Parser.Value("Controls_Left_JoyConController_DPad_Up_"      + i)),
                    DPadDown    = ToID(Parser.Value("Controls_Left_JoyConController_DPad_Down_"    + i)),
                    DPadLeft    = ToID(Parser.Value("Controls_Left_JoyConController_DPad_Left_"    + i)),
                    DPadRight   = ToID(Parser.Value("Controls_Left_JoyConController_DPad_Right_"   + i)),
                    ButtonMinus = ToID(Parser.Value("Controls_Left_JoyConController_Button_Minus_" + i)),
                    ButtonL     = ToID(Parser.Value("Controls_Left_JoyConController_Button_L_"     + i)),
                    ButtonZL    = ToID(Parser.Value("Controls_Left_JoyConController_Button_ZL_"    + i))
                };

                JoyConControllerRight RightJoycon = new JoyConControllerRight
                {
                    Stick       = ToID(Parser.Value("Controls_Right_JoyConController_Stick_"        + i)),
                    StickButton = ToID(Parser.Value("Controls_Right_JoyConController_Stick_Button_" + i)),
                    ButtonA     = ToID(Parser.Value("Controls_Right_JoyConController_Button_A_"     + i)),
                    ButtonB     = ToID(Parser.Value("Controls_Right_JoyConController_Button_B_"     + i)),
                    ButtonX     = ToID(Parser.Value("Controls_Right_JoyConController_Button_X_"     + i)),
                    ButtonY     = ToID(Parser.Value("Controls_Right_JoyConController_Button_Y_"     + i)),
                    ButtonPlus  = ToID(Parser.Value("Controls_Right_JoyConController_Button_Plus_"  + i)),
                    ButtonR     = ToID(Parser.Value("Controls_Right_JoyConController_Button_R_"     + i)),
                    ButtonZR    = ToID(Parser.Value("Controls_Right_JoyConController_Button_ZR_"    + i))
                };

                JoyConController Controller = new JoyConController(
                               Convert.ToBoolean(GamePadEnable),
                               Convert.ToInt32  (Parser.Value("GamePad_Index_" + i)),
                        (float)Convert.ToDouble (Parser.Value("GamePad_Deadzone_" + i),          CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble (Parser.Value("GamePad_Trigger_Threshold_" + i), CultureInfo.InvariantCulture),
                        LeftJoycon, RightJoycon);

                JoyConControllerList.Add(Controller);
            }

            //Finally, convert that to a regular Array.
            JoyConControllers = JoyConControllerList.ToArray();
        }

        private static ControllerInputID ToID(string Key)
        {
            switch (Key.ToUpper())
            {
                case "LSTICK":    return ControllerInputID.LStick;
                case "DPADUP":    return ControllerInputID.DPadUp;
                case "DPADDOWN":  return ControllerInputID.DPadDown;
                case "DPADLEFT":  return ControllerInputID.DPadLeft;
                case "DPADRIGHT": return ControllerInputID.DPadRight;
                case "BACK":      return ControllerInputID.Back;
                case "LSHOULDER": return ControllerInputID.LShoulder;
                case "LTRIGGER":  return ControllerInputID.LTrigger;

                case "RSTICK":    return ControllerInputID.RStick;
                case "A":         return ControllerInputID.A;
                case "B":         return ControllerInputID.B;
                case "X":         return ControllerInputID.X;
                case "Y":         return ControllerInputID.Y;
                case "START":     return ControllerInputID.Start;
                case "RSHOULDER": return ControllerInputID.RShoulder;
                case "RTRIGGER":  return ControllerInputID.RTrigger;

                case "LJOYSTICK": return ControllerInputID.LJoystick;
                case "RJOYSTICK": return ControllerInputID.RJoystick;

                default: return ControllerInputID.Invalid;
            }
        }

        private static HidHostDevice ToHostDevice(string Key)
        {
            switch (Key.ToUpper())
            {
                case "NONE":     return HidHostDevice.None;
                case "KEYBOARD": return HidHostDevice.Keyboard;
            }

            if (Key.ToUpper().Contains("GAMEPAD_") && Char.IsDigit(Key[Key.Length-1]))
            {
                switch (Key[Key.Length - 1])
                {
                    case '0': return HidHostDevice.GamePad0;
                    case '1': return HidHostDevice.GamePad1;
                    case '2': return HidHostDevice.GamePad2;
                    case '3': return HidHostDevice.GamePad3;
                    case '4': return HidHostDevice.GamePad4;
                    case '5': return HidHostDevice.GamePad5;
                    case '6': return HidHostDevice.GamePad6;
                    case '7': return HidHostDevice.GamePad7;
                    case '8': return HidHostDevice.GamePad8;
                }
            }

            throw new ArgumentException("Not a valid Input Device");
        }
    }

    //https://stackoverflow.com/a/37772571
    public class IniParser
    {
        private readonly Dictionary<string, string> Values;

        public IniParser(string Path)
        {
            Values = File.ReadLines(Path)
                .Where(Line => !string.IsNullOrWhiteSpace(Line) && !Line.StartsWith('#'))
                .Select(Line => Line.Split('=', 2))
                .ToDictionary(Parts => Parts[0].Trim(), Parts => Parts.Length > 1 ? Parts[1].Trim() : null);
        }

        public string Value(string Name)
        {
            return Values.TryGetValue(Name, out string Value) ? Value : null;
        }
    }
}