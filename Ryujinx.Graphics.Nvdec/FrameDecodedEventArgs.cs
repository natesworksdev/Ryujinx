namespace Ryujinx.Graphics.Nvdec
{
    public readonly struct FrameDecodedEventArgs
    {
        public ApplicationId ApplicationId { get; }
        public uint LumaOffset { get; }
        public uint ChromaOffset { get; }

        internal FrameDecodedEventArgs(ApplicationId applicationId, uint lumaOffset, uint chromaOffset)
        {
            ApplicationId = applicationId;
            LumaOffset = lumaOffset;
            ChromaOffset = chromaOffset;
        }
    }
}
