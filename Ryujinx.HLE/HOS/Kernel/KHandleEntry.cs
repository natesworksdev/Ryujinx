namespace Ryujinx.HLE.HOS.Kernel
{
    internal class KHandleEntry
    {
        public KHandleEntry Next { get; set; }

        public int Index { get; private set; }

        public ushort HandleId { get; set; }
        public object Obj      { get; set; }

        public KHandleEntry(int index)
        {
            this.Index = index;
        }
    }
}