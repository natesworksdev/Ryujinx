using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    class ShaderSpecializationList
    {
        private readonly List<CachedShaderProgram> _entries = new List<CachedShaderProgram>();

        public void Add(CachedShaderProgram program)
        {
            _entries.Add(program);
        }

        public bool TryFindForGraphics(GpuChannel channel, GpuChannelPoolState poolState, out CachedShaderProgram program)
        {
            foreach (var entry in _entries)
            {
                if (entry.SpecializationState.MatchesGraphics(channel, poolState))
                {
                    program = entry;
                    return true;
                }
            }

            program = default;
            return false;
        }

        public bool TryFindForCompute(GpuChannel channel, GpuChannelPoolState poolState, out CachedShaderProgram program)
        {
            foreach (var entry in _entries)
            {
                if (entry.SpecializationState.MatchesCompute(channel, poolState))
                {
                    program = entry;
                    return true;
                }
            }

            program = default;
            return false;
        }
    }
}