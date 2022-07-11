using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Ryujinx.Ava.Ui.Backend.Vulkan;
using Ryujinx.Ava.Ui.Vulkan;
using Ryujinx.Common.Configuration;
using Ryujinx.Graphics.Vulkan;
using Silk.NET.Vulkan;
using SkiaSharp;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class VulkanRendererControl : RendererControl
    {
        private VulkanPlatformInterface _platformInterface;

        public VulkanRendererControl(GraphicsDebugLevel graphicsDebugLevel) : base(graphicsDebugLevel)
        {
            _platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
        }

        public override void DestroyBackgroundContext()
        {

        }

        protected override ICustomDrawOperation CreateDrawOperation()
        {
            return new VulkanDrawOperation(this);
        }

        protected override void CreateWindow()
        {
        }

        internal override void MakeCurrent()
        {
        }

        internal override void MakeCurrent(SwappableNativeWindowBase window)
        {
        }

        internal override void Present(object image)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Image = image;
            }).Wait();

            QueueRender();
        }

        private class VulkanDrawOperation : ICustomDrawOperation
        {
            public Rect Bounds { get; }

            private readonly VulkanRendererControl _control;

            public VulkanDrawOperation(VulkanRendererControl control)
            {
                _control = control;
                Bounds = _control.Bounds;
            }

            public unsafe void Dispose()
            {
                
            }

            public bool Equals(ICustomDrawOperation other)
            {
                return other is VulkanDrawOperation operation && Equals(this, operation) && operation.Bounds == Bounds;
            }

            public bool HitTest(Point p)
            {
                return Bounds.Contains(p);
            }

            public unsafe void Render(IDrawingContextImpl context)
            {
                if (_control.Image == null || _control.RenderSize.Width == 0 || _control.RenderSize.Height == 0 || context is not ISkiaDrawingContextImpl skiaDrawingContextImpl)
                {
                    return;
                }

                var image = (PresentImageInfo)_control.Image;

                try
                {

                    if (!image.State.IsValid)
                    {
                        return;
                    }

                    var commandBuffer = _control._platformInterface.Device.CommandBufferPool.CreateCommandBuffer();

                    commandBuffer.BeginRecording();

                    image.GetImage(_control._platformInterface.Device.InternalHandle, _control._platformInterface.PhysicalDevice.InternalHandle, commandBuffer.InternalHandle, out var currentImage, out var memory);

                    commandBuffer.Submit();

                    _control._platformInterface.Device.QueueWaitIdle();

                    var gpu = AvaloniaLocator.Current.GetService<VulkanSkiaGpu>();

                    var imageInfo = new GRVkImageInfo()
                    {
                        CurrentQueueFamily = _control._platformInterface.PhysicalDevice.QueueFamilyIndex,
                        Format = (uint)Format.R8G8B8A8Unorm,
                        Image = currentImage.Handle,
                        ImageLayout = (uint)ImageLayout.ColorAttachmentOptimal,
                        ImageTiling = (uint)ImageTiling.Optimal,
                        ImageUsageFlags = (uint)(ImageUsageFlags.ImageUsageColorAttachmentBit
                                                 | ImageUsageFlags.ImageUsageTransferSrcBit
                                                 | ImageUsageFlags.ImageUsageTransferDstBit),
                        LevelCount = 1,
                        SampleCount = 1,
                        Protected = false,
                        Alloc = new GRVkAlloc()
                        {
                            Memory = memory.Handle,
                            Flags = 0,
                            Offset = image.MemoryOffset,
                            Size = image.MemorySize
                        }
                    };

                    using var backendTexture = new GRBackendRenderTarget(
                        (int)image.Extent.Width,
                        (int)image.Extent.Height,
                        1,
                        imageInfo);

                    using var surface = SKSurface.Create(
                        gpu.GrContext,
                        backendTexture,
                        GRSurfaceOrigin.TopLeft,
                        SKColorType.Rgba8888);

                    if (surface == null)
                    {
                        return;
                    }

                    var rect = new Rect(new Point(), new Size(image.Extent.Width, image.Extent.Height));

                    using var snapshot = surface.Snapshot();
                    skiaDrawingContextImpl.SkCanvas.DrawImage(snapshot, rect.ToSKRect(), _control.Bounds.ToSKRect(), new SKPaint());
                }
                finally
                {
                    image.CompletionAction();
                }
            }
        }
    }
}
