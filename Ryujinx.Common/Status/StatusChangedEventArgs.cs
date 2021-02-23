using System;

namespace Ryujinx.Common.Status
{
    public class StatusChangedEventArgs : EventArgs
    {
        public readonly int Current;
        public readonly int Total;
        public readonly StatusType StatusType;

        public StatusChangedEventArgs(int current, int total, StatusType statusType)
        {
            Current = current;
            Total = total;
            StatusType = statusType;
        }
    }
}
