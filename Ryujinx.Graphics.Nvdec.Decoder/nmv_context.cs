using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct nmv_context
    {
        public Array3<byte> joints;
        public Array2<nmv_component> comps;
    }
}
