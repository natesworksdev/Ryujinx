using System;

namespace Ryujinx.Common.Status
{
    public static class StatusHandler
    {
        public static event EventHandler<StatusChangedEventArgs> StatusChangedEvent;
        public static event EventHandler StatusDisableEvent;

        public static void ChangeStatus(int current, int total, StatusType type)
        {
            StatusChangedEvent?.Invoke(null, new StatusChangedEventArgs(current, total, type));
        }

        public static void DisableStatus()
        {
            StatusDisableEvent?.Invoke(null, EventArgs.Empty);
        }
    }
}
