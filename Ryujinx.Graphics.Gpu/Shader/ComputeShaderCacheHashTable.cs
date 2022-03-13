using Ryujinx.Common.Cache;

namespace Ryujinx.Graphics.Gpu.Shader
{
    class ComputeShaderCacheHashTable
    {
        private readonly PartitionedHashTable<ShaderSpecializationList> _cache;

        public ComputeShaderCacheHashTable()
        {
            _cache = new PartitionedHashTable<ShaderSpecializationList>();
        }

        public void Add(CachedShaderProgram program)
        {
            var specList = _cache.GetOrAdd(program.Shaders[0].Code, new ShaderSpecializationList());
            specList.Add(program);
        }

        public bool TryFind(
            GpuChannel channel,
            GpuChannelPoolState poolState,
            ulong gpuVa,
            out CachedShaderProgram program,
            out byte[] cachedGuestCode)
        {
            program = null;
            ShaderCodeAccessor codeAccessor = new ShaderCodeAccessor(channel.MemoryManager, gpuVa);
            bool hasSpecList = _cache.TryFindItem(codeAccessor, out var specList, out cachedGuestCode);
            return hasSpecList && specList.TryFindForCompute(channel, poolState, out program);
        }
    }
}