using System;

namespace Ryujinx.Graphics.Shader.CodeGen.Spirv
{
    struct TextureMeta : IEquatable<TextureMeta>
    {
        public int CbufSlot { get; }
        public int Handle { get; }
        public TextureFormat Format { get; }
        public SamplerType Type { get; }

        public TextureMeta(int cbufSlot, int handle, TextureFormat format, SamplerType type)
        {
            CbufSlot = cbufSlot;
            Handle = handle;
            Format = format;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            return obj is TextureMeta other && Equals(other);
        }

        public bool Equals(TextureMeta other)
        {
            return Handle == other.Handle && Type == other.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Handle, Type);
        }
    }
}