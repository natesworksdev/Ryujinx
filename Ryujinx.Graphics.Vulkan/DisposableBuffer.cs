using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    struct DisposableBuffer : System.IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public Buffer Value { get; }

        public DisposableBuffer(Vk api, Device device, Buffer buffer)
        {
            _api = api;
            _device = device;
            Value = buffer;
        }

        public unsafe void Dispose()
        {
            _api.DestroyBuffer(_device, Value, null);
        }
    }
}
