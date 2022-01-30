using System;
using System.IO;
using System.Text;

namespace Spv.Generator
{
    public class LiteralString : Operand, IEquatable<LiteralString>
    {
        public OperandType Type => OperandType.String;

        private string _value;

        public LiteralString(string value)
        {
            _value = value;
        }

        public ushort WordCount => (ushort)(_value.Length / 4 + 1);

        public void WriteOperand(Stream stream)
        {
            byte[] rawValue = Encoding.ASCII.GetBytes(_value);

            stream.Write(rawValue);

            int paddingSize = 4 - (rawValue.Length % 4);

            stream.Write(new byte[paddingSize]);
        }

        public override bool Equals(object obj)
        {
            return obj is LiteralString literalString && Equals(literalString);
        }

        public bool Equals(LiteralString cmpObj)
        {
            return Type == cmpObj.Type && _value.Equals(cmpObj._value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, _value);
        }

        public bool Equals(Operand obj)
        {
            return obj is LiteralString literalString && Equals(literalString);
        }
    }
}
