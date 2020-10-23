using Ryujinx.Graphics.GAL;
using System.Collections.Generic;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class SamplerCache
    {
        private readonly Dictionary<SamplerCreateInfo, Sampler> _cache = new Dictionary<SamplerCreateInfo, Sampler>();

        public Sampler GetOrCreate(SamplerCreateInfo info)
        {
            if (_cache.TryGetValue(info, out Sampler sampler))
            {
                sampler.IncrementReferenceCount();
                return sampler;
            }

            sampler = new Sampler(this, info);
            _cache.Add(info, sampler);
            return sampler;
        }

        public void Remove(Sampler sampler)
        {
            _cache.Remove(sampler.Info);
        }
    }
}
