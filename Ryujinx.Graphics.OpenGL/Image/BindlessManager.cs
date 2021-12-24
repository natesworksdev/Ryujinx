using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.OpenGL.Image
{
    /// <summary>
    /// Host bindless texture manager.
    /// </summary>
    class BindlessManager
    {
        private struct IdList
        {
            public int Id { get; }
            public HashSet<int> Others { get; }

            public IdList(int id)
            {
                Id = id;
                Others = null;
            }

            public IdList(HashSet<int> others)
            {
                Id = 0;
                Others = others;
            }
        }

        private readonly Renderer _renderer;
        private BindlessHandleManager _handleManager;
        private readonly Dictionary<TextureView, IdList> _textures;
        private readonly Dictionary<Sampler, IdList> _samplers;

        private readonly HashSet<long> _handles = new HashSet<long>();

        public BindlessManager(Renderer renderer)
        {
            _renderer = renderer;
            _textures = new Dictionary<TextureView, IdList>();
            _samplers = new Dictionary<Sampler, IdList>();
        }

        public void Add(int textureId, ITexture texture, int samplerId, ISampler sampler)
        {
            EnsureHandleManager();
            Register(textureId, samplerId, texture as TextureBase, sampler as Sampler);
        }

        private void Register(int textureId, int samplerId, TextureBase texture, Sampler sampler)
        {
            long bindlessHandle = sampler != null
                ? GetTextureSamplerHandle(texture.Handle, sampler.Handle)
                : GetTextureHandle(texture.Handle);

            if (bindlessHandle != 0 && texture.AddBindlessHandle(textureId, samplerId, this, bindlessHandle))
            {
                _handles.Add(bindlessHandle);
                // System.Console.WriteLine($"Register {textureId} {samplerId} 0x{bindlessHandle:X} {texture.Info.Width}x{texture.Info.Height} {texture.Info.Target} {texture.Format} | {_handles.Count}");
                MakeTextureHandleResident(bindlessHandle);
                _handleManager.AddBindlessHandle(textureId, samplerId, bindlessHandle, texture.ScaleFactor);
            }
        }

        public void Unregister(int textureId, int samplerId, long bindlessHandle)
        {
            // System.Console.WriteLine($"Unregister {textureId} {samplerId} 0x{bindlessHandle:X}");
            _handleManager.RemoveBindlessHandle(textureId, samplerId);
            MakeTextureHandleNonResident(bindlessHandle);
            _handles.Remove(bindlessHandle);
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
            else
            {
                return GL.Arb.GetTextureHandle(texture);
            }
        }

        private static long GetTextureSamplerHandle(int texture, int sampler)
        {
            if (HwCapabilities.SupportsNvBindlessTexture)
            {
                return GL.NV.GetTextureSamplerHandle(texture, sampler);
            }
            else
            {
                return GL.Arb.GetTextureSamplerHandle(texture, sampler);
            }
        }

        private static void MakeTextureHandleResident(long handle)
        {
            if (HwCapabilities.SupportsNvBindlessTexture)
            {
                GL.NV.MakeTextureHandleResident(handle);
            }
            else
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
            else
            {
                GL.Arb.MakeTextureHandleNonResident(handle);
            }
        }
    }
}
