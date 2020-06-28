using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    // This must match the structure used by NVDEC, do not modify.
    public struct MvRef
    {
        public Array2<Mv> Mvs;
        public Array2<int> RefFrames;
    }
}
