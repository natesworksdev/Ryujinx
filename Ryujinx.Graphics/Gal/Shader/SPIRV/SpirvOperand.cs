using System;
using System.IO;
using System.Text;

namespace Ryujinx.Graphics.Gal.Shader.SPIRV
{
    public abstract class Operand
    {
        public abstract int GetWordCount();

        public abstract void Write(BinaryWriter BinaryWriter);
    }

    public class Id: Operand
    {
        private Instruction Instruction;

        public Id(Instruction Instruction)
        {
            this.Instruction = Instruction;
        }

        public override void Write(BinaryWriter BinaryWriter)
        {
            BinaryWriter.Write((uint)Instruction.ResultId);
        }

        public override int GetWordCount()
        {
            return 1;
        }
    }

    public abstract class Literal: Operand
    {
    }

    public class LiteralString: Literal
    {
        public byte[] Value;

        public LiteralString(string String)
        {
            Value = Encoding.UTF8.GetBytes(String);
        }

        public override void Write(BinaryWriter BinaryWriter)
        {
            BinaryWriter.Write(Value);

            // Write remaining zero bytes
            for (int i = 0; i < 4 - (Value.Length % 4); i++)
            {
                BinaryWriter.Write((byte)0);
            }
        }

        public override int GetWordCount()
        {
            return Value.Length / 4 + 1;
        }

        public override bool Equals(object Object)
        {
            if (Object is LiteralString Other)
            {
                return this.Value == Other.Value;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class LiteralNumber: Literal
    {
        public TypeCode Type;

        public int Integer;

        public float Float32;

        public double Float64;

        public LiteralNumber(int Value)
        {
            Integer = Value;
            Type = Value.GetTypeCode();
        }

        public LiteralNumber(float Value)
        {
            Float32 = Value;
            Type = Value.GetTypeCode();
        }

        public LiteralNumber(double Value)
        {
            Float64 = Value;
            Type = Value.GetTypeCode();
        }

        public override void Write(BinaryWriter BinaryWriter)
        {
            switch (Type)
            {
                case TypeCode.Int32:
                    BinaryWriter.Write(Integer);
                    break;

                case TypeCode.Single:
                    BinaryWriter.Write(Float32);
                    break;

                case TypeCode.Double:
                    BinaryWriter.Write(Float64);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public override int GetWordCount()
        {
            switch (Type)
            {
                case TypeCode.Int32:
                case TypeCode.Single:
                    return 1;

                case TypeCode.Double:
                    return 2;

                default:
                    throw new NotImplementedException();
            }
        }

        public override bool Equals(object Object)
        {
            if (Object is LiteralNumber Other && this.Type == Other.Type)
            {
                return this.Integer == Other.Integer
                    && this.Float32 == Other.Float32
                    && this.Float64 == Other.Float64;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() + Integer.GetHashCode()
                + Float32.GetHashCode() + Float64.GetHashCode();
        }
    }
}
