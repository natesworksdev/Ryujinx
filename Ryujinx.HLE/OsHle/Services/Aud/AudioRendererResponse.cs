namespace Ryujinx.HLE.OsHle.Services.Aud
{
    struct AudioRendererResponse
    {
        public int Revision;
        public int ErrorInfoSize;
        public int MemoryPoolsSize;
        public int VoicesSize;
        public int Unknown0;
        public int EffectsSize;
        public int Unknown1;
        public int SinksSize;
        public int PerformanceManagerSize;
        public int Unknown2;
        public int Unknown3;
        public int Unknown4;
        public int Unknown5;
        public int Unknown6;
        public int Unknown7;
        public int Unknown8;
        public int TotalSize;
    }
}
