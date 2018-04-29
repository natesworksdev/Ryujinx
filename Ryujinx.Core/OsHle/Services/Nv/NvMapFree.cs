namespace Ryujinx.Core.OsHle.Services.Nv
{
    struct NvMapFree
    {
        public int  Handle;
        public int  Padding;
        public long RefCount;
        public int  Size;
        public int  Flags;
    }
}