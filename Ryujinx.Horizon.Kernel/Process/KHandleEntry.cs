using Ryujinx.Horizon.Kernel.Common;

namespace Ryujinx.Horizon.Kernel.Process
{
    class KHandleEntry
    {
        public KHandleEntry Next { get; set; }

        public int Index { get; private set; }

        public ushort HandleId { get; set; }
        public KAutoObject Obj { get; set; }

        public KHandleEntry(int index)
        {
            Index = index;
        }
    }
}