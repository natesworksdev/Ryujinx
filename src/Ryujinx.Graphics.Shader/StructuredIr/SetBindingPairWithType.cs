using System;

namespace Ryujinx.Graphics.Shader
{
    readonly struct SetBindingPairWithType : IEquatable<SetBindingPairWithType>
    {
        public int Set { get; }
        public int Binding { get; }
        public SamplerType Type { get; }

        public SetBindingPairWithType(int set, int binding, SamplerType type)
        {
            Set = set;
            Binding = binding;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(SetBindingPairWithType other)
        {
            return other.Set == Set && other.Binding == Binding && other.Type == Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Set, Binding, Type);
        }

        public int Pack()
        {
            return Pack(Set, Binding);
        }

        public static int Pack(int set, int binding)
        {
            return (ushort)set | (checked((ushort)binding) << 16);
        }

        public static SetBindingPairWithType Unpack(int packed, SamplerType type)
        {
            return new((ushort)packed, (ushort)((uint)packed >> 16), type & ~(SamplerType.Shadow | SamplerType.Separate));
        }
    }
}
