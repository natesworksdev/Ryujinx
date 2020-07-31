using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Diagnostics;

namespace ARMeilleure.CodeGen.Optimizations
{
    static class Optimizer
    {
        public static void RunPass(ControlFlowGraph cfg)
        {
            bool modified;

            do
            {
                modified = false;

                for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
                {
                    Node node = block.Operations.First;

                    while (node != null)
                    {
                        Node nextNode = node.ListNext;

                        bool isUnused = IsUnused(node);

                        if (!(node is Operation operation) || isUnused)
                        {
                            if (isUnused)
                            {
                                RemoveNode(block, node);

                                modified = true;
                            }

                            node = nextNode;

                            continue;
                        }

                        ConstantFolding.RunPass(operation);

                        Simplification.RunPass(operation);

                        if (DestIsLocalVar(operation))
                        {   
                            if (IsPropagableCompare(operation))
                            {
                                modified |= PropagateCompare(operation);

                                if (modified && IsUnused(operation))
                                {
                                    RemoveNode(block, node);
                                }
                            }
                            else if (IsPropagableCopy(operation))
                            {
                                PropagateCopy(operation);

                                RemoveNode(block, node);

                                modified = true;
                            }
                        }

                        node = nextNode;
                    }
                }
            }
            while (modified);
        }

        public static void RemoveUnusedNodes(ControlFlowGraph cfg)
        {
            bool modified;

            do
            {
                modified = false;

                for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
                {
                    Node node = block.Operations.First;

                    while (node != null)
                    {
                        Node nextNode = node.ListNext;

                        if (IsUnused(node))
                        {
                            RemoveNode(block, node);

                            modified = true;
                        }

                        node = nextNode;
                    }
                }
            }
            while (modified);
        }

        private static bool PropagateCompare(Operation compOp)
        {
            bool modified = false;

            Operand dest = compOp.Destination;
            Operand src1 = compOp.GetSource(0);
            Operand src2 = compOp.GetSource(1);
            Operand comp = compOp.GetSource(2);

            Comparison compType = (Comparison)comp.AsInt32();

            Node[] uses = dest.Uses.ToArray();

            foreach (Node use in uses)
            {
                if (!(use is Operation operation))
                {
                    continue;
                }

                Comparison actualCompType;

                if (operation.Instruction == Instruction.BranchIfTrue)
                {
                    actualCompType = compType;
                }
                else if (operation.Instruction == Instruction.BranchIfFalse)
                {
                    actualCompType = compType.Invert();
                }
                else
                {
                    continue;
                }

                operation.TurnIntoBranchIf(src1, src2, actualCompType);

                modified = true;
            }

            return modified;
        }

        private static void PropagateCopy(Operation copyOp)
        {
            // Propagate copy source operand to all uses of the destination operand.
            Operand dest   = copyOp.Destination;
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

        private static void RemoveNode(BasicBlock block, Node node)
        {
            // Remove a node from the nodes list, and also remove itself
            // from all the use lists on the operands that this node uses.
            block.Operations.Remove(node);

            for (int index = 0; index < node.SourcesCount; index++)
            {
                node.SetSource(index, null);
            }

            Debug.Assert(node.Destination == null || node.Destination.Uses.Count == 0);

            node.Destination = null;
        }

        private static bool IsUnused(Node node)
        {
            return DestIsLocalVar(node) && node.Destination.Uses.Count == 0 && !HasSideEffects(node);
        }

        private static bool DestIsLocalVar(Node node)
        {
            return node.Destination != null && node.Destination.Kind == OperandKind.LocalVariable;
        }

        private static bool HasSideEffects(Node node)
        {
            return (node is Operation operation) && (operation.Instruction == Instruction.Call
                || operation.Instruction == Instruction.Tailcall
                || operation.Instruction == Instruction.CompareAndSwap
                || operation.Instruction == Instruction.CompareAndSwap16
                || operation.Instruction == Instruction.CompareAndSwap8);
        }

        private static bool IsPropagableCompare(Operation operation)
        {
            return operation.Instruction == Instruction.Compare;
        }

        private static bool IsPropagableCopy(Operation operation)
        {
            if (operation.Instruction != Instruction.Copy)
            {
                return false;
            }

            return operation.Destination.Type == operation.GetSource(0).Type;
        }
    }
}