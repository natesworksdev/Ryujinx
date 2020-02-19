using System;

namespace Ryujinx.Ui
{
    public class StatusUpdatedEventArgs : EventArgs
    {
        public bool   VSyncEnabled        { get; set; }
        public string HostFpsStatus       { get; set; }
        public string GameFpsStatus       { get; set; }
        public string HostFrameTimeStatus { get; set; }
        public string GameFrameTimeStatus { get; set; }
    }
}