using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using Ryujinx.UI.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ryujinx
{
    public class ControlSettings : Window
    {
        private HLE.Switch device { get; set; }

        internal static Configuration SwitchConfig { get; private set; }

        [GUI] Window       CSWin;
        [GUI] CheckButton  DirectKeyboardAccess;
        [GUI] Image        ControllerImage;

        [GUI] ToggleButton LStickUp1;
        [GUI] ToggleButton LStickDown1;
        [GUI] ToggleButton LStickLeft1;
        [GUI] ToggleButton LStickRight1;
        [GUI] ToggleButton LStickButton1;
        [GUI] ToggleButton DpadUp1;
        [GUI] ToggleButton DpadDown1;
        [GUI] ToggleButton DpadLeft1;
        [GUI] ToggleButton DpadRight1;
        [GUI] ToggleButton Minus1;
        [GUI] ToggleButton L1;
        [GUI] ToggleButton ZL1;
        [GUI] ToggleButton RStickUp1;
        [GUI] ToggleButton RStickDown1;
        [GUI] ToggleButton RStickLeft1;
        [GUI] ToggleButton RStickRight1;
        [GUI] ToggleButton RStickButton1;
        [GUI] ToggleButton A1;
        [GUI] ToggleButton B1;
        [GUI] ToggleButton X1;
        [GUI] ToggleButton Y1;
        [GUI] ToggleButton Plus1;
        [GUI] ToggleButton R1;
        [GUI] ToggleButton ZR1;

        public static void ConfigureControls(Configuration Instance) { SwitchConfig = Instance; }

        public ControlSettings(HLE.Switch _device) : this(new Builder("Ryujinx.GUI.ControlSettings.glade"), _device) { }

        private ControlSettings(Builder builder, HLE.Switch _device) : base(builder.GetObject("CSWin").Handle)
        {
            device = _device;

            builder.Autoconnect(this);

            CSWin.Icon             = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png");
            ControllerImage.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.JoyCon.png", 400, 400);

            LStickUp1.Toggled     += (o, args) => Button_Pressed(o, args, LStickUp1);
            LStickDown1.Toggled   += (o, args) => Button_Pressed(o, args, LStickDown1);
            LStickLeft1.Toggled   += (o, args) => Button_Pressed(o, args, LStickLeft1);
            LStickRight1.Toggled  += (o, args) => Button_Pressed(o, args, LStickRight1);
            LStickButton1.Toggled += (o, args) => Button_Pressed(o, args, LStickButton1);
            DpadUp1.Toggled       += (o, args) => Button_Pressed(o, args, DpadUp1);
            DpadDown1.Toggled     += (o, args) => Button_Pressed(o, args, DpadDown1);
            DpadLeft1.Toggled     += (o, args) => Button_Pressed(o, args, DpadLeft1);
            DpadRight1.Toggled    += (o, args) => Button_Pressed(o, args, DpadRight1);
            Minus1.Toggled        += (o, args) => Button_Pressed(o, args, Minus1);
            L1.Toggled            += (o, args) => Button_Pressed(o, args, L1);
            ZL1.Toggled           += (o, args) => Button_Pressed(o, args, ZL1);
            RStickUp1.Toggled     += (o, args) => Button_Pressed(o, args, RStickUp1);
            RStickDown1.Toggled   += (o, args) => Button_Pressed(o, args, RStickDown1);
            RStickLeft1.Toggled   += (o, args) => Button_Pressed(o, args, RStickLeft1);
            RStickRight1.Toggled  += (o, args) => Button_Pressed(o, args, RStickRight1);
            RStickButton1.Toggled += (o, args) => Button_Pressed(o, args, RStickButton1);
            A1.Toggled            += (o, args) => Button_Pressed(o, args, A1);
            B1.Toggled            += (o, args) => Button_Pressed(o, args, B1);
            X1.Toggled            += (o, args) => Button_Pressed(o, args, X1);
            Y1.Toggled            += (o, args) => Button_Pressed(o, args, Y1);
            Plus1.Toggled         += (o, args) => Button_Pressed(o, args, Plus1);
            R1.Toggled            += (o, args) => Button_Pressed(o, args, R1);
            ZR1.Toggled           += (o, args) => Button_Pressed(o, args, ZR1);

            if (SwitchConfig.EnableKeyboard) { DirectKeyboardAccess.Click(); }
            LStickUp1.Label     = SwitchConfig.KeyboardControls.LeftJoycon.StickUp.ToString();
            LStickDown1.Label   = SwitchConfig.KeyboardControls.LeftJoycon.StickDown.ToString();
            LStickLeft1.Label   = SwitchConfig.KeyboardControls.LeftJoycon.StickLeft.ToString();
            LStickRight1.Label  = SwitchConfig.KeyboardControls.LeftJoycon.StickRight.ToString();
            LStickButton1.Label = SwitchConfig.KeyboardControls.LeftJoycon.StickButton.ToString();
            DpadUp1.Label       = SwitchConfig.KeyboardControls.LeftJoycon.DPadUp.ToString();
            DpadDown1.Label     = SwitchConfig.KeyboardControls.LeftJoycon.DPadDown.ToString();
            DpadLeft1.Label     = SwitchConfig.KeyboardControls.LeftJoycon.DPadLeft.ToString();
            DpadRight1.Label    = SwitchConfig.KeyboardControls.LeftJoycon.DPadRight.ToString();
            Minus1.Label        = SwitchConfig.KeyboardControls.LeftJoycon.ButtonMinus.ToString();
            L1.Label            = SwitchConfig.KeyboardControls.LeftJoycon.ButtonL.ToString();
            ZL1.Label           = SwitchConfig.KeyboardControls.LeftJoycon.ButtonZl.ToString();
            RStickUp1.Label     = SwitchConfig.KeyboardControls.RightJoycon.StickUp.ToString();
            RStickDown1.Label   = SwitchConfig.KeyboardControls.RightJoycon.StickDown.ToString();
            RStickLeft1.Label   = SwitchConfig.KeyboardControls.RightJoycon.StickLeft.ToString();
            RStickRight1.Label  = SwitchConfig.KeyboardControls.RightJoycon.StickRight.ToString();
            RStickButton1.Label = SwitchConfig.KeyboardControls.RightJoycon.StickButton.ToString();
            A1.Label            = SwitchConfig.KeyboardControls.RightJoycon.ButtonA.ToString();
            B1.Label            = SwitchConfig.KeyboardControls.RightJoycon.ButtonB.ToString();
            X1.Label            = SwitchConfig.KeyboardControls.RightJoycon.ButtonX.ToString();
            Y1.Label            = SwitchConfig.KeyboardControls.RightJoycon.ButtonY.ToString();
            Plus1.Label         = SwitchConfig.KeyboardControls.RightJoycon.ButtonPlus.ToString();
            R1.Label            = SwitchConfig.KeyboardControls.RightJoycon.ButtonR.ToString();
            ZR1.Label           = SwitchConfig.KeyboardControls.RightJoycon.ButtonZr.ToString();
        }

        //Events
        private void Button_Pressed(object button, EventArgs args, ToggleButton Button)
        {
            KeyPressEvent += On_KeyPress;

            void On_KeyPress(object obj , KeyPressEventArgs KeyPressed)
            {
                string key = KeyPressed.Event.Key.ToString();

                if (Enum.IsDefined(typeof(OpenTK.Input.Key), key.First().ToString().ToUpper() + key.Substring(1))) { Button.Label = key.First().ToString().ToUpper() + key.Substring(1); }
                else if (GdkToTKInput.ContainsKey(key))                                                            { Button.Label = GdkToTKInput[key]; }
                else                                                                                               { Button.Label = "Space"; }

                Button.SetStateFlags(0, true);
                KeyPressEvent -= On_KeyPress;
            }
        }

        private void SaveToggle_Activated(object obj, EventArgs args)
        {
            if (DirectKeyboardAccess.Active) { SwitchConfig.EnableKeyboard = true; }

            SwitchConfig.KeyboardControls.LeftJoycon = new NpadKeyboardLeft()
            {
                StickUp     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), LStickUp1.Label),
                StickDown   = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), LStickDown1.Label),
                StickLeft   = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), LStickLeft1.Label),
                StickRight  = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), LStickRight1.Label),
                StickButton = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), LStickButton1.Label),
                DPadUp      = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), DpadUp1.Label),
                DPadDown    = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), DpadDown1.Label),
                DPadLeft    = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), DpadLeft1.Label),
                DPadRight   = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), DpadRight1.Label),
                ButtonMinus = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), Minus1.Label),
                ButtonL     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), L1.Label),
                ButtonZl    = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), ZL1.Label),
            };

            SwitchConfig.KeyboardControls.RightJoycon = new NpadKeyboardRight()
            {
                StickUp     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), RStickUp1.Label),
                StickDown   = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), RStickDown1.Label),
                StickLeft   = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), RStickLeft1.Label),
                StickRight  = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), RStickRight1.Label),
                StickButton = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), RStickButton1.Label),
                ButtonA     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), A1.Label),
                ButtonB     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), B1.Label),
                ButtonX     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), X1.Label),
                ButtonY     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), Y1.Label),
                ButtonPlus  = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), Plus1.Label),
                ButtonR     = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), R1.Label),
                ButtonZr    = (OpenTK.Input.Key)Enum.Parse(typeof(OpenTK.Input.Key), ZR1.Label),
            };

            Configuration.SaveConfig(SwitchConfig, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json"));
            Configuration.Configure(device, SwitchConfig);

            Destroy();
        }

        private void CloseToggle_Activated(object obj, EventArgs args)
        {
            Destroy();
        }

        public readonly Dictionary<string, string> GdkToTKInput = new Dictionary<string, string>()
        {
            {"Key_0", "Number0"},
            {"Key_1", "Number1"},
            {"Key_2", "Number2"},
            {"Key_3", "Number3"},
            {"Key_4", "Number4"},
            {"Key_5", "Number5"},
            {"Key_6", "Number6"},
            {"Key_7", "Number7"},
            {"Key_8", "Number8"},
            {"Key_9", "Number9"},
            {"equal", "Plus"},
            {"uparrow", "Up"},
            {"downarrow", "Down"},
            {"leftarrow", "Left"},
            {"rightarrow", "Right"},
            {"Control_L", "ControlLeft"},
            {"Control_R", "ControlRight"},
            {"Shift_L", "ShiftLeft"},
            {"Shift_R", "ShiftRight"},
            {"Alt_L", "AltLeft"},
            {"Alt_R", "AltRight"},
            {"Page_Up", "PageUp"},
            {"Page_Down", "PageDown"},
            {"KP_Enter", "KeypadEnter"},
            {"KP_Up", "Up"},
            {"KP_Down", "Down"},
            {"KP_Left", "Left"},
            {"KP_Right", "Right"},
            {"KP_Divide", "KeypadDivide"},
            {"KP_Multiply", "KeypadMultiply"},
            {"KP_Subtract", "KeypadSubtract"},
            {"KP_Add", "KeypadAdd"},
            {"KP_Decimal", "KeypadDecimal"},
            {"KP_0", "Keypad0"},
            {"KP_1", "Keypad1"},
            {"KP_2", "Keypad2"},
            {"KP_3", "Keypad3"},
            {"KP_4", "Keypad4"},
            {"KP_5", "Keypad5"},
            {"KP_6", "Keypad6"},
            {"KP_7", "Keypad7"},
            {"KP_8", "Keypad8"},
            {"KP_9", "Keypad9"},
        };
    }
}
