using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class HybridAllocator : IRegisterAllocator
    {
        private struct BlockInfo
        {
            public bool HasCall { get; }

            public int IntFixedRegisters { get; }
            public int VecFixedRegisters { get; }

            public BlockInfo(bool hasCall, int intFixedRegisters, int vecFixedRegisters)
            {
                HasCall           = hasCall;
                IntFixedRegisters = intFixedRegisters;
                VecFixedRegisters = vecFixedRegisters;
            }
        }

        private struct LocalInfo
        {
            public int Uses { get; set; }
            public int UsesAllocated { get; set; }
            public int Sequence { get; set; }
            public Operand Temp { get; set; }
            public Operand Register { get; set; }
            public Operand SpillOffset { get; set; }
            public OperandType Type { get; }

            private int _first;
            private int _last;

            public bool IsBlockLocal => _first == _last;

            public LocalInfo(Operand local, int uses, int blkIndex)
            {
                Uses = uses;
                Type = local.Type;

                UsesAllocated = 0;
                Sequence = 0;
                Temp = default;
                Register = default;
                SpillOffset = default;

                _first = -1;
                _last  = -1;

                SetBlockIndex(blkIndex);
            }

            public void SetBlockIndex(int blkIndex)
            {
                if (_first == -1 || blkIndex < _first)
                {
                    _first = blkIndex;
                }

                if (_last == -1 || blkIndex > _last)
                {
                    _last = blkIndex;
                }
            }
        }

        private const int RegistersCount = 16;

        // The "visited" state is stored in the MSB of the local's value.
        private const ulong VisitedMask = 1ul << 63;

        private BlockInfo[] _blockInfo;
        private LocalInfo[] _localInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVisited(Operand local)
        {
            Debug.Assert(local.Kind == OperandKind.LocalVariable);

            return (local.GetValueUnsafe() & VisitedMask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetVisited(Operand local)
        {
            Debug.Assert(local.Kind == OperandKind.LocalVariable);

            local.GetValueUnsafe() |= VisitedMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref LocalInfo GetLocalInfo(Operand local)
        {
            Debug.Assert(local.Kind == OperandKind.LocalVariable);
            Debug.Assert(IsVisited(local), "Local variable not visited. Used before defined?");

            return ref _localInfo[(uint)local.GetValueUnsafe() - 1];
        }

        public AllocationResult RunPass(ControlFlowGraph cfg, StackAllocator stackAlloc, RegisterMasks regMasks)
        {
            int intUsedRegisters = 0;
            int vecUsedRegisters = 0;

            int intFreeRegisters = regMasks.IntAvailableRegisters;
            int vecFreeRegisters = regMasks.VecAvailableRegisters;

            _blockInfo = new BlockInfo[cfg.Blocks.Count];
            _localInfo = new LocalInfo[cfg.Blocks.Count * 3];

            int localInfoCount = 0;

            for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];

                int intFixedRegisters = 0;
                int vecFixedRegisters = 0;

                bool hasCall = false;

                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    if (node.Instruction == Instruction.Call)
                    {
                        hasCall = true;
                    }

                    foreach (Operand source in node.SourcesUnsafe)
                    {
                        if (source.Kind == OperandKind.LocalVariable)
                        {
                            GetLocalInfo(source).SetBlockIndex(block.Index);
                        }
                        else if (source.Kind == OperandKind.Memory)
                        {
                            MemoryOperand memOp = source.GetMemory();

                            if (memOp.BaseAddress != default)
                            {
                                GetLocalInfo(memOp.BaseAddress).SetBlockIndex(block.Index);
                            }

                            if (memOp.Index != default)
                            {
                                GetLocalInfo(memOp.Index).SetBlockIndex(block.Index);
                            }
                        }
                    }

                    foreach (Operand dest in node.DestinationsUnsafe)
                    {
                        if (dest.Kind == OperandKind.LocalVariable)
                        {
                            if (IsVisited(dest))
                            {
                                GetLocalInfo(dest).SetBlockIndex(block.Index);
                            }
                            else
                            {
                                dest.NumberLocal(++localInfoCount);

                                if (localInfoCount > _localInfo.Length)
                                {
                                    Array.Resize(ref _localInfo, localInfoCount * 2);
                                }

                                SetVisited(dest);
                                GetLocalInfo(dest) = new LocalInfo(dest, UsesCount(dest), block.Index);
                            }
                        }
                        else if (dest.Kind == OperandKind.Register)
                        {
                            if (dest.Type.IsInteger())
                            {
                                intFixedRegisters |= 1 << dest.GetRegister().Index;
                            }
                            else
                            {
                                vecFixedRegisters |= 1 << dest.GetRegister().Index;
                            }
                        }
                    }
                }

                _blockInfo[block.Index] = new BlockInfo(hasCall, intFixedRegisters, vecFixedRegisters);
            }

            Operand[] intActive = new Operand[RegistersCount];
            Operand[] vecActive = new Operand[RegistersCount];

            Operation dummyNode = Operation(Instruction.Extended, default);

            for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];

                ref BlockInfo blkInfo = ref _blockInfo[block.Index];

                int intLocalFreeRegisters = intFreeRegisters & ~blkInfo.IntFixedRegisters;
                int vecLocalFreeRegisters = vecFreeRegisters & ~blkInfo.VecFixedRegisters;

                int intActiveRegisters = 0;
                int vecActiveRegisters = 0;

                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    bool folded = false;

                    int intCurrActiveRegisters = 0;
                    int vecCurrActiveRegisters = 0;

                    // If operation is a copy of a local and that local is living on the stack, we turn the copy into
                    // a fill, instead of inserting a fill before it.
                    if (node.Instruction == Instruction.Copy)
                    {
                        Operand source = node.GetSource(0);

                        if (source.Kind == OperandKind.LocalVariable)
                        {
                            ref LocalInfo info = ref GetLocalInfo(source);

                            if (info.Register == default && info.SpillOffset != default)
                            {
                                Operation fillOp = Operation(Instruction.Fill, node.Destination, info.SpillOffset);

                                block.Operations.AddBefore(node, fillOp);
                                block.Operations.Remove(node);

                                node = fillOp;

                                folded = true;
                            }
                        }
                    }
                    // If operation is call, spill caller saved registers.
                    else if (node.Instruction == Instruction.Call)
                    {
                        int intCallerSavedRegisters = regMasks.IntCallerSavedRegisters & intActiveRegisters;
                        int vecCallerSavedRegisters = regMasks.VecCallerSavedRegisters & vecActiveRegisters;

                        while (intCallerSavedRegisters != 0)
                        {
                            int reg = BitOperations.TrailingZeroCount(intCallerSavedRegisters);

                            SpillRegister(ref GetLocalInfo(intActive[reg]), node);

                            intActive[reg] = default;
                            intActiveRegisters &= ~(1 << reg);
                            intCurrActiveRegisters |= 1 << reg;
                            intCallerSavedRegisters &= ~(1 << reg);
                        }

                        while (vecCallerSavedRegisters != 0)
                        {
                            int reg = BitOperations.TrailingZeroCount(vecCallerSavedRegisters);

                            SpillRegister(ref GetLocalInfo(vecActive[reg]), node);

                            vecActive[reg] = default;
                            vecActiveRegisters &= ~(1 << reg);
                            vecCurrActiveRegisters |= 1 << reg;
                            vecCallerSavedRegisters &= ~(1 << reg);
                        }
                    }

                    // If the operation is folded to a fill, no need to inspect sources; since sources of fills are
                    // constant operands which do not require registers.
                    if (!folded)
                    {
                        foreach (ref Operand source in node.SourcesUnsafe)
                        {
                            if (source.Kind == OperandKind.LocalVariable)
                            {
                                source = UseRegister(source);
                            }
                            else if (source.Kind == OperandKind.Memory)
                            {
                                MemoryOperand memOp = source.GetMemory();

                                if (memOp.BaseAddress != default)
                                {
                                    memOp.BaseAddress = UseRegister(memOp.BaseAddress);
                                }

                                if (memOp.Index != default)
                                {
                                    memOp.Index = UseRegister(memOp.Index);
                                }
                            }
                        }
                    }

                    foreach (ref Operand dest in node.DestinationsUnsafe)
                    {
                        if (dest.Kind == OperandKind.LocalVariable)
                        {
                            dest = DefRegister(dest);
                        }
                    }

                    Operand UseRegister(Operand local)
                    {
                        ref LocalInfo info = ref GetLocalInfo(local);

                        info.UsesAllocated++;

                        Debug.Assert(info.UsesAllocated <= info.Uses);

                        // If the local does not have a register, allocate one and reload the local from the stack.
                        if (info.Register == default)
                        {
                            info.Uses++;
                            info.Register = DefRegister(local);

                            FillRegister(ref info, node); 
                        }

                        Operand result = info.Register;
                        Register reg = info.Register.GetRegister();

                        if (local.Type.IsInteger())
                        {
                            intCurrActiveRegisters |= 1 << reg.Index;
                        }
                        else
                        {
                            vecCurrActiveRegisters |= 1 << reg.Index;
                        }

                        // If we've reached the last use of the local, we can free the register "gracefully".
                        if (info.UsesAllocated == info.Uses)
                        {
                            // If the local is not a block local, we have to spill it; otherwise this would cause
                            // issues when the local is used in a loop.
                            //
                            // If the local has only a single definition, we can skip spilling because the definition
                            // will be spilled at the block where it was defined's exit or it will be spilled because it
                            // was evicted.
                            if (!info.IsBlockLocal && local.AssignmentsCount > 1)
                            {
                                SpillRegister(ref info, node);
                            }

                            if (local.Type.IsInteger())
                            {
                                intActiveRegisters &= ~(1 << reg.Index);
                                intActive[reg.Index] = default;
                            }
                            else
                            {
                                vecActiveRegisters &= ~(1 << reg.Index);
                                vecActive[reg.Index] = default;
                            }

                            info.Register = default;
                        }

                        return result;
                    }
                
                    Operand DefRegister(Operand local)
                    {
                        ref LocalInfo info = ref GetLocalInfo(local);

                        info.UsesAllocated++;

                        Debug.Assert(info.UsesAllocated <= info.Uses);

                        // If the local already has a register it is living in, return that register.
                        if (info.Register != default)
                        {
                            return info.Register;
                        }

                        int mask = local.Type.IsInteger()
                            ? intLocalFreeRegisters & ~(intActiveRegisters | intCurrActiveRegisters)
                            : vecLocalFreeRegisters & ~(vecActiveRegisters | vecCurrActiveRegisters);

                        int selectedReg;

                        // If we have inactive registers available, use one of them.
                        if (mask != 0)
                        {
                            selectedReg = BitOperations.TrailingZeroCount(mask);
                        }
                        // Otherwise we spill an active register and use the that register.
                        else
                        {
                            int spillReg;
                            int spillMask;
                            Operand spillLocal;
                            Operand[] spillActive;

                            if (local.Type.IsInteger())
                            {
                                spillMask = intActiveRegisters & ~intCurrActiveRegisters;
                                spillActive = intActive;
                            }
                            else
                            {
                                spillMask = vecActiveRegisters & ~vecCurrActiveRegisters;
                                spillActive = vecActive;
                            }

                            // The heuristic will select the first register which is holding a non block local. This is
                            // based on the assumption that block locals are more likely to be used next.
                            //
                            // TODO: Quite often, this assumption is not necessarily true, investigate other heuristics.
                            int tempMask = spillMask;

                            do
                            {
                                spillReg = BitOperations.TrailingZeroCount(tempMask);
                                spillLocal = spillActive[spillReg];

                                if (!GetLocalInfo(spillLocal).IsBlockLocal)
                                {
                                    break;
                                }

                                tempMask &= ~(1 << spillReg);
                            }
                            while (tempMask != 0);

                            SpillRegister(ref GetLocalInfo(spillLocal), node);

                            selectedReg = spillReg;
                        }

                        info.Register = Register(selectedReg, local.Type.ToRegisterType(), local.Type);

                        // Move selected register to the active set.
                        if (local.Type.IsInteger())
                        {
                            intUsedRegisters |= 1 << selectedReg;
                            intActiveRegisters |= 1 << selectedReg;
                            intCurrActiveRegisters |= 1 << selectedReg;
                            intActive[selectedReg] = local;
                        }
                        else
                        {
                            vecUsedRegisters |= 1 << selectedReg;
                            vecActiveRegisters |= 1 << selectedReg;
                            vecCurrActiveRegisters |= 1 << selectedReg;
                            vecActive[selectedReg] = local;
                        }

                        return info.Register;
                    }
                }

                // If there are still registers in the active set after allocation of the block, we spill them for the
                // next block.
                if ((intActiveRegisters | vecActiveRegisters) != 0)
                {
                    // If the block has 0 successors then the control flow exits. This means we can skip spilling since
                    // we're exiting anyways.
                    bool needSpill = block.SuccessorsCount > 0;

                    dummyNode = block.Append(dummyNode);

                    while (intActiveRegisters != 0)
                    {
                        int reg = BitOperations.TrailingZeroCount(intActiveRegisters);
                        ref LocalInfo info = ref GetLocalInfo(intActive[reg]);

                        if (needSpill && !info.IsBlockLocal)
                        {
                            SpillRegister(ref info, dummyNode);
                        }
                        else
                        {
                            info.Register = default;
                        }

                        intActiveRegisters &= ~(1 << reg);
                        intActive[reg] = default;
                    }

                    while (vecActiveRegisters != 0)
                    {
                        int reg = BitOperations.TrailingZeroCount(vecActiveRegisters);
                        ref LocalInfo info = ref GetLocalInfo(vecActive[reg]);

                        if (needSpill && !info.IsBlockLocal)
                        {
                            SpillRegister(ref info, dummyNode);
                        }
                        else
                        {
                            info.Register = default;
                        }

                        vecActiveRegisters &= ~(1 << reg);
                        vecActive[reg] = default;
                    }

                    block.Operations.Remove(dummyNode);
                }

                void FillRegister(ref LocalInfo info, Operation node)
                {
                    Debug.Assert(info.Register != default);
                    Debug.Assert(info.SpillOffset != default);

                    Operation fillOp = Operation(Instruction.Fill, info.Register, info.SpillOffset);

                    block.Operations.AddBefore(node, fillOp);
                }

                void SpillRegister(ref LocalInfo info, Operation node)
                {
                    Debug.Assert(info.Register != default);

                    if (info.SpillOffset == default)
                    {
                        info.SpillOffset = Const(stackAlloc.Allocate(info.Type));
                    }

                    Operation spillOp = Operation(Instruction.Spill, default, info.SpillOffset, info.Register);

                    block.Operations.AddBefore(node, spillOp);

                    info.Register = default;
                }
            }

            return new AllocationResult(intUsedRegisters, vecUsedRegisters, stackAlloc.TotalSize);
        }

        private static int UsesCount(Operand local)
        {
            return local.AssignmentsCount + local.UsesCount;
        }
    }
}