using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Memory
{
    public struct NvGpuPBEntry
    {
        public int Method { get; private set; }

        public int SubChannel { get; private set; }

        private int[] _mArguments;

        public ReadOnlyCollection<int> Arguments => Array.AsReadOnly(_mArguments);

        public NvGpuPBEntry(int method, int subChannel, params int[] arguments)
        {
            Method      = method;
            SubChannel  = subChannel;
            _mArguments = arguments;
        }
    }
}