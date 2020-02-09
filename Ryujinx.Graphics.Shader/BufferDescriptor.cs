using System;

namespace Ryujinx.Graphics.Shader
{
    [Serializable]
    public struct BufferDescriptor
    {
        public string Name { get; }

        public int Slot { get; }

        public BufferDescriptor(string name, int slot)
        {
            Name = name;
            Slot = slot;
        }
    }
}