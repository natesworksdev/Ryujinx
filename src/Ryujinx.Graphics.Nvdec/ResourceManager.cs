using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Nvdec.Image;

namespace Ryujinx.Graphics.Nvdec
{
    readonly struct ResourceManager
    {
        public DeviceMemoryManager Gmm { get; }
        public SurfaceCache Cache { get; }

        public ResourceManager(DeviceMemoryManager gmm, SurfaceCache cache)
        {
            Gmm = gmm;
            Cache = cache;
        }
    }
}
