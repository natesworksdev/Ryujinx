namespace Ryujinx.Audio.Renderer.Server.Upsampler
{
    public struct UpsamplerBufferState
    {
        public const int HistoryLength = 20;

        public float Scale;
        public float[] History = new float[HistoryLength];
        public int CurrentIndex = 0;
        public bool Initialized = false;
        public int Phase = 0;

        public UpsamplerBufferState()
        {
        }
    }
}