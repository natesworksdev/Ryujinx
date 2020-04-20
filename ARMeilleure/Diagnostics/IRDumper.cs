using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARMeilleure.Diagnostics
{
    struct IRDumper
    {
        private const string Indentation = " ";

        private int _indentLevel;

        private readonly StringBuilder Builder;

        private readonly Dictionary<Operand, string> _localNames;
        private readonly Dictionary<ulong, string> _symbolNames;

        private IRDumper(int indent)
        {
            _indentLevel = indent;

            Builder = new StringBuilder();

            _localNames = new Dictionary<Operand, string>();
            _symbolNames = new Dictionary<ulong, string>();
        }

        private void Indent()
        {
            Builder.EnsureCapacity(Builder.Capacity + _indentLevel * Indentation.Length);

            for (int index = 0; index < _indentLevel; index++)
            {
                Builder.Append(Indentation);
            }
        }

        private void IncreaseIndentation()
        {
            _indentLevel++;
        }

        private void DecreaseIndentation()
        {
            _indentLevel--;
        }

        private void DumpBlockName(BasicBlock block)
        {
            Builder.Append("block").Append(block.Index);
        }

        private void DumpBlockHeader(BasicBlock block)
        {
            DumpBlockName(block);

            if (block.Next != null)
            {
                Builder.Append(" (next ");
                DumpBlockName(block.Next);
                Builder.Append(')');
            }

            if (block.Branch != null)
            {
                Builder.Append(" (branch ");
                DumpBlockName(block.Branch);
                Builder.Append(')');
            }

            Builder.Append(':');
        }

        private void DumpOperand(Operand operand)
        {
            if (operand == null)
            {
                Builder.Append("<NULL>");
                return;
            }

            Builder.Append(GetTypeName(operand.Type)).Append(' ');

            switch (operand.Kind)
            {
                case OperandKind.LocalVariable:
                    if (!_localNames.TryGetValue(operand, out string localName))
                    {
                        localName = $"%{_localNames.Count}";

                        _localNames.Add(operand, localName);
                    }

                    Builder.Append(localName);
                    break;

                case OperandKind.Register:
                    Register reg = operand.GetRegister();

                    switch (reg.Type)
                    {
                        case RegisterType.Flag:    Builder.Append('b'); break;
                        case RegisterType.FpFlag:  Builder.Append('f'); break;
                        case RegisterType.Integer: Builder.Append('r'); break;
                        case RegisterType.Vector:  Builder.Append('v'); break;
                    }

                    Builder.Append(reg.Index);
                    break;

                case OperandKind.Constant:
                    string symbolName = Symbols.Get(operand.Value);

                    if (symbolName != null)
                    {
                        _symbolNames.Add(operand.Value, symbolName);
                    }

                    Builder.Append("0x").Append(operand.Value.ToString("X"));
                    break;

                case OperandKind.Memory:
                    var memOp = (MemoryOperand)operand;

                    Builder.Append('[');

                    DumpOperand(memOp.BaseAddress);

                    if (memOp.Index != null)
                    {
                        Builder.Append(" + ");

                        DumpOperand(memOp.Index);

                        switch (memOp.Scale)
                        {
                            case Multiplier.x2: Builder.Append("*2"); break;
                            case Multiplier.x4: Builder.Append("*4"); break;
                            case Multiplier.x8: Builder.Append("*8"); break;
                        }
                    }

                    if (memOp.Displacement != 0)
                    {
                        Builder.Append(" + 0x").Append(memOp.Displacement.ToString("X"));
                    }

                    Builder.Append(']');
                    break;

                default:
                    Builder.Append(operand.Type);
                    break;
            }
        }

        private void DumpNode(Node node)
        {
            if (node.DestinationsCount > 0)
            {
                for (int index = 0; index < node.DestinationsCount; index++)
                {
                    DumpOperand(node.GetDestination(index));

                    if (index < node.DestinationsCount - 1)
                    {
                        Builder.Append(", ");
                    }
                }

                Builder.Append(" = ");
            }

            switch (node)
            {
                case PhiNode phi:
                    Builder.Append("Phi ");

                    for (int index = 0; index < phi.SourcesCount; index++)
                    {
                        Builder.Append('(');

                        DumpBlockName(phi.GetBlock(index));

                        Builder.Append(": ");

                        DumpOperand(phi.GetSource(index));

                        Builder.Append(')');

                        if (index < phi.SourcesCount - 1)
                        {
                            Builder.Append(", ");
                        }
                    }
                    break;

                case Operation operation:
                    Builder.Append(operation.Instruction);

                    if (operation.Instruction == Instruction.Extended)
                    {
                        var intrinOp = (IntrinsicOperation)operation;

                        Builder.Append('.').Append(intrinOp.Intrinsic);
                    }

                    Builder.Append(' ');

                    for (int index = 0; index < operation.SourcesCount; index++)
                    {
                        DumpOperand(operation.GetSource(index));

                        if (index < operation.SourcesCount - 1)
                        {
                            Builder.Append(", ");
                        }
                    }
                    break;
            }

            if (_symbolNames.Count == 1)
            {
                Builder.Append(" ;; ").Append(_symbolNames.First().Value);
            }
            else if (_symbolNames.Count > 1)
            {
                Builder.Append(" ;;");

                foreach ((ulong value, string name) in _symbolNames)
                {
                    Builder.Append(" 0x").Append(value.ToString("X")).Append(" = ").Append(name);
                }
            }

            // Reset the set of symbols for the next Node we're going to dump.
            _symbolNames.Clear();
        }

        public static string GetDump(ControlFlowGraph cfg)
        {
            IRDumper dumper = new IRDumper(1);

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                dumper.Indent();
                dumper.DumpBlockHeader(block);

                dumper.Builder.AppendLine();

                dumper.IncreaseIndentation();

                for (Node node = block.Operations.First; node != null; node = node.ListNext)
                {
                    dumper.Indent();
                    dumper.DumpNode(node);

                    dumper.Builder.AppendLine();
                }

                dumper.DecreaseIndentation();
            }

            return dumper.Builder.ToString();
        }

        private static string GetTypeName(OperandType type)
        {
            switch (type)
            {
                case OperandType.FP32: return "f32";
                case OperandType.FP64: return "f64";
                case OperandType.I32:  return "i32";
                case OperandType.I64:  return "i64";
                case OperandType.None: return "none";
                case OperandType.V128: return "v128";
            }

            throw new ArgumentException($"Invalid operand type \"{type}\".");
        }
    }
}