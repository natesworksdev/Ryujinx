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
        public int Unknown0;
        public int Unknown1;
        public int Unknown2;
        public int Unknown3;
        public int Unknown4;
        public int Unknown5;
        public int Unknown6;
        public int TotalSize;
    }
}
