using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Status
{
    public class StatusChangedEventArgs : EventArgs
    {
        public readonly int Current;
        public readonly int Total;
        public readonly string ClassName;
        public  StatusChangedEventArgs(int current,int total, string className)
        {
            Current = current;
            Total = total;
            ClassName = className;
        }
    }
}
