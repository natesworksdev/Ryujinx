
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Status
{
    public static class StatusChanged
    {
        public static event EventHandler<StatusChangedEventArgs> StatusChangedEvent;
        public static event EventHandler StatusDisableEvent;
        public static void ChangeStatus(int current, int total, string message)
        {
            StatusChangedEvent.Invoke(null, new StatusChangedEventArgs(current, total, message));
        }
        public static void DisableStatus()
        {
            StatusDisableEvent.Invoke(null,EventArgs.Empty);
        }
    }
}
