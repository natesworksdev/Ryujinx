namespace Ryujinx.Graphics.VideoDecoding
{
    unsafe struct FFmpegFrame
    {
        public int Width;
        public int Height;

        public byte* LumaPtr;
        public byte* ChromaRPtr;
        public byte* ChromaBPtr;
    }
}