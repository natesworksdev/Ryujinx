using System;

namespace Ryujinx.Graphics.Shader
{
    readonly struct SetBindingPair : IEquatable<SetBindingPair>
    {
        public int Set { get; }
        public int Binding { get; }

        public SetBindingPair(int set, int binding)
        {
            Set = set;
            Binding = binding;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(SetBindingPair other)
        {
            return other.Set == Set && other.Binding == Binding;
        }

        public override int GetHashCode()
        {
            return ((uint)Set | (ulong)(uint)Binding << 32).GetHashCode();
        }

        public int Pack()
        {
            return Pack(Set, Binding);
        }

        public static int Pack(int set, int binding)
        {
            return (ushort)set | (checked((ushort)binding) << 16);
        }

        public static SetBindingPair Unpack(int packed)
        {
            return new((ushort)packed, (ushort)((uint)packed >> 16));
        }
    }
}
