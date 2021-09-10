using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.OpenGL
{
    class Framebuffer : IDisposable
    {
        public int Handle { get; private set; }

        private FramebufferAttachment _lastDsAttachment;

        private readonly TextureView[] _colors;
        private TextureView _depthStencil;

        private int _colorsCount;
        private bool _dualSourceBlend;
        private bool _needsLayersValidation;

        public Framebuffer()
        {
            Handle = GL.GenFramebuffer();

            _colors = new TextureView[8];
        }

        public int Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
            return Handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AttachColor(int index, TextureView color, bool layered = true)
        {
            if (_colors[index] == color && layered)
            {
                return;
            }

            FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0 + index;

            if (color != null)
            {
                if (layered)
                {
                    if (color.Layered)
                    {
                        _needsLayersValidation = true;
                    }

                    GL.FramebufferTexture(FramebufferTarget.Framebuffer, attachment, color.Handle, 0);
                }
                else
                {
                    GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, attachment, color.Handle, 0, 0);
                }
            }
            else
            {
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, attachment, 0, 0);
            }

            _colors[index] = color;
        }

        public void AttachDepthStencil(TextureView depthStencil, bool layered = true)
        {
            // Detach the last depth/stencil buffer if there is any.
            if (_lastDsAttachment != 0)
            {
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, _lastDsAttachment, 0, 0);
            }

            if (depthStencil != null)
            {
                FramebufferAttachment attachment;

                if (IsPackedDepthStencilFormat(depthStencil.Format))
                {
                    attachment = FramebufferAttachment.DepthStencilAttachment;
                }
                else if (IsDepthOnlyFormat(depthStencil.Format))
                {
                    attachment = FramebufferAttachment.DepthAttachment;
                }
                else
                {
                    attachment = FramebufferAttachment.StencilAttachment;
                }

                if (layered)
                {
                    if (depthStencil.Layered)
                    {
                        _needsLayersValidation = true;
                    }

                    GL.FramebufferTexture(FramebufferTarget.Framebuffer, attachment, depthStencil.Handle, 0);
                }
                else
                {
                    GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, attachment, depthStencil.Handle, 0, 0);
                }

                _lastDsAttachment = attachment;
            }
            else
            {
                _lastDsAttachment = 0;
            }

            _depthStencil = depthStencil;
        }

        public void Validate()
        {
            if (!_needsLayersValidation)
            {
                return;
            }

            // If we have mismatching targets, then we need to force
            // the layered attachments to use only the base layer.
            _needsLayersValidation = false;

            Target prev = default;
            bool targetsMatch = true;
            bool initialized = false;

            for (int i = 0; i < _colors.Length + 1; i++)
            {
                TextureView view = i < _colors.Length ? _colors[i] : _depthStencil;
                if (view == null)
                {
                    continue;
                }

                if (initialized)
                {
                    targetsMatch &= prev == view.Target;
                }
                else
                {
                    prev = view.Target;
                    initialized = true;
                }
            }

            if (!targetsMatch)
            {
                for (int i = 0; i < _colors.Length; i++)
                {
                    TextureView color = _colors[i];
                    if (color == null || !color.Layered)
                    {
                        continue;
                    }

                    AttachColor(i, color, false);
                }

                if (_depthStencil != null && _depthStencil.Layered)
                {
                    AttachDepthStencil(_depthStencil, false);
                }
            }
        }

        public void SetDualSourceBlend(bool enable)
        {
            bool oldEnable = _dualSourceBlend;

            _dualSourceBlend = enable;

            // When dual source blend is used,
            // we can only have one draw buffer.
            if (enable)
            {
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            }
            else if (oldEnable)
            {
                SetDrawBuffersImpl(_colorsCount);
            }
        }

        public void SetDrawBuffers(int colorsCount)
        {
            if (_colorsCount != colorsCount && !_dualSourceBlend)
            {
                SetDrawBuffersImpl(colorsCount);
            }

            _colorsCount = colorsCount;
        }

        private void SetDrawBuffersImpl(int colorsCount)
        {
            DrawBuffersEnum[] drawBuffers = new DrawBuffersEnum[colorsCount];

            for (int index = 0; index < colorsCount; index++)
            {
                drawBuffers[index] = DrawBuffersEnum.ColorAttachment0 + index;
            }

            GL.DrawBuffers(colorsCount, drawBuffers);
        }

        private static bool IsPackedDepthStencilFormat(Format format)
        {
            return format == Format.D24UnormS8Uint ||
                   format == Format.D32FloatS8Uint;
        }

        private static bool IsDepthOnlyFormat(Format format)
        {
            return format == Format.D16Unorm ||
                   format == Format.D24X8Unorm ||
                   format == Format.D32Float;
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteFramebuffer(Handle);

                Handle = 0;
            }
        }
    }
}
