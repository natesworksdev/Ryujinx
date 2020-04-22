using System;

namespace ARMeilleure.IntermediateRepresentation
{
    readonly struct Register : IEquatable<Register>
    {
        public readonly int Index { get; }

        public readonly RegisterType Type { get; }

        public Register(int index, RegisterType type)
        {
            Index = index;
            Type  = type;
        }

        public override readonly int GetHashCode()
        {
            return (ushort)Index | ((int)Type << 16);
        }

        public static bool operator ==(Register x, Register y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Register x, Register y)
        {
            return !x.Equals(y);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is Register reg && Equals(reg);
        }

        public readonly bool Equals(Register other)
        {
            return other.Index == Index &&
                   other.Type  == Type;
        }
    }
}