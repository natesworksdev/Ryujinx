using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using System.Collections.Generic;

namespace Ryujinx.Graphics.OpenGL.Image
{
    /// <summary>
    /// Host bindless texture manager.
    /// </summary>
    class BindlessManager
    {
        private readonly OpenGLRenderer _renderer;
        private BindlessHandleManager _handleManager;
        private readonly Dictionary<int, (ITexture, float)> _separateTextures;
        private readonly Dictionary<int, ISampler> _separateSamplers;

        private readonly HashSet<long> _handles = new HashSet<long>();

        public BindlessManager(OpenGLRenderer renderer)
        {
            _renderer = renderer;
            _separateTextures = new();
            _separateSamplers = new();
        }

        public void AddSeparateSampler(int samplerId, ISampler sampler)
        {
            _separateSamplers[samplerId] = sampler;

            foreach ((int textureId, (ITexture texture, float textureScale)) in _separateTextures)
            {
                Add(textureId, texture, textureScale, samplerId, sampler);
            }
        }

        public void AddSeparateTexture(int textureId, ITexture texture, float textureScale)
        {
            _separateTextures[textureId] = (texture, textureScale);

            bool hasDeletedSamplers = false;

            foreach ((int samplerId, ISampler sampler) in _separateSamplers)
            {
                if ((sampler as Sampler).Handle == 0)
                {
                    hasDeletedSamplers = true;
                    continue;
                }

                Add(textureId, texture, textureScale, samplerId, sampler);
            }

            if (hasDeletedSamplers)
            {
                List<int> toRemove = new List<int>();

                foreach ((int samplerId, ISampler sampler) in _separateSamplers)
                {
                    if ((sampler as Sampler).Handle == 0)
                    {
                        toRemove.Add(samplerId);
                    }
                }

                foreach (int samplerId in toRemove)
                {
                    _separateSamplers.Remove(samplerId);
                }
            }
        }

        public void Add(int textureId, ITexture texture, float textureScale, int samplerId, ISampler sampler)
        {
            EnsureHandleManager();
            Register(textureId, samplerId, texture as TextureBase, sampler as Sampler, textureScale);
        }

        private void Register(int textureId, int samplerId, TextureBase texture, Sampler sampler, float textureScale)
        {
            if (texture == null)
            {
                return;
            }

            long bindlessHandle = sampler != null
                ? GetTextureSamplerHandle(texture.Handle, sampler.Handle)
                : GetTextureHandle(texture.Handle);

            if (bindlessHandle != 0 && texture.AddBindlessHandle(textureId, samplerId, this, bindlessHandle))
            {
                _handles.Add(bindlessHandle);
                MakeTextureHandleResident(bindlessHandle);
                _handleManager.AddBindlessHandle(textureId, samplerId, bindlessHandle, textureScale);
            }
        }

        public void Unregister(int textureId, int samplerId, long bindlessHandle)
        {
            _handleManager.RemoveBindlessHandle(textureId, samplerId);
            MakeTextureHandleNonResident(bindlessHandle);
            _handles.Remove(bindlessHandle);
            _separateTextures.Remove(textureId);
        }

        private void EnsureHandleManager()
        {
            if (_handleManager == null)
            {
                _handleManager = new BindlessHandleManager(_renderer);
                _handleManager.Bind(_renderer);
            }
        }

        private static long GetTextureHandle(int texture)
        {
            if (HwCapabilities.SupportsNvBindlessTexture)
            {
                return GL.NV.GetTextureHandle(texture);
            }
            else if (HwCapabilities.SupportsArbBindlessTexture)
            {
                return GL.Arb.GetTextureHandle(texture);
            }

            return 0;
        }

        private static long GetTextureSamplerHandle(int texture, int sampler)
        {
            if (HwCapabilities.SupportsNvBindlessTexture)
            {
                return GL.NV.GetTextureSamplerHandle(texture, sampler);
            }
            else if (HwCapabilities.SupportsArbBindlessTexture)
            {
                return GL.Arb.GetTextureSamplerHandle(texture, sampler);
            }

            return 0;
        }

        private static void MakeTextureHandleResident(long handle)
        {
            if (HwCapabilities.SupportsNvBindlessTexture)
            {
                GL.NV.MakeTextureHandleResident(handle);
            }
            else if (HwCapabilities.SupportsArbBindlessTexture)
            {
                GL.Arb.MakeTextureHandleResident(handle);
            }
        }

        private static void MakeTextureHandleNonResident(long handle)
        {
            if (HwCapabilities.SupportsNvBindlessTexture)
            {
                GL.NV.MakeTextureHandleNonResident(handle);
            }
            else if (HwCapabilities.SupportsArbBindlessTexture)
            {
                GL.Arb.MakeTextureHandleNonResident(handle);
            }
        }
    }
}
