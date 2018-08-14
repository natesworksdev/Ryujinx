using Ryujinx.UI.Input;
using Ryujinx.HLE.Logging;
using Ryujinx.HLE.Input;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using static Ryujinx.HLE.OsHle.SystemStateMgr;

namespace Ryujinx
{
    public static class Config
    {
        public static JoyConKeyboard     JoyConKeyboard        { get; private set; }
        public static JoyConController[] JoyConControllers     { get; private set; }
        public static bool               GamePadEnable         { get; private set; }

        public static void Read(Logger Log)
        {
            string IniFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string IniPath = Path.Combine(IniFolder, "Ryujinx.conf");

            IniParser Parser = new IniParser(IniPath);

            AOptimizations.DisableMemoryChecks = !Convert.ToBoolean(Parser.Value("Enable_Memory_Checks"));

            GraphicsConfig.ShadersDumpPath = Parser.Value("Graphics_Shaders_Dump_Path");

            Log.SetEnable(LogLevel.Debug,   Convert.ToBoolean(Parser.Value("Logging_Enable_Debug")));
            Log.SetEnable(LogLevel.Stub,    Convert.ToBoolean(Parser.Value("Logging_Enable_Stub")));
            Log.SetEnable(LogLevel.Info,    Convert.ToBoolean(Parser.Value("Logging_Enable_Info")));
            Log.SetEnable(LogLevel.Warning, Convert.ToBoolean(Parser.Value("Logging_Enable_Warn")));
            Log.SetEnable(LogLevel.Error,   Convert.ToBoolean(Parser.Value("Logging_Enable_Error")));
            
            DockedMode = Convert.ToBoolean(Parser.Value("Docked_Mode"));

            string[] FilteredLogClasses = Parser.Value("Logging_Filtered_Classes").Split(',', StringSplitOptions.RemoveEmptyEntries);

            GamePadEnable = Boolean.Parse(Parser.Value("GamePad_Enable"));

            //Device Mappings
            HidEmulatedDevices.Devices.Handheld      = ToEmulatedDevice(Parser.Value("Handheld_Device")); // -2: None, -1: Keyboard, Everything Else: GamePad Index
            HidEmulatedDevices.Devices.Player1       = ToEmulatedDevice(Parser.Value("Player1_Device"));
            HidEmulatedDevices.Devices.Player2       = ToEmulatedDevice(Parser.Value("Player2_Device"));
            HidEmulatedDevices.Devices.Player3       = ToEmulatedDevice(Parser.Value("Player3_Device"));
            HidEmulatedDevices.Devices.Player4       = ToEmulatedDevice(Parser.Value("Player4_Device"));
            HidEmulatedDevices.Devices.Player5       = ToEmulatedDevice(Parser.Value("Player5_Device"));
            HidEmulatedDevices.Devices.Player6       = ToEmulatedDevice(Parser.Value("Player6_Device"));
            HidEmulatedDevices.Devices.Player7       = ToEmulatedDevice(Parser.Value("Player7_Device"));
            HidEmulatedDevices.Devices.Player8       = ToEmulatedDevice(Parser.Value("Player8_Device"));
            HidEmulatedDevices.Devices.PlayerUnknown = ToEmulatedDevice(Parser.Value("PlayerUnknown_Device"));

            //When the classes are specified on the list, we only
            //enable the classes that are on the list.
            //So, first disable everything, then enable
            //the classes that the user added to the list.
            if (FilteredLogClasses.Length > 0)
            {
                foreach (LogClass Class in Enum.GetValues(typeof(LogClass)))
                {
                    Log.SetEnable(Class, false);
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
                            Log.SetEnable(Class, true);
                        }
                    }
                }
            }

            JoyConKeyboard = new JoyConKeyboard(

                new JoyConKeyboardLeft
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
                },

                new JoyConKeyboardRight
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
                });


            List<JoyConController> JoyConControllerList = new List<JoyConController>();

            //Populate the Controller List
            for (int i = 0; i < 255; ++i)
            {
                if (Parser.Value(i + "_GamePad_Index") == null) break;

                JoyConController Controller = new JoyConController(
                       Convert.ToBoolean(GamePadEnable),
                       Convert.ToInt32  (Parser.Value(i + "_GamePad_Index")),
                (float)Convert.ToDouble (Parser.Value(i + "_GamePad_Deadzone"),          CultureInfo.InvariantCulture),
                (float)Convert.ToDouble (Parser.Value(i + "_GamePad_Trigger_Threshold"), CultureInfo.InvariantCulture),

                new JoyConControllerLeft
                {
                    Stick       = ToID(Parser.Value(i + "_Controls_Left_JoyConController_Stick")),
                    StickButton = ToID(Parser.Value(i + "_Controls_Left_JoyConController_Stick_Button")),
                    DPadUp      = ToID(Parser.Value(i + "_Controls_Left_JoyConController_DPad_Up")),
                    DPadDown    = ToID(Parser.Value(i + "_Controls_Left_JoyConController_DPad_Down")),
                    DPadLeft    = ToID(Parser.Value(i + "_Controls_Left_JoyConController_DPad_Left")),
                    DPadRight   = ToID(Parser.Value(i + "_Controls_Left_JoyConController_DPad_Right")),
                    ButtonMinus = ToID(Parser.Value(i + "_Controls_Left_JoyConController_Button_Minus")),
                    ButtonL     = ToID(Parser.Value(i + "_Controls_Left_JoyConController_Button_L")),
                    ButtonZL    = ToID(Parser.Value(i + "_Controls_Left_JoyConController_Button_ZL"))
                },

                new JoyConControllerRight
                {
                    Stick       = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Stick")),
                    StickButton = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Stick_Button")),
                    ButtonA     = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_A")),
                    ButtonB     = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_B")),
                    ButtonX     = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_X")),
                    ButtonY     = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_Y")),
                    ButtonPlus  = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_Plus")),
                    ButtonR     = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_R")),
                    ButtonZR    = ToID(Parser.Value(i + "_Controls_Right_JoyConController_Button_ZR"))
                });

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

        // -2: None, -1: Keyboard, Everything Else: GamePad Index
        private static int ToEmulatedDevice(string Key)
        {
            switch (Key.ToUpper())
            {
                case "NONE":     return -2;
                case "KEYBOARD": return -1;
            }

            if (Key.ToUpper().StartsWith("GAMEPAD_")) return Int32.Parse(Key.Substring(Key.Length - 1));

            return -2;
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