using OpenTK.Input;
using Ryujinx.HLE.Input;
using Ryujinx.HLE.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ryujinx
{
    public static class Config
    {
        public static JoyCon FakeJoyCon { get; private set; }

        public static float GamePad_Deadzone;
        public static bool  GamePad_Enable;
        public static int   GamePad_Index;

        public static string Controls_Right_FakeJoycon_GamePadButton_A;
        public static string Controls_Right_FakeJoycon_GamePadButton_B;
        public static string Controls_Right_FakeJoycon_GamePadButton_X;
        public static string Controls_Right_FakeJoycon_GamePadButton_Y;
        public static string Controls_Right_FakeJoycon_GamePadButton_Plus;
        public static string Controls_Right_FakeJoycon_GamePadButton_R;
        public static string Controls_Right_FakeJoycon_GamePadStick_Button;
        public static string Controls_Right_FakeJoycon_GamePadTrigger_ZR;

        public static string Controls_Left_FakeJoycon_GamePadDPad_Up;
        public static string Controls_Left_FakeJoycon_GamePadDPad_Down;
        public static string Controls_Left_FakeJoycon_GamePadDPad_Left;
        public static string Controls_Left_FakeJoycon_GamePadDPad_Right;
        public static string Controls_Left_FakeJoycon_GamePadButton_Minus;
        public static string Controls_Left_FakeJoycon_GamePadButton_L;
        public static string Controls_Left_FakeJoycon_GamePadStick_Button;
        public static string Controls_Left_FakeJoycon_GamePadTrigger_ZL;

        public static void Read(Logger Log)
        {
            string IniFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string IniPath = Path.Combine(IniFolder, "Ryujinx.conf");

            IniParser Parser = new IniParser(IniPath);

            AOptimizations.DisableMemoryChecks = !Convert.ToBoolean(Parser.Value("Enable_Memory_Checks"));

            Log.SetEnable(LogLevel.Debug,   Convert.ToBoolean(Parser.Value("Logging_Enable_Debug")));
            Log.SetEnable(LogLevel.Stub,    Convert.ToBoolean(Parser.Value("Logging_Enable_Stub")));
            Log.SetEnable(LogLevel.Info,    Convert.ToBoolean(Parser.Value("Logging_Enable_Info")));
            Log.SetEnable(LogLevel.Warning, Convert.ToBoolean(Parser.Value("Logging_Enable_Warn")));
            Log.SetEnable(LogLevel.Error,   Convert.ToBoolean(Parser.Value("Logging_Enable_Error")));

            GamePad_Enable   =        Convert.ToBoolean(Parser.Value("GamePad_Enable"));
            GamePad_Index    =        Convert.ToInt32  (Parser.Value("GamePad_Index"));
            GamePad_Deadzone = (float)Convert.ToDouble (Parser.Value("GamePad_Deadzone"));

            Controls_Right_FakeJoycon_GamePadButton_A     = Parser.Value("Controls_Right_FakeJoycon_GamePadButton_A");
            Controls_Right_FakeJoycon_GamePadButton_B     = Parser.Value("Controls_Right_FakeJoycon_GamePadButton_B");
            Controls_Right_FakeJoycon_GamePadButton_X     = Parser.Value("Controls_Right_FakeJoycon_GamePadButton_X");
            Controls_Right_FakeJoycon_GamePadButton_Y     = Parser.Value("Controls_Right_FakeJoycon_GamePadButton_Y");
            Controls_Right_FakeJoycon_GamePadButton_Plus  = Parser.Value("Controls_Right_FakeJoycon_GamePadButton_Plus");
            Controls_Right_FakeJoycon_GamePadButton_R     = Parser.Value("Controls_Right_FakeJoycon_GamePadButton_R");
            Controls_Right_FakeJoycon_GamePadStick_Button = Parser.Value("Controls_Right_FakeJoycon_GamePadStick_Button");
            Controls_Right_FakeJoycon_GamePadTrigger_ZR   = Parser.Value("Controls_Right_FakeJoycon_GamePadTrigger_ZR");

            Controls_Left_FakeJoycon_GamePadDPad_Up       = Parser.Value("Controls_Left_FakeJoycon_GamePadDPad_Up");
            Controls_Left_FakeJoycon_GamePadDPad_Down     = Parser.Value("Controls_Left_FakeJoycon_GamePadDPad_Down");
            Controls_Left_FakeJoycon_GamePadDPad_Left     = Parser.Value("Controls_Left_FakeJoycon_GamePadDPad_Left");
            Controls_Left_FakeJoycon_GamePadDPad_Right    = Parser.Value("Controls_Left_FakeJoycon_GamePadDPad_Right");
            Controls_Left_FakeJoycon_GamePadButton_Minus  = Parser.Value("Controls_Left_FakeJoycon_GamePadButton_Minus");
            Controls_Left_FakeJoycon_GamePadButton_L      = Parser.Value("Controls_Left_FakeJoycon_GamePadButton_L");
            Controls_Left_FakeJoycon_GamePadStick_Button  = Parser.Value("Controls_Left_FakeJoycon_GamePadStick_Button");
            Controls_Left_FakeJoycon_GamePadTrigger_ZL    = Parser.Value("Controls_Left_FakeJoycon_GamePadTrigger_ZL");

            string[] FilteredLogClasses = Parser.Value("Logging_Filtered_Classes").Split(',', StringSplitOptions.RemoveEmptyEntries);

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

            FakeJoyCon = new JoyCon
            {
                Left = new JoyConLeft
                {
                    StickUp     = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Stick_Up")),
                    StickDown   = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Stick_Down")),
                    StickLeft   = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Stick_Left")),
                    StickRight  = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Stick_Right")),
                    StickButton = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Stick_Button")),
                    DPadUp      = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_DPad_Up")),
                    DPadDown    = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_DPad_Down")),
                    DPadLeft    = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_DPad_Left")),
                    DPadRight   = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_DPad_Right")),
                    ButtonMinus = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Button_Minus")),
                    ButtonL     = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Button_L")),
                    ButtonZL    = Convert.ToInt16(Parser.Value("Controls_Left_FakeJoycon_Button_ZL"))
                },

                Right = new JoyConRight
                {
                    StickUp     = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Stick_Up")),
                    StickDown   = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Stick_Down")),
                    StickLeft   = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Stick_Left")),
                    StickRight  = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Stick_Right")),
                    StickButton = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Stick_Button")),
                    ButtonA     = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_A")),
                    ButtonB     = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_B")),
                    ButtonX     = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_X")),
                    ButtonY     = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_Y")),
                    ButtonPlus  = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_Plus")),
                    ButtonR     = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_R")),
                    ButtonZR    = Convert.ToInt16(Parser.Value("Controls_Right_FakeJoycon_Button_ZR"))
                }
            };
        }
    }

    // https://stackoverflow.com/a/37772571
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
