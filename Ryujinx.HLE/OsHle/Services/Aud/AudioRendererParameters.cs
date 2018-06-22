namespace Ryujinx.HLE.OsHle.Services.Aud
{
    struct AudioRendererParameters
    {
        public int  SampleRate;
        public int  SampleCount;
        public int  Unknown0;
        public int  Unknown1;
        public int  VoiceCount;
        public int  SinkCount;
        public int  EffectCount;
        public int  Unknown2;
        public byte Unknown3;
        public byte Padding0;
        public byte Padding1;
        public byte Padding2;
        public int  SplitterCount;
        public int  Unknown4;
        public int  Padding3;
        public int  Magic;
    }
}
