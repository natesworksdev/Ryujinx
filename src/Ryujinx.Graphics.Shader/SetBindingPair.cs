using System;

namespace Ryujinx.Graphics.Shader
{
    public readonly struct SetBindingPair : IEquatable<SetBindingPair>
    {
        public readonly int SetIndex;
        public readonly int Binding;

        public SetBindingPair(int setIndex, int binding)
        {
            SetIndex = setIndex;
            Binding = binding;
        }

        public override bool Equals(object obj)
        {
            return obj is SetBindingPair other && Equals(other);
        }

        public bool Equals(SetBindingPair other)
        {
            return SetIndex == other.SetIndex && Binding == other.Binding;
        }

        public override int GetHashCode()
        {
            return (((ulong)(uint)SetIndex << 32) | (uint)Binding).GetHashCode();
        }

        public static bool operator ==(SetBindingPair left, SetBindingPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SetBindingPair left, SetBindingPair right)
        {
            return !(left == right);
        }
    }
}
