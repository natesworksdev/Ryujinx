using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    struct DisposableBufferView : System.IDisposable
    {
        private readonly Vk _api;
        private readonly Device _device;

        public BufferView Value { get; }

        public DisposableBufferView(Vk api, Device device, BufferView bufferView)
        {
            _api = api;
            _device = device;
            Value = bufferView;
        }

        public unsafe void Dispose()
        {
            _api.DestroyBufferView(_device, Value, null);
        }
    }
}
