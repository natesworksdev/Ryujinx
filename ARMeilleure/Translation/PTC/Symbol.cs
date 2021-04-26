using System;

namespace ARMeilleure.Translation.PTC
{
    struct Symbol
    {
        private readonly ulong _value;

        public SymbolType Type { get; }
        public ulong Value
        {
            get
            {
                if (Type == SymbolType.None)
                {
                    ThrowSymbolNone();
                }

                return _value;
            }
        }

        public Symbol(SymbolType type, ulong value)
        {
            (Type, _value) = (type, value);
        }

        public static bool operator ==(Symbol a, Symbol b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(Symbol a, Symbol b)
        {
            return !(a == b);
        }

        public bool Equals(Symbol other)
        {
            return other.Type == Type && other._value == _value;
        }

        public override bool Equals(object obj)
        {
            return obj is Symbol sym && Equals(sym);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, _value);
        }

        public override string ToString()
        {
            return $"{Type}:{_value}";
        }

        private static void ThrowSymbolNone()
        {
            throw new InvalidOperationException("Symbol refers to nothing.");
        }
    }
}
