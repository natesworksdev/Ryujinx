using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    class DrawState
    {
        public int FirstIndex;
        public int IndexCount;
        public bool DrawIndexed;
        public bool VsUsesInstanceId;
        public bool IsAnyVbInstanced;
        public PrimitiveTopology Topology;
        public IbStreamer IbStreamer = new IbStreamer();
    }
}
