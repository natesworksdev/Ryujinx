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
            public int FixedRegisters { get; }

            public BlockInfo(int fixedRegisters)
            {
                FixedRegisters = fixedRegisters;
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

            // Cached to avoid redundant computations.
            public RegisterType RegisterType { get; }

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

                RegisterType = Type.ToRegisterType();

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

        private const int IntMask = (1 << RegistersCount) - 1;
        private const int VecMask = IntMask << RegistersCount;

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
            _blockInfo = new BlockInfo[cfg.Blocks.Count];
            _localInfo = new LocalInfo[cfg.Blocks.Count * 3];

            int localInfoCount = 0;

            for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];

                int fixedRegisters = 0;

                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
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
                            fixedRegisters |= 1 << Index(dest.GetRegister());
                        }
                    }
                }

                _blockInfo[block.Index] = new BlockInfo(fixedRegisters);
            }

            int usedRegisters = 0;
            int freeRegisters = Merge(regMasks.VecAvailableRegisters, regMasks.IntAvailableRegisters);
            int callerSavedRegisters = Merge(regMasks.VecCallerSavedRegisters, regMasks.IntCallerSavedRegisters);

            Operand[] active = new Operand[RegistersCount * 2];

            Operation dummyNode = Operation(Instruction.Extended, default);

            for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];
                BlockInfo blkInfo = _blockInfo[block.Index];

                int freeLocalRegisters = freeRegisters & ~blkInfo.FixedRegisters;
                int activeRegisters = 0;

                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    bool folded = false;

                    // Set of registers currently being used on the operation. These registers are __not__ candidate for
                    // allocation or spilling in the current operation.
                    int activeCurrRegisters = 0;

                    // If operation is a copy of a local and that local is living on the stack, we turn the copy into a
                    // fill, instead of inserting a fill before it.
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

                    // If operation is a call, spill caller saved registers which are in the active set.
                    if (node.Instruction == Instruction.Call)
                    {
                        int toSpill = callerSavedRegisters & activeRegisters;

                        while (toSpill != 0)
                        {
                            int reg = BitOperations.TrailingZeroCount(toSpill);

                            SpillRegister(ref GetLocalInfo(active[reg]), node);

                            activeRegisters &= ~(1 << reg);
                            activeCurrRegisters |= 1 << reg;
                            active[reg] = default;

                            toSpill &= ~(1 << reg);
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
                        int reg = Index(result.GetRegister());

                        activeCurrRegisters |= 1 << reg;

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

                            activeRegisters &= ~(1 << reg);
                            active[reg] = default;

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

                        int typeCount;
                        int typeMask;

                        if (info.RegisterType == RegisterType.Integer)
                        {
                            typeMask = IntMask;
                            typeCount = 0;
                        }
                        else
                        {
                            typeMask = VecMask;
                            typeCount = RegistersCount;
                        }

                        int mask = freeLocalRegisters & ~(activeRegisters | activeCurrRegisters) & typeMask;
                        int selectedReg;

                        // If we have inactive registers available, use one of them.
                        if (mask != 0)
                        {
                            selectedReg = BitOperations.TrailingZeroCount(mask);
                        }
                        // Otherwise we spill an active register and use the that register.
                        else
                        {
                            int spillMask = activeRegisters & ~activeCurrRegisters & typeMask;
                            int spillReg;
                            Operand spillLocal;

                            // The heuristic will select the first register which is holding a non block local. This is
                            // based on the assumption that block locals are more likely to be used next.
                            //
                            // TODO: Quite often, this assumption is not necessarily true, investigate other heuristics.
                            do
                            {
                                spillReg = BitOperations.TrailingZeroCount(spillMask);
                                spillLocal = active[spillReg];

                                if (!GetLocalInfo(spillLocal).IsBlockLocal)
                                {
                                    break;
                                }

                                spillMask &= ~(1 << spillReg);
                            }
                            while (spillMask != 0);

                            SpillRegister(ref GetLocalInfo(spillLocal), node);

                            selectedReg = spillReg;
                        }

                        info.Register = Register(selectedReg - typeCount, info.RegisterType, local.Type);

                        // Move selected register to the active set.
                        usedRegisters |= 1 << selectedReg;
                        activeRegisters |= 1 << selectedReg;
                        activeCurrRegisters |= 1 << selectedReg;
                        active[selectedReg] = local;

                        return info.Register;
                    }
                }

                // If there are still registers in the active set after allocation of the block, we spill them for the
                // next block.
                if (activeRegisters != 0)
                {
                    // If the block has 0 successors then the control flow exits. This means we can skip spilling since
                    // we're exiting anyways.
                    bool needSpill = block.SuccessorsCount > 0;

                    dummyNode = block.Append(dummyNode);

                    while (activeRegisters != 0)
                    {
                        int reg = BitOperations.TrailingZeroCount(activeRegisters);
                        ref LocalInfo info = ref GetLocalInfo(active[reg]);

                        if (needSpill && !info.IsBlockLocal)
                        {
                            SpillRegister(ref info, dummyNode);
                        }
                        else
                        {
                            info.Register = default;
                        }

                        activeRegisters &= ~(1 << reg);
                        active[reg] = default;
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

            var (vecUsedRegisters, intUsedRegisters) = Split(usedRegisters);

            return new AllocationResult(intUsedRegisters, vecUsedRegisters, stackAlloc.TotalSize);
        }

        private static int Index(Register reg)
        {
            int index = reg.Index;

            if (reg.Type == RegisterType.Vector)
            {
                index += RegistersCount;
            }

            return index;
        }

        private static int Merge(int vecSet, int intSet)
        {
            return vecSet << RegistersCount | intSet;
        }

        private static (int, int) Split(int set)
        {
            int intMask = (1 << RegistersCount) - 1;
            int vecMask = intMask << RegistersCount;

            return ((int)((uint)(set & vecMask) >> RegistersCount), set & intMask);
        }

        private static int UsesCount(Operand local)
        {
            return local.AssignmentsCount + local.UsesCount;
        }
    }
}