namespace Ryujinx.Graphics.Nvdec
{
    public readonly struct FrameDecodedEventArgs
    {
        public ApplicationId CodecId { get; }
        public uint LumaOffset { get; }
        public uint ChromaOffset { get; }

        internal FrameDecodedEventArgs(ApplicationId codecId, uint lumaOffset, uint chromaOffset)
        {
            CodecId = codecId;
            LumaOffset = lumaOffset;
            ChromaOffset = chromaOffset;
        }
    }
}
