using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Core.OsHle.Services.Nv
{
    class NvHostEvent
    {
        public int Id;
        public int Thresh;

        public bool Free;
    }
}