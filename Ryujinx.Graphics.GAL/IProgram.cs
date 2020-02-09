using System;

namespace Ryujinx.Graphics.GAL
{
    public interface IProgram : IDisposable
    {
        void SetUniformBufferBindingPoint(string name, int bindingPoint);
        void SetStorageBufferBindingPoint(string name, int bindingPoint);
        void SetTextureUnit(string name, int unit);
        void SetImageUnit(string name, int unit);

        byte[] GetGpuBinary();
    }
}
