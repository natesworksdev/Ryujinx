using System;

namespace Ryujinx.Ui
{
    public class StatusUpdatedEventArgs : EventArgs
    {
        public bool   VSyncEnabled;
        public bool   IsMuted;
        public string DockedMode;
        public string AspectRatio;
        public string GameStatus;
        public string FifoStatus;
        public string GpuName;

        public StatusUpdatedEventArgs(bool vSyncEnabled, bool isMuted, string dockedMode, string aspectRatio, string gameStatus, string fifoStatus, string gpuName)
        {
            VSyncEnabled = vSyncEnabled;
            IsMuted      = isMuted;
            DockedMode   = dockedMode;
            AspectRatio  = aspectRatio;
            GameStatus   = gameStatus;
            FifoStatus   = fifoStatus;
            GpuName      = gpuName;
        }
    }
}