namespace Ryujinx.Audio.Renderer.Server.Upsampler
{
    public struct UpsamplerBufferState
    {
        public const int HistoryLength = 20;

        public float Scale;
        public readonly float[] History = new float[HistoryLength];
        public bool Initialized = false;
        public int Phase = 0;

        public UpsamplerBufferState()
        {
        }
    }
}