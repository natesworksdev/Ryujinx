using ARMeilleure.IntermediateRepresentation;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    static class RegisterToLocal
    {
        public static void Rename(ControlFlowGraph cfg)
        {
            Dictionary<Register, Operand> registerToLocalMap = new Dictionary<Register, Operand>();

            Operand GetLocal(Operand op)
            {
                Register register = op.GetRegister();

                if (!registerToLocalMap.TryGetValue(register, out Operand local))
                {
                    local = cfg.AllocateLocal(op.Type);

                    registerToLocalMap.Add(register, local);
                }

                return local;
            }

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (Node node = block.Operations.First; node != null; node = node.ListNext)
                {
                    if (node.DestinationsCount != 0 && node.Destination.Kind == OperandKind.Register)
                    {
                        node.Destination = GetLocal(node.Destination);
                    }

                    for (int index = 0; index < node.SourcesCount; index++)
                    {
                        Operand source = node.GetSource(index);

                        if (source.Kind == OperandKind.Register)
                        {
                            node.SetSource(index, GetLocal(source));
                        }
                    }
                }
            }
        }
    }
}