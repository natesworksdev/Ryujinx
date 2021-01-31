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
                    var operation = block.Operations.First;

                    while (operation != null)
                    {
                        var nextNode = operation.ListNext;

                        bool isUnused = IsUnused(uses, operation);

                        if (isUnused)
                        {
                            RemoveNode(block, uses, operation);

                            modified = true;

                            operation = nextNode;

                            continue;
                        }

                        Operand? op = ConstantFolding.RunPass(operation);

                        if (op != null)
                        {
                            RemoveAllUses(uses, operation);
                            operation.TurnIntoCopy(op.Value);
                        }

                        op = Simplification.RunPass(operation);

                        if (op != null)
                        {
                            RemoveAllUses(uses, operation);
                            operation.TurnIntoCopy(op.Value);
                        }

                        if (DestIsLocalVar(operation))
                        {   
                            if (IsPropagableCompare(operation))
                            {
                                modified |= PropagateCompare(uses, operation);

                                if (modified && IsUnused(uses, operation))
                                {
                                    RemoveNode(block, uses, operation);
                                }
                            }
                            else if (IsPropagableCopy(operation))
                            {
                                PropagateCopy(uses, operation);

                                RemoveNode(block, uses, operation);

                                modified = true;
                            }
                        }

                        operation = nextNode;
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
                    var node = block.Operations.First;

                    while (node != null)
                    {
                        var nextNode = node.ListNext;

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

        private static Dictionary<Operand, List<Operation>> BuildUses(ControlFlowGraph cfg)
        {
            var uses = new Dictionary<Operand, List<Operation>>();

            void AddUse(Operation node, Operand source)
            {
                if (source.Kind == OperandKind.LocalVariable)
                {
                    if (!uses.TryGetValue(source, out var list))
                    {
                        list = new List<Operation>();
                        uses.Add(source, list);
                    }

                    list.Add(node);
                }
            }

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (var node = block.Operations.First; node != null; node = node.ListNext)
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
                            MemoryOperand memOp = source.GetMemoryOperand();

                            if (memOp.BaseAddress != null && memOp.BaseAddress.Value.Kind == OperandKind.LocalVariable)
                            {
                                AddUse(node, memOp.BaseAddress.Value);
                            }

                            if (memOp.Index != null && memOp.Index.Value.Kind == OperandKind.LocalVariable)
                            {
                                AddUse(node, memOp.Index.Value);
                            }
                        }
                    }
                }
            }

            return uses;
        }

        private static bool PropagateCompare(Dictionary<Operand, List<Operation>> uses, Operation compOp)
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

            foreach (var operation in usesList)
            {
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

        private static void PropagateCopy(Dictionary<Operand, List<Operation>> uses, Operation copyOp)
        {
            // Propagate copy source operand to all uses of the destination operand.
            Operand dest   = copyOp.Destination;
            Operand source = copyOp.GetSource(0);

            if (!uses.TryGetValue(dest, out var usesList))
            {
                return;
            }

            foreach (var use in usesList)
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

        private static void RemoveNode(BasicBlock block, Dictionary<Operand, List<Operation>> uses, Operation node)
        {
            // Remove a node from the nodes list, and also remove itself
            // from all the use lists on the operands that this node uses.
            block.Operations.Remove(node);
            RemoveAllUses(uses, node);
            Debug.Assert(node.DestinationsCount == 0 || !uses.ContainsKey(node.Destination));
        }

        private static void RemoveAllUses(Dictionary<Operand, List<Operation>> uses, Operation node)
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
                    MemoryOperand memOp = source.GetMemoryOperand();

                    if (memOp.BaseAddress != null && memOp.BaseAddress.Value.Kind == OperandKind.LocalVariable)
                    {
                        RemoveUse(uses, node, memOp.BaseAddress.Value);
                    }

                    if (memOp.Index != null && memOp.Index.Value.Kind == OperandKind.LocalVariable)
                    {
                        RemoveUse(uses, node, memOp.Index.Value);
                    }
                }
            }
        }

        private static void RemoveUse(Dictionary<Operand, List<Operation>> uses, Operation node, Operand source)
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

        private static bool IsUnused(Dictionary<Operand, List<Operation>> uses, Operation node)
        {
            return DestIsLocalVar(node) && !uses.ContainsKey(node.Destination) && !HasSideEffects(node);
        }

        private static bool DestIsLocalVar(Operation operation)
        {
            return operation.DestinationsCount != 0 && operation.Destination.Kind == OperandKind.LocalVariable;
        }

        private static bool HasSideEffects(Operation operation)
        {
            return operation.Instruction == Instruction.Call
                || operation.Instruction == Instruction.Tailcall
                || operation.Instruction == Instruction.CompareAndSwap
                || operation.Instruction == Instruction.CompareAndSwap16
                || operation.Instruction == Instruction.CompareAndSwap8;
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