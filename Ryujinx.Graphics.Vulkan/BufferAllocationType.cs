namespace Ryujinx.Graphics.Vulkan
{
    internal enum BufferAllocationType
    {
        Auto = 0,

        HostMapped,
        DeviceLocal,
        DeviceLocalMapped
    }
}
