using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using Ryujinx.HLE.HOS.SystemState;
using System;
using System.IO;
using Utf8Json;
using Utf8Json.Resolvers;

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
        [GUI] CheckButton  CustThemeToggle;
        [GUI] Entry        CustThemeDir;
        [GUI] TextView     GameDirsBox;
        [GUI] ToggleButton SaveToggle;
        [GUI] ToggleButton CloseToggle;

        public static void ConfigureSettings(Configuration Instance) { SwitchConfig = Instance; }

        public GeneralSettings(HLE.Switch _device) : this(new Builder("Ryujinx.GeneralSettings.glade"), _device) { }

        private GeneralSettings(Builder builder, HLE.Switch _device) : base(builder.GetObject("GSWin").Handle)
        {
            device = _device;

            builder.Autoconnect(this);

            SaveToggle.Toggled      += SaveToggle_Activated;
            CloseToggle.Toggled     += CloseToggle_Activated;
            CustThemeToggle.Clicked += CustThemeToggle_Activated;

            if (SwitchConfig.LoggingEnableError)        { ErrorLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableWarn)         { WarningLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableInfo)         { InfoLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableStub)         { StubLogToggle.Click(); }
            if (SwitchConfig.LoggingEnableDebug)        { DebugLogToggle.Click(); }
            if (SwitchConfig.EnableFileLog)             { FileLogToggle.Click(); }
            if (SwitchConfig.DockedMode)                { DockedModeToggle.Click(); }
            if (SwitchConfig.EnableDiscordIntergration) { DiscordToggle.Click(); }
            if (SwitchConfig.EnableVsync)               { VSyncToggle.Click(); }
            if (SwitchConfig.EnableMulticoreScheduling) { MultiSchedToggle.Click(); }
            if (SwitchConfig.EnableFsIntegrityChecks)   { FSICToggle.Click(); }
            if (SwitchConfig.EnableAggressiveCpuOpts)   { AggrToggle.Click(); }
            if (SwitchConfig.IgnoreMissingServices)     { IgnoreToggle.Click(); }
            if (SwitchConfig.EnableCustomTheme)         { CustThemeToggle.Click(); }
            SystemLanguageSelect.SetActiveId(SwitchConfig.SystemLanguage.ToString());

            CustThemeDir.Buffer.Text = SwitchConfig.CustomThemePath;
            GameDirsBox.Buffer.Text = File.ReadAllText("./GameDirs.dat");

            if (CustThemeToggle.Active == false) { CustThemeDir.Sensitive = false; }
        }

        //Events
        private void CustThemeToggle_Activated(object obj, EventArgs args)
        {
            if (CustThemeToggle.Active == false) { CustThemeDir.Sensitive = false; } else { CustThemeDir.Sensitive = true; }
        }

        private void CloseToggle_Activated(object obj, EventArgs args)
        {
            Destroy();
        }

        private void SaveToggle_Activated(object obj, EventArgs args)
        {
            if (ErrorLogToggle.Active)   { SwitchConfig.LoggingEnableError        = true; }
            if (WarningLogToggle.Active) { SwitchConfig.LoggingEnableWarn         = true; }
            if (InfoLogToggle.Active)    { SwitchConfig.LoggingEnableInfo         = true; }
            if (StubLogToggle.Active)    { SwitchConfig.LoggingEnableStub         = true; }
            if (DebugLogToggle.Active)   { SwitchConfig.LoggingEnableDebug        = true; }
            if (FileLogToggle.Active)    { SwitchConfig.EnableFileLog             = true; }
            if (DockedModeToggle.Active) { SwitchConfig.DockedMode                = true; }
            if (DiscordToggle.Active)    { SwitchConfig.EnableDiscordIntergration = true; }
            if (VSyncToggle.Active)      { SwitchConfig.EnableVsync               = true; }
            if (MultiSchedToggle.Active) { SwitchConfig.EnableMulticoreScheduling = true; }
            if (FSICToggle.Active)       { SwitchConfig.EnableFsIntegrityChecks   = true; }
            if (AggrToggle.Active)       { SwitchConfig.EnableAggressiveCpuOpts   = true; }
            if (IgnoreToggle.Active)     { SwitchConfig.IgnoreMissingServices     = true; }
            if (CustThemeToggle.Active)  { SwitchConfig.EnableCustomTheme         = true; }

            SwitchConfig.SystemLanguage  = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), SystemLanguageSelect.ActiveId);
            SwitchConfig.CustomThemePath = CustThemeDir.Buffer.Text;

            File.WriteAllText("./GameDirs.dat", GameDirsBox.Buffer.Text);

            Configuration.Configure(device, SwitchConfig);

            Destroy();
        }
    }
}
