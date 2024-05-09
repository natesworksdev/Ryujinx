using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.OpenGL
{
    class Framebuffer : IDisposable
    {
        public uint Handle { get; private set; }
        private uint _clearFbHandle;
        private bool _clearFbInitialized;

        private FramebufferAttachment _lastDsAttachment;

        private readonly TextureView[] _colors;
        private TextureView _depthStencil;

        private int _colorsCount;
        private bool _dualSourceBlend;
        private GL _api;

        public Framebuffer(GL api)
        {
            _api = api;

            Handle = _api.GenFramebuffer();
            _clearFbHandle = _api.GenFramebuffer();

            _colors = new TextureView[8];
        }

        public uint Bind()
        {
            _api.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
            return Handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AttachColor(int index, TextureView color)
        {
            if (_colors[index] == color)
            {
                return;
            }

            FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0 + index;

            _api.FramebufferTexture(FramebufferTarget.Framebuffer, attachment, color?.Handle ?? 0, 0);

            _colors[index] = color;
        }

        public void AttachDepthStencil(TextureView depthStencil)
        {
            // Detach the last depth/stencil buffer if there is any.
            if (_lastDsAttachment != 0)
            {
                _api.FramebufferTexture(FramebufferTarget.Framebuffer, _lastDsAttachment, 0, 0);
            }

            if (depthStencil != null)
            {
                FramebufferAttachment attachment = GetAttachment(depthStencil.Format);

                _api.FramebufferTexture(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    depthStencil.Handle,
                    0);

                _lastDsAttachment = attachment;
            }
            else
            {
                _lastDsAttachment = 0;
            }

            _depthStencil = depthStencil;
        }

        public void SetDualSourceBlend(bool enable)
        {
            bool oldEnable = _dualSourceBlend;

            _dualSourceBlend = enable;

            // When dual source blend is used,
            // we can only have one draw buffer.
            if (enable)
            {
                _api.DrawBuffer(DrawBufferMode.ColorAttachment0);
            }
            else if (oldEnable)
            {
                SetDrawBuffersImpl(_api, _colorsCount);
            }
        }

        public void SetDrawBuffers(int colorsCount)
        {
            if (_colorsCount != colorsCount && !_dualSourceBlend)
            {
                SetDrawBuffersImpl(_api, colorsCount);
            }

            _colorsCount = colorsCount;
        }

        private static void SetDrawBuffersImpl(GL api, int colorsCount)
        {
            DrawBufferMode[] drawBuffers = new DrawBufferMode[colorsCount];

            for (int index = 0; index < colorsCount; index++)
            {
                drawBuffers[index] = DrawBufferMode.ColorAttachment0 + index;
            }

            api.DrawBuffers(colorsCount, drawBuffers);
        }

        private static FramebufferAttachment GetAttachment(Format format)
        {
            if (FormatTable.IsPackedDepthStencil(format))
            {
                return FramebufferAttachment.DepthStencilAttachment;
            }
            else if (FormatTable.IsDepthOnly(format))
            {
                return FramebufferAttachment.DepthAttachment;
            }
            else
            {
                return FramebufferAttachment.StencilAttachment;
            }
        }

        public int GetColorLayerCount(int index)
        {
            return _colors[index]?.Info.GetDepthOrLayers() ?? 0;
        }

        public int GetDepthStencilLayerCount()
        {
            return _depthStencil?.Info.GetDepthOrLayers() ?? 0;
        }

        public void AttachColorLayerForClear(int index, int layer)
        {
            TextureView color = _colors[index];

            if (!IsLayered(color))
            {
                return;
            }

            BindClearFb();
            _api.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + index, color.Handle, 0, layer);
        }

        public void DetachColorLayerForClear(int index)
        {
            TextureView color = _colors[index];

            if (!IsLayered(color))
            {
                return;
            }

            _api.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + index, 0, 0);
            Bind();
        }

        public void AttachDepthStencilLayerForClear(int layer)
        {
            TextureView depthStencil = _depthStencil;

            if (!IsLayered(depthStencil))
            {
                return;
            }

            BindClearFb();
            _api.FramebufferTextureLayer(FramebufferTarget.Framebuffer, GetAttachment(depthStencil.Format), depthStencil.Handle, 0, layer);
        }

        public void DetachDepthStencilLayerForClear()
        {
            TextureView depthStencil = _depthStencil;

            if (!IsLayered(depthStencil))
            {
                return;
            }

            _api.FramebufferTexture(FramebufferTarget.Framebuffer, GetAttachment(depthStencil.Format), 0, 0);
            Bind();
        }

        private void BindClearFb()
        {
            _api.BindFramebuffer(FramebufferTarget.Framebuffer, _clearFbHandle);

            if (!_clearFbInitialized)
            {
                SetDrawBuffersImpl(_api, Constants.MaxRenderTargets);
                _clearFbInitialized = true;
            }
        }

        private static bool IsLayered(TextureView view)
        {
            return view != null &&
                   view.Target != Target.Texture1D &&
                   view.Target != Target.Texture2D &&
                   view.Target != Target.Texture2DMultisample &&
                   view.Target != Target.TextureBuffer;
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                _api.DeleteFramebuffer(Handle);

                Handle = 0;
            }

            if (_clearFbHandle != 0)
            {
                _api.DeleteFramebuffer(_clearFbHandle);

                _clearFbHandle = 0;
            }
        }
    }
}
