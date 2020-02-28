using System;

namespace Ryujinx.Ui
{
    public class StatusUpdatedEventArgs : EventArgs
    {
        public bool   VSyncEnabled { get; set; }
        public string HostStatus   { get; set; }
        public string GameStatus   { get; set; }
    }
}