using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    static class PhiFunctions
    {
        private struct PhiEntry
        {
            public BasicBlock Predecessor { get; }
            public Operand    Source      { get; }

            public PhiEntry(BasicBlock predecessor, Operand source)
            {
                Predecessor = predecessor;
                Source      = source;
            }
        }
        public static void Remove(ControlFlowGraph cfg)
        {
            List<int> defBlocks = new List<int>();

            HashSet<Operand> visited = new HashSet<Operand>();

            //Build a list with the index of the block where each variable
            //is defined, and additionally number all the variables.
            foreach (BasicBlock block in cfg.Blocks)
            {
                foreach (Node node in block.Operations)
                {
                    if (node.Dest != null && node.Dest.Kind == OperandKind.LocalVariable && visited.Add(node.Dest))
                    {
                        node.Dest.NumberLocal(defBlocks.Count);

                        defBlocks.Add(block.Index);
                    }
                }
            }

            foreach (BasicBlock block in cfg.Blocks)
            {
                LinkedListNode<Node> node = block.Operations.First;

                while (node?.Value is PhiNode phi)
                {
                    LinkedListNode<Node> nextNode = node.Next;

                    Operand local = Local(phi.Dest.Type);

                    PhiEntry[] phiSources = GetPhiSources(phi);

                    for (int srcIndex = 0; srcIndex < phiSources.Length; srcIndex++)
                    {
                        BasicBlock predecessor = phiSources[srcIndex].Predecessor;

                        Operand source = phiSources[srcIndex].Source;

                        predecessor.Append(new Operation(Instruction.Copy, local, source));
                    }

                    for (int index = 0; index < phi.SourcesCount; index++)
                    {
                        phi.SetSource(index, null);
                    }

                    Operation copyOp = new Operation(Instruction.Copy, phi.Dest, local);

                    block.Operations.AddBefore(node, copyOp);

                    phi.Dest = null;

                    block.Operations.Remove(node);

                    node = nextNode;
                }
            }
        }

        private static PhiEntry[] GetPhiSources(PhiNode phi)
        {
            Dictionary<Operand, HashSet<BasicBlock>> defBlocksPerSrc = new Dictionary<Operand, HashSet<BasicBlock>>();

            List<PhiEntry> phiSources = new List<PhiEntry>();

            for (int index = 0; index < phi.SourcesCount; index++)
            {
                Operand source = phi.GetSource(index);

                BasicBlock predecessor = phi.GetBlock(index);

                BasicBlock defBlock = FindDefBlock(source, predecessor);

                if (defBlock != null)
                {
                    if (!defBlocksPerSrc.TryGetValue(source, out HashSet<BasicBlock> defBlocks))
                    {
                        defBlocks = new HashSet<BasicBlock>();

                        defBlocksPerSrc.Add(source, defBlocks);
                    }

                    if (!defBlocks.Add(defBlock))
                    {
                        continue;
                    }
                }

                phiSources.Add(new PhiEntry(defBlock ?? predecessor, source));
            }

            return phiSources.ToArray();
        }

        private static BasicBlock FindDefBlock(Operand source, BasicBlock predecessor)
        {
            if (source.Kind == OperandKind.LocalVariable)
            {
                while (true)
                {
                    foreach (Node node in predecessor.Operations)
                    {
                        if (node.Dest == source)
                        {
                            return predecessor;
                        }
                    }

                    if (predecessor == predecessor.ImmediateDominator)
                    {
                        break;
                    }

                    predecessor = predecessor.ImmediateDominator;
                }
            }

            return null;
        }
    }
}