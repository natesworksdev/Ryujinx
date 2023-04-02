namespace Ryujinx.Common.Configuration.Hid
{
    public struct KeyboardHotkeys
    {
        public Hotkey Exit { get; set; }
        public Hotkey Pause { get; set; }
        public Hotkey ResScaleUp { get; set; }
        public Hotkey ResScaleDown { get; set; }
        public Hotkey Screenshot { get; set; }
        public Hotkey ShowUi { get; set; }
        public Hotkey ToggleDockedMode { get; set; }
        public Hotkey ToggleFullscreen { get; set; }
        public Hotkey ToggleMute { get; set; }
        public Hotkey ToggleVsync { get; set; }
        public Hotkey VolumeUp { get; set; }
        public Hotkey VolumeDown { get; set; }
    }
}
