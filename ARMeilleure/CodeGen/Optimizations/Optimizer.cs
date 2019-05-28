using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Collections.Generic;
using System.Linq;

namespace ARMeilleure.CodeGen.Optimizations
{
    static class Optimizer
    {
        public static void Optimize(ControlFlowGraph cfg)
        {
            bool modified;

            do
            {
                modified = false;

                foreach (BasicBlock block in cfg.Blocks)
                {
                    LinkedListNode<Node> node = block.Operations.First;

                    while (node != null)
                    {
                        LinkedListNode<Node> nextNode = node.Next;

                        bool isUnused = IsUnused(node.Value);

                        if (!(node.Value is Operation operation) || (isUnused && !IsMemoryStore(operation.Inst)))
                        {
                            if (isUnused)
                            {
                                RemoveNode(block, node);

                                modified = true;
                            }

                            node = nextNode;

                            continue;
                        }

                        ConstantFolding.Fold(operation);

                        Simplification.Simplify(operation);

                        if (DestIsLocalVar(operation) && IsPropagableCopy(operation))
                        {
                            PropagateCopy(operation);

                            RemoveNode(block, node);

                            modified = true;
                        }

                        node = nextNode;
                    }
                }
            }
            while (modified);
        }

        private static void PropagateCopy(Operation copyOp)
        {
            //Propagate copy source operand to all uses of
            //the destination operand.
            Operand dest   = copyOp.Dest;
            Operand source = copyOp.GetSource(0);

            Node[] uses = dest.Uses.ToArray();

            foreach (Node use in uses)
            {
                for (int index = 0; index < use.SourcesCount; index++)
                {
                    if (use.GetSource(index) == dest)
                    {
                        use.SetSource(index, source);
                    }
                }
            }
        }

        private static void RemoveNode(BasicBlock block, LinkedListNode<Node> llNode)
        {
            //Remove a node from the nodes list, and also remove itself
            //from all the use lists on the operands that this node uses.
            block.Operations.Remove(llNode);

            Queue<Node> nodes = new Queue<Node>();

            nodes.Enqueue(llNode.Value);

            while (nodes.TryDequeue(out Node node))
            {
                for (int index = 0; index < node.SourcesCount; index++)
                {
                    Operand src = node.GetSource(index);

                    if (src.Kind != OperandKind.LocalVariable)
                    {
                        continue;
                    }

                    if (src.Uses.Remove(node) && src.Uses.Count == 0)
                    {
                        foreach (Node assignment in src.Assignments)
                        {
                            nodes.Enqueue(assignment);
                        }
                    }
                }
            }
        }

        private static bool IsUnused(Node node)
        {
            return DestIsLocalVar(node) && node.Dest.Uses.Count == 0;
        }

        private static bool DestIsLocalVar(Node node)
        {
            return node.Dest != null && node.Dest.Kind == OperandKind.LocalVariable;
        }

        private static bool IsPropagableCopy(Operation operation)
        {
            if (operation.Inst != Instruction.Copy)
            {
                return false;
            }

            return operation.Dest.Type == operation.GetSource(0).Type;
        }

        private static bool IsMemoryStore(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Store:
                case Instruction.Store16:
                case Instruction.Store8:
                    return true;
            }

            return false;
        }
    }
}