using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCache
    {
        private PipelineLayoutCacheEntry[] _plce;

        public PipelineLayoutCache()
        {
            _plce = new PipelineLayoutCacheEntry[1 << Constants.MaxShaderStages];
        }

        public PipelineLayoutCacheEntry GetOrCreate(VulkanGraphicsDevice gd, Device device, uint stages)
        {
            if (_plce[stages] == null)
            {
                _plce[stages] = new PipelineLayoutCacheEntry(gd, device, stages);
            }

            return _plce[stages];
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < _plce.Length; i++)
                {
                    _plce[i]?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
