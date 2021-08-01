using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Spv.Generator
{
    public sealed class Instruction : Operand, IEquatable<Instruction>
    {
        public const uint InvalidId = uint.MaxValue;

        public Specification.Op Opcode { get; private set; }
        private Instruction _resultType;
        public List<Operand> _operands;

        public uint Id { get; set; }

        public Instruction(Specification.Op opcode, uint id = InvalidId, Instruction resultType = null)
        {
            Opcode = opcode;
            Id = id;
            _resultType = resultType;

            _operands = new List<Operand>();
        }

        public void SetId(uint id)
        {
            Id = id;
        }

        public OperandType Type => OperandType.Instruction;

        public ushort GetTotalWordCount()
        {
            ushort result = WordCount;

            if (Id != InvalidId)
            {
                result++;
            }

            if (_resultType != null)
            {
                result += _resultType.WordCount;
            }

            foreach (Operand operand in _operands)
            {
                result += operand.WordCount;
            }

            return result;
        }

        public ushort WordCount => 1;

        private void AddOperand(Operand value)
        {
            Debug.Assert(value != null);
            _operands.Add(value);
        }

        public void AddOperand(Operand[] value)
        {
            foreach (Operand instruction in value)
            {
                AddOperand(instruction);
            }
        }

        public void AddOperand(LiteralInteger[] value)
        {
            foreach (LiteralInteger instruction in value)
            {
                AddOperand(instruction);
            }
        }

        public void AddOperand(LiteralInteger value)
        {
            AddOperand((Operand)value);
        }

        public void AddOperand(Instruction[] value)
        {
            foreach (Instruction instruction in value)
            {
                AddOperand(instruction);
            }
        }

        public void AddOperand(Instruction value)
        {
            AddOperand((Operand)value);
        }

        public void AddOperand(string value)
        {
            AddOperand(new LiteralString(value));
        }

        public void AddOperand<T>(T value) where T: struct
        {
            if (!typeof(T).IsPrimitive && !typeof(T).IsEnum)
            {
                throw new InvalidOperationException();
            }

            AddOperand(LiteralInteger.CreateForEnum(value));
        }

        public void Write(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            // Word 0
            writer.Write((ushort)Opcode);
            writer.Write(GetTotalWordCount());

            _resultType?.WriteOperand(stream);

            if (Id != InvalidId)
            {
                writer.Write(Id);
            }

            foreach (Operand operand in _operands)
            {
                operand.WriteOperand(stream);
            }
        }

        public void WriteOperand(Stream stream)
        {
            Debug.Assert(Id != InvalidId);

            if (Id == InvalidId)
            {
                string methodToCall;

                if (Opcode == Specification.Op.OpVariable)
                {
                    methodToCall = "AddLocalVariable or AddGlobalVariable";
                }
                else if (Opcode == Specification.Op.OpLabel)
                {
                    methodToCall = "AddLabel";
                }
                else
                {
                    throw new InvalidOperationException("Internal error");
                }

                throw new InvalidOperationException($"Id wasn't bound to the module, please make sure to call {methodToCall}");
            }

            stream.Write(BitConverter.GetBytes(Id));
        }

        public override bool Equals(object obj)
        {
            return obj is Instruction instruction && Equals(instruction);
        }

        public bool Equals(Instruction cmpObj)
        {
            bool result = Type == cmpObj.Type && Id == cmpObj.Id;

            if (result)
            {
                if (_resultType != null && cmpObj._resultType != null)
                {
                    result &= _resultType.Equals(cmpObj._resultType);
                }
                else if (_resultType != null || cmpObj._resultType != null)
                {
                    return false;
                }
            }

            if (result)
            {
                result &= EqualsContent(cmpObj);
            }

            return result;
        }

        public bool EqualsResultType(Instruction cmpObj)
        {
            return _resultType.Opcode == cmpObj._resultType.Opcode && _resultType.EqualsContent(cmpObj._resultType);
        }

        public bool EqualsContent(Instruction cmpObj)
        {
            return _operands.SequenceEqual(cmpObj._operands);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Opcode, Id, _resultType, _operands);
        }

        public bool Equals(Operand obj)
        {
            return obj is Instruction instruction && Equals(instruction);
        }
    }
}
