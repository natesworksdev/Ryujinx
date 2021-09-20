using Ryujinx.Graphics.Nvdec.Types.Vp9;

namespace Ryujinx.Graphics.Nvdec
{
    struct NvdecStatus
    {
        public uint MbsCorrectlyDecoded;
        public uint MbsInError;
        public uint Reserved;
        public uint ErrorStatus;
        public FrameStats Stats;
        public uint SliceHeaderErrorCode;
    }
}