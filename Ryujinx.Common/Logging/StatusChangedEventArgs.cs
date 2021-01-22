using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Logging
{
    public class StatusChangedEventArgs : EventArgs
    {
        public readonly int Current;
        public readonly int Total;
        public readonly string ClassName;
        public bool ShouldDisable;
        public bool ShaderUpdate;
        public  StatusChangedEventArgs(int current,int total, string className,bool shouldDisable,bool shaderUpdate)
        {
            Current = current;
            Total = total;
            ClassName = className;
            ShouldDisable = shouldDisable;
            ShaderUpdate = shaderUpdate;
        }
    }
}
