namespace Ryujinx.Graphics.Host1x
{
    unsafe struct ThiRegisters
    {
        public uint IncrSyncpt;
        public uint Reserved4;
        public uint IncrSyncptErr;
        public uint CtxswIncrSyncpt;
        public fixed uint Reserved10[4];
        public uint Ctxsw;
        public uint Reserved24;
        public uint ContSyncptEof;
        public fixed uint Reserved2C[5];
        public uint Method0;
        public uint Method1;
        public fixed uint Reserved48[12];
        public uint IntStatus;
        public uint IntMask;
    }
}
