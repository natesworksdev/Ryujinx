using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    struct DisposableFramebuffer : IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Framebuffer Value { get; }

        public DisposableFramebuffer(Vk api, Device device, Framebuffer framebuffer)
        {
            _api = api;
            _device = device;
            Value = framebuffer;
        }

        public unsafe void Dispose()
        {
            _api.DestroyFramebuffer(_device, Value, null);
        }
    }
}
