using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using System;
using System.IO;

namespace Ryujinx
{
    public class GeneralSettings : Window
    {
        internal HLE.Switch device { get; private set; }

        internal static Configuration SwitchConfig { get; private set; }

        //UI Controls
        [GUI] CheckButton  ErrorLogToggle;
        [GUI] CheckButton  WarningLogToggle;
        [GUI] CheckButton  InfoLogToggle;
        [GUI] CheckButton  StubLogToggle;
        [GUI] CheckButton  DebugLogToggle;
        [GUI] CheckButton  FileLogToggle;
        [GUI] CheckButton  DockedModeToggle;
        [GUI] CheckButton  DiscordToggle;
        [GUI] CheckButton  VSyncToggle;
        [GUI] CheckButton  MultiSchedToggle;
        [GUI] CheckButton  FSICToggle;
        [GUI] CheckButton  AggrToggle;
        [GUI] CheckButton  IgnoreToggle;
        [GUI] ComboBoxText SystemLanguageSelect;
        [GUI] TextView     GameDirsBox;
        [GUI] Button       SaveButton;
        [GUI] Button       CancelButton;

        public GeneralSettings(HLE.Switch _device) : this(new Builder("Ryujinx.GeneralSettings.glade"), _device) { }

        private GeneralSettings(Builder builder, HLE.Switch _device) : base(builder.GetObject("GSWin").Handle)
        {
            device = _device;

            builder.Autoconnect(this);

            SaveButton.Activated   += Save_Activated;
            CancelButton.Activated += Cancel_Activated;

            if (SwitchConfig.LoggingEnableError == true) { ErrorLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableWarn == true) { WarningLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableInfo == true) { InfoLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableStub == true) { StubLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableDebug == true) { DebugLogToggle.Click(); }
            if (SwitchConfig.EnableFileLog == true) { FileLogToggle.Click(); }
            if (SwitchConfig.DockedMode == true) { DockedModeToggle.Click(); }
            if (SwitchConfig.EnableDiscordIntergration == true) { DiscordToggle.Click(); }
            if (SwitchConfig.EnableVsync == true) { VSyncToggle.Click(); }
            if (SwitchConfig.EnableMulticoreScheduling == true) { MultiSchedToggle.Click(); }
            if (SwitchConfig.EnableFsIntegrityChecks == true) { FSICToggle.Click(); }
            if (SwitchConfig.EnableAggressiveCpuOpts == true) { AggrToggle.Click(); }
            if (SwitchConfig.IgnoreMissingServices == true) { IgnoreToggle.Click(); }
            SystemLanguageSelect.SetActiveId(SwitchConfig.SystemLanguage.ToString());

            GameDirsBox.Buffer.Text = File.ReadAllText("./GameDirs.dat");
        }

        public static void ConfigureSettings(Configuration Instance) { SwitchConfig = Instance; }

        //Events
        private void Save_Activated(object obj, EventArgs args)
        {
            //Saving code is about to make this a BIG boi

            File.WriteAllText("./GameDirs.dat", GameDirsBox.Buffer.Text);

            Destroy();
        }

        private void Cancel_Activated(object obj, EventArgs args)
        {
            Destroy();
        }
    }
}
