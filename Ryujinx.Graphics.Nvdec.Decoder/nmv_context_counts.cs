using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public struct nmv_context_counts
    {
        public Array4<uint> joints;
        public Array2<nmv_component_counts> comps;
    }
}
