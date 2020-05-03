using System;

namespace Ryujinx.Ui
{
    public class StatusUpdatedEventArgs : EventArgs
    {
        public bool   VSyncEnabled;
        public string HostStatus;
        public string GameStatus;
        public string GpuName;
        public string DockedMode;

        public StatusUpdatedEventArgs(bool vSyncEnabled, string hostStatus, string gameStatus, string gpuName, string dockedMode)
        {
            VSyncEnabled = vSyncEnabled;
            HostStatus   = hostStatus;
            GameStatus   = gameStatus;
            GpuName      = gpuName;
            DockedMode   = dockedMode;
        }
    }
}