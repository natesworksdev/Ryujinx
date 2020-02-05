namespace Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager
{
    struct UpdateDataHeader
    {
        public int Revision;
        public int BehaviorSize;
        public int MemoryPoolSize;
        public int VoiceSize;
        public int EffectSize;
        public int SinkSize;
        public int TotalSize;
        public int PerformanceManagerSize;
        public int ElapsedFrameCountInfoSize;
#pragma warning disable CS0649
        public int VoiceResourceSize;
        public int MixSize;
        public int Unknown24;
        public int Unknown2C;
        public int Unknown30;
        public int Unknown34;
        public int Unknown38;
#pragma warning restore CS0649
    }
}