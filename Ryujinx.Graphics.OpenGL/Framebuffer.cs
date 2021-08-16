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

        private int _colorsCount;
        private bool _dualSourceBlend;

        public unsafe static int Create()
        {
            int localHandle = 0;

            if (HwCapabilities.Vendor == HwCapabilities.GpuVendor.Nvidia)
            {
                // Nvidia bug, cf. https://forums.developer.nvidia.com/t/bug-spec-violation-checknamedframebufferstatus-returns-gl-framebuffer-incomplete-dimensions-ext-under-gl-4-5-core/58647
                GL.GenFramebuffers(1, &localHandle);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, localHandle);
            }
            else
            {
                GL.CreateFramebuffers(1, &localHandle);
            }

            return localHandle;
        }

        public Framebuffer()
        {
            Handle = Create();

            _colors = new TextureView[8];
        }

        public int Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
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

            GL.NamedFramebufferTexture(Handle, attachment, color?.Handle ?? 0, 0);

            _colors[index] = color;
        }

        public void AttachDepthStencil(TextureView depthStencil)
        {
            // Detach the last depth/stencil buffer if there is any.
            if (_lastDsAttachment != 0)
            {
                GL.NamedFramebufferTexture(Handle, _lastDsAttachment, 0, 0);
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

                GL.NamedFramebufferTexture(
                    Handle,
                    attachment,
                    depthStencil.Handle,
                    0);

                _lastDsAttachment = attachment;
            }
            else
            {
                _lastDsAttachment = 0;
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
                GL.NamedFramebufferDrawBuffer(Handle, DrawBufferMode.ColorAttachment0);
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

            GL.NamedFramebufferDrawBuffers(Handle, colorsCount, drawBuffers);
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
