using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    static class Ssa
    {
        private class DefMap
        {
            private Dictionary<Register, Operand> _map;

            private long[] _phiMasks;

            public DefMap()
            {
                _map = new Dictionary<Register, Operand>();

                _phiMasks = new long[(RegisterConsts.TotalCount + 63) / 64];
            }

            public bool TryAddOperand(Register reg, Operand operand)
            {
                return _map.TryAdd(reg, operand);
            }

            public bool TryGetOperand(Register reg, out Operand operand)
            {
                return _map.TryGetValue(reg, out operand);
            }

            public bool AddPhi(Register reg)
            {
                int key = GetKeyFromRegister(reg);

                int index = key / 64;
                int bit   = key & 63;

                long mask = 1L << bit;

                if ((_phiMasks[index] & mask) != 0)
                {
                    return false;
                }

                _phiMasks[index] |= mask;

                return true;
            }

            public bool HasPhi(Register reg)
            {
                int key = GetKeyFromRegister(reg);

                int index = key / 64;
                int bit   = key & 63;

                return (_phiMasks[index] & (1L << bit)) != 0;
            }
        }

        public static void Rename(ControlFlowGraph cfg)
        {
            DefMap[] globalDefs = new DefMap[cfg.Blocks.Count];

            foreach (BasicBlock block in cfg.Blocks)
            {
                globalDefs[block.Index] = new DefMap();
            }

            Queue<BasicBlock> dfPhiBlocks = new Queue<BasicBlock>();

            //First pass, get all defs and locals uses.
            foreach (BasicBlock block in cfg.Blocks)
            {
                Operand[] localDefs = new Operand[RegisterConsts.TotalCount];

                LinkedListNode<Node> node = block.Operations.First;

                Operand RenameLocal(Operand operand)
                {
                    if (operand != null && operand.Kind == OperandKind.Register)
                    {
                        Operand local = localDefs[GetKeyFromRegister(operand.GetRegister())];

                        if (local != null && local.Type != operand.Type)
                        {
                            Operand temp = Local(operand.Type);

                            Operation castOp = new Operation(Instruction.Copy, temp, local);

                            block.Operations.AddBefore(node, castOp);

                            local = temp;
                        }

                        operand = local ?? operand;
                    }

                    return operand;
                }

                while (node != null)
                {
                    if (node.Value is Operation operation)
                    {
                        for (int index = 0; index < operation.SourcesCount; index++)
                        {
                            operation.SetSource(index, RenameLocal(operation.GetSource(index)));
                        }

                        Operand dest = operation.Dest;

                        if (dest != null && dest.Kind == OperandKind.Register)
                        {
                            Operand local = Local(dest.Type);

                            localDefs[GetKeyFromRegister(dest.GetRegister())] = local;

                            operation.Dest = local;
                        }
                    }

                    node = node.Next;
                }

                for (int index = 0; index < RegisterConsts.TotalCount; index++)
                {
                    Operand local = localDefs[index];

                    if (local == null)
                    {
                        continue;
                    }

                    Register reg = GetRegisterFromKey(index);

                    globalDefs[block.Index].TryAddOperand(reg, local);

                    dfPhiBlocks.Enqueue(block);

                    while (dfPhiBlocks.TryDequeue(out BasicBlock dfPhiBlock))
                    {
                        foreach (BasicBlock domFrontier in dfPhiBlock.DominanceFrontiers)
                        {
                            if (globalDefs[domFrontier.Index].AddPhi(reg))
                            {
                                dfPhiBlocks.Enqueue(domFrontier);
                            }
                        }
                    }
                }
            }

            //Second pass, rename variables with definitions on different blocks.
            foreach (BasicBlock block in cfg.Blocks)
            {
                Operand[] localDefs = new Operand[RegisterConsts.TotalCount];

                LinkedListNode<Node> node = block.Operations.First;

                Operand RenameGlobal(Operand operand)
                {
                    if (operand != null && operand.Kind == OperandKind.Register)
                    {
                        int key = GetKeyFromRegister(operand.GetRegister());

                        Operand local = localDefs[key];

                        if (local != null)
                        {
                            if (local.Type != operand.Type)
                            {
                                Operand temp = Local(operand.Type);

                                Operation castOp = new Operation(Instruction.Copy, temp, local);

                                block.Operations.AddBefore(node, castOp);

                                local = temp;
                            }

                            return local;
                        }

                        operand = FindDef(globalDefs, block, operand);

                        localDefs[key] = operand;
                    }

                    return operand;
                }

                while (node != null)
                {
                    if (node.Value is Operation operation)
                    {
                        for (int index = 0; index < operation.SourcesCount; index++)
                        {
                            operation.SetSource(index, RenameGlobal(operation.GetSource(index)));
                        }
                    }

                    node = node.Next;
                }
            }
        }

        private static Operand FindDef(DefMap[] globalDefs, BasicBlock current, Operand operand)
        {
            if (globalDefs[current.Index].HasPhi(operand.GetRegister()))
            {
                return InsertPhi(globalDefs, current, operand);
            }

            if (current != current.ImmediateDominator)
            {
                return FindDefOnPred(globalDefs, current.ImmediateDominator, operand);
            }

            return Undef();
        }

        private static Operand FindDefOnPred(DefMap[] globalDefs, BasicBlock current, Operand operand)
        {
            foreach (BasicBlock block in SelfAndImmediateDominators(current))
            {
                DefMap defMap = globalDefs[block.Index];

                if (defMap.TryGetOperand(operand.GetRegister(), out Operand lastDef))
                {
                    return lastDef;
                }

                if (defMap.HasPhi(operand.GetRegister()))
                {
                    return InsertPhi(globalDefs, block, operand);
                }
            }

            return Undef();
        }

        private static IEnumerable<BasicBlock> SelfAndImmediateDominators(BasicBlock block)
        {
            while (block != block.ImmediateDominator)
            {
                yield return block;

                block = block.ImmediateDominator;
            }

            yield return block;
        }

        private static Operand InsertPhi(DefMap[] globalDefs, BasicBlock block, Operand operand)
        {
            //This block has a Phi that has not been materialized yet, but that
            //would define a new version of the variable we're looking for. We need
            //to materialize the Phi, add all the block/operand pairs into the Phi, and
            //then use the definition from that Phi.
            Operand local = Local(operand.Type);

            PhiNode phi = new PhiNode(local, block.Predecessors.Count);

            AddPhi(block, phi);

            globalDefs[block.Index].TryAddOperand(operand.GetRegister(), local);

            for (int index = 0; index < block.Predecessors.Count; index++)
            {
                BasicBlock predecessor = block.Predecessors[index];

                phi.SetBlock(index, predecessor);
                phi.SetSource(index, FindDefOnPred(globalDefs, predecessor, operand));
            }

            return local;
        }

        private static void AddPhi(BasicBlock block, PhiNode phi)
        {
            LinkedListNode<Node> node = block.Operations.First;

            if (node != null)
            {
                while (node.Next?.Value is PhiNode)
                {
                    node = node.Next;
                }
            }

            if (node?.Value is PhiNode)
            {
                block.Operations.AddAfter(node, phi);
            }
            else
            {
                block.Operations.AddFirst(phi);
            }
        }

        private static int GetKeyFromRegister(Register reg)
        {
            if (reg.Type == RegisterType.Integer)
            {
                return reg.Index;
            }
            else if (reg.Type == RegisterType.Vector)
            {
                return RegisterConsts.IntRegsCount + reg.Index;
            }
            else /* if (reg.Type == RegisterType.Flag) */
            {
                return RegisterConsts.IntAndVecRegsCount + reg.Index;
            }
        }

        private static Register GetRegisterFromKey(int key)
        {
            if (key < RegisterConsts.IntRegsCount)
            {
                return new Register(key, RegisterType.Integer);
            }
            else if (key < RegisterConsts.IntAndVecRegsCount)
            {
                return new Register(key - RegisterConsts.IntRegsCount, RegisterType.Vector);
            }
            else /* if (key < RegisterConsts.TotalCount) */
            {
                return new Register(key - RegisterConsts.IntAndVecRegsCount, RegisterType.Flag);
            }
        }
    }
}