namespace Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager
{
    struct UpdateDataHeader
    {
        public int Revision;
        public int BehaviorSize;
        public int MemoryPoolSize;
        public int VoiceSize;
#pragma warning disable CS0649
        public int VoiceResourceSize;
#pragma warning restore CS0649
        public int EffectSize;
#pragma warning disable CS0649
        public int MixSize;
#pragma warning restore CS0649
        public int SinkSize;
        public int PerformanceManagerSize;
#pragma warning disable CS0649
        public int Unknown24;
#pragma warning restore CS0649
        public int ElapsedFrameCountInfoSize;
#pragma warning disable CS0649
        public int Unknown2C;
        public int Unknown30;
        public int Unknown34;
        public int Unknown38;
#pragma warning restore CS0649
        public int TotalSize;
    }
}