using System;

namespace Ryujinx.Ui
{
    public class StatusUpdatedEventArgs : EventArgs
    {
        public bool   VSyncEnabled;
        public string DockedMode;
        public string AspectRatio;
        public string GameStatus;
        public string FifoStatus;
        public string GpuName;
        public string GpuBackend;

        public StatusUpdatedEventArgs(bool vSyncEnabled, string gpuBackend, string dockedMode, string aspectRatio, string gameStatus, string fifoStatus, string gpuName)
        {
            VSyncEnabled = vSyncEnabled;
            GpuBackend   = gpuBackend;
            DockedMode   = dockedMode;
            AspectRatio  = aspectRatio;
            GameStatus   = gameStatus;
            FifoStatus   = fifoStatus;
            GpuName      = gpuName;
        }
    }
}