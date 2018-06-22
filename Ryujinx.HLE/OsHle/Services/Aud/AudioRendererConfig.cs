namespace Ryujinx.HLE.OsHle.Services.Aud
{
    struct AudioRendererConfig
    {
        public int Revision;
        public int BehaviourSize;
        public int MemoryPoolsSize;
        public int VoicesSize;
        public int VoiceResourceSize;
        public int EffectsSize;
        public int MixesSize;
        public int SinksSize;
        public int PerformanceBufferSize;
        public int Padding0;
        public int Padding1;
        public int Padding2;
        public int Padding3;
        public int Padding4;
        public int Padding5;
        public int Padding6;
        public int TotalSize;
    }
}
