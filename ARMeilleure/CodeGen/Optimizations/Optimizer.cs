using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Collections.Generic;
using System.Diagnostics;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.Optimizations
{
    static class Optimizer
    {
        public static void RunPass(ControlFlowGraph cfg)
        {
            var uses = BuildUses(cfg);

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

                        bool isUnused = IsUnused(uses, node);

                        if (node is not Operation operation || isUnused)
                        {
                            if (isUnused)
                            {
                                RemoveNode(block, uses, node);

                                modified = true;
                            }

                            node = nextNode;

                            continue;
                        }

                        Operand op = ConstantFolding.RunPass(operation);

                        if (op != null)
                        {
                            RemoveAllUses(uses, node);
                            operation.TurnIntoCopy(op);
                        }

                        op = Simplification.RunPass(operation);

                        if (op != null)
                        {
                            RemoveAllUses(uses, node);
                            operation.TurnIntoCopy(op);
                        }

                        if (DestIsLocalVar(operation))
                        {   
                            if (IsPropagableCompare(operation))
                            {
                                modified |= PropagateCompare(uses, operation);

                                if (modified && IsUnused(uses, operation))
                                {
                                    RemoveNode(block, uses, node);
                                }
                            }
                            else if (IsPropagableCopy(operation))
                            {
                                PropagateCopy(uses, operation);

                                RemoveNode(block, uses, node);

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
            var uses = BuildUses(cfg);

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

                        if (IsUnused(uses, node))
                        {
                            RemoveNode(block, uses, node);

                            modified = true;
                        }

                        node = nextNode;
                    }
                }
            }
            while (modified);
        }

        private static Dictionary<Operand, List<Node>> BuildUses(ControlFlowGraph cfg)
        {
            var uses = new Dictionary<Operand, List<Node>>();

            void AddUse(Node node, Operand source)
            {
                if (source.Kind == OperandKind.LocalVariable)
                {
                    if (!uses.TryGetValue(source, out var list))
                    {
                        list = new List<Node>();
                        uses.Add(source, list);
                    }

                    list.Add(node);
                }
            }

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (Node node = block.Operations.First; node != null; node = node.ListNext)
                {
                    for (int srcIndex = 0; srcIndex < node.SourcesCount; srcIndex++)
                    {
                        Operand source = node.GetSource(srcIndex);

                        if (source.Kind == OperandKind.LocalVariable)
                        {
                            AddUse(node, source);
                        }
                        else if (source.Kind == OperandKind.Memory)
                        {
                            MemoryOperand memOp = (MemoryOperand)source;

                            if (memOp.BaseAddress != null && memOp.BaseAddress.Kind == OperandKind.LocalVariable)
                            {
                                AddUse(node, memOp.BaseAddress);
                            }

                            if (memOp.Index != null && memOp.Index.Kind == OperandKind.LocalVariable)
                            {
                                AddUse(node, memOp.Index);
                            }
                        }
                    }
                }
            }

            return uses;
        }

        private static bool PropagateCompare(Dictionary<Operand, List<Node>> uses, Operation compOp)
        {
            // Try to propagate Compare operations into their BranchIf uses, when these BranchIf uses are in the form
            // of:
            //
            // - BranchIf %x, 0x0, Equal        ;; i.e BranchIfFalse %x
            // - BranchIf %x, 0x0, NotEqual     ;; i.e BranchIfTrue %x
            //
            // The commutative property of Equal and NotEqual is taken into consideration as well.
            //
            // For example:
            //
            //  %x = Compare %a, %b, comp
            //  BranchIf %x, 0x0, NotEqual
            //
            // =>
            //
            //  BranchIf %a, %b, comp

            static bool IsZeroBranch(Operation operation, out Comparison compType)
            {
                compType = Comparison.Equal;

                if (operation.Instruction != Instruction.BranchIf)
                {
                    return false;
                }

                Operand src1 = operation.GetSource(0);
                Operand src2 = operation.GetSource(1);
                Operand comp = operation.GetSource(2);

                compType = (Comparison)comp.AsInt32();

                return (src1.Kind == OperandKind.Constant && src1.Value == 0) ||
                       (src2.Kind == OperandKind.Constant && src2.Value == 0);
            }

            bool modified = false;

            Operand dest = compOp.Destination;
            Operand src1 = compOp.GetSource(0);
            Operand src2 = compOp.GetSource(1);
            Operand comp = compOp.GetSource(2);

            Comparison compType = (Comparison)comp.AsInt32();

            if (!uses.TryGetValue(dest, out var usesList))
            {
                return false;
            }

            foreach (Node use in usesList)
            {
                if (use is not Operation operation)
                {
                    continue;
                }

                // If operation is a BranchIf and has a constant value 0 in its RHS or LHS source operands.
                if (IsZeroBranch(operation, out Comparison otherCompType))
                {
                    Comparison propCompType;

                    if (otherCompType == Comparison.NotEqual)
                    {
                        propCompType = compType;
                    }
                    else if (otherCompType == Comparison.Equal)
                    {
                        propCompType = compType.Invert();
                    }
                    else
                    {
                        continue;
                    }

                    operation.SetSource(0, src1);
                    operation.SetSource(1, src2);
                    operation.SetSource(2, Const((int)propCompType));

                    modified = true;
                }
            }

            return modified;
        }

        private static void PropagateCopy(Dictionary<Operand, List<Node>> uses, Operation copyOp)
        {
            // Propagate copy source operand to all uses of the destination operand.
            Operand dest   = copyOp.Destination;
            Operand source = copyOp.GetSource(0);

            if (!uses.TryGetValue(dest, out var usesList))
            {
                return;
            }

            foreach (Node use in usesList)
            {
                for (int index = 0; index < use.SourcesCount; index++)
                {
                    if (use.GetSource(index) == dest)
                    {
                        use.SetSource(index, source);
                    }
                }
            }

            uses.Remove(dest);
        }

        private static void RemoveNode(BasicBlock block, Dictionary<Operand, List<Node>> uses, Node node)
        {
            // Remove a node from the nodes list, and also remove itself
            // from all the use lists on the operands that this node uses.
            block.Operations.Remove(node);
            RemoveAllUses(uses, node);

            for (int index = 0; index < node.SourcesCount; index++)
            {
                node.SetSource(index, null);
            }

            Debug.Assert(node.DestinationsCount == 0 || !uses.ContainsKey(node.Destination));

            node.Destination = null;
        }

        private static void RemoveAllUses(Dictionary<Operand, List<Node>> uses, Node node)
        {
            for (int srcIndex = 0; srcIndex < node.SourcesCount; srcIndex++)
            {
                Operand source = node.GetSource(srcIndex);

                if (source.Kind == OperandKind.LocalVariable)
                {
                    RemoveUse(uses, node, source);
                }
                else if (source.Kind == OperandKind.Memory)
                {
                    MemoryOperand memOp = (MemoryOperand)source;

                    if (memOp.BaseAddress != null && memOp.BaseAddress.Kind == OperandKind.LocalVariable)
                    {
                        RemoveUse(uses, node, memOp.BaseAddress);
                    }

                    if (memOp.Index != null && memOp.Index.Kind == OperandKind.LocalVariable)
                    {
                        RemoveUse(uses, node, memOp.Index);
                    }
                }
            }
        }

        private static void RemoveUse(Dictionary<Operand, List<Node>> uses, Node node, Operand source)
        {
            if (uses.TryGetValue(source, out var list))
            {
                if (list.Count > 1)
                {
                    list.Remove(node);
                }
                else
                {
                    uses.Remove(source);
                }
            }
        }

        private static bool IsUnused(Dictionary<Operand, List<Node>> uses, Node node)
        {
            return DestIsLocalVar(node) && !uses.ContainsKey(node.Destination) && !HasSideEffects(node);
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