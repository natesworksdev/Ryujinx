using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class BindlessTexture : IDisposable
    {
        private readonly TextureBase _texture;
        private readonly HashSet<Sampler> _samplers;
        private readonly HashSet<long> _handles;

        private static long GetTextureSamplerHandle(int texture, int sampler)
        {
            return GL.NV.GetTextureSamplerHandle(texture, sampler);
        }

        private static void MakeTextureHandleResident(long handle)
        {
            GL.NV.MakeTextureHandleResident(handle);
        }

        private static void MakeTextureHandleNonResident(long handle)
        {
            GL.NV.MakeTextureHandleNonResident(handle);
        }

        public BindlessTexture(TextureBase texture)
        {
            _texture = texture;
            _samplers = new HashSet<Sampler>();
            _handles = new HashSet<long>();
            texture.IncrementReferenceCount();
        }

        public long GetHandle(ISampler sampler)
        {
            Sampler samp = (Sampler)sampler;

            if (_samplers.Add(samp))
            {
                samp.IncrementReferenceCount();
            }

            long handle = GetTextureSamplerHandle(_texture.Handle, samp.Handle);

            // Zero is never a valid handle value, it means that something went wrong.
            if (handle == 0L)
            {
                return 0L;
            }

            if (_handles.Add(handle))
            {
                MakeTextureHandleResident(handle);
            }

            return handle;
        }

        public void Dispose()
        {
            foreach (long handle in _handles)
            {
                MakeTextureHandleNonResident(handle);
            }

            _handles.Clear();
            _texture.DecrementReferenceCount();

            foreach (Sampler sampler in _samplers)
            {
                sampler.DecrementReferenceCount();
            }

            _samplers.Clear();
        }
    }
}
