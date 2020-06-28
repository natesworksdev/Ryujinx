using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct NmvContextCounts
    {
        public Array4<uint> Joints;
        public NmvComponentCounts Comps;

        public NmvContextCounts(ref nmv_context_counts counts)
        {
            Joints = counts.joints;
            Comps = new NmvComponentCounts(ref counts.comps);
        }
    }
}
