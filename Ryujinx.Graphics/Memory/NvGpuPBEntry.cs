using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Memory
{
    public struct NvGpuPbEntry
    {
        public int Method { get; private set; }

        public int SubChannel { get; private set; }

        private int[] _arguments;

        public ReadOnlyCollection<int> Arguments => Array.AsReadOnly(_arguments);

        public NvGpuPbEntry(int method, int subChannel, params int[] arguments)
        {
            Method      = method;
            SubChannel  = subChannel;
            _arguments = arguments;
        }
    }
}