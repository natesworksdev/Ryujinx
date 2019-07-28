using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class FastLinearScan
    {
        private const int InstructionGap = 2;

        private const int RegistersCount = 16;

        private const int MaxIROperands = 4;

        private class OperationInfo
        {
            public LinkedListNode<Node> Node { get; }

            public int IntSpillUsedRegisters { get; set; }
            public int VecSpillUsedRegisters { get; set; }

            public OperationInfo(LinkedListNode<Node> node)
            {
                Node = node;
            }
        }

        private List<OperationInfo> _operationNodes;

        private int _intSpillTemps;
        private int _vecSpillTemps;

        private List<LiveInterval> _intervals;

        private class CompareIntervalsEnd : IComparer<LiveInterval>
        {
            public int Compare(LiveInterval lhs, LiveInterval rhs)
            {
                if (lhs.GetEnd() == rhs.GetEnd())
                {
                    return 0;
                }
                else if (lhs.GetEnd() < rhs.GetEnd())
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public AllocationResult RunPass(ControlFlowGraph cfg, StackAllocator stackAlloc, RegisterMasks regMasks)
        {
            PhiFunctions.Remove(cfg);

            BuildIntervals(cfg, regMasks);

            List<LiveInterval>[] fixedIntervals = new List<LiveInterval>[2];

            fixedIntervals[0] = new List<LiveInterval>();
            fixedIntervals[1] = new List<LiveInterval>();

            int intUsedRegisters = 0;
            int vecUsedRegisters = 0;

            for (int index = 0; index < RegistersCount * 2; index++)
            {
                LiveInterval interval = _intervals[index];

                if (!interval.IsEmpty)
                {
                    if (interval.Register.Type == RegisterType.Integer)
                    {
                        intUsedRegisters |= 1 << interval.Register.Index;
                    }
                    else /* if (interval.Register.Type == RegisterType.Vector) */
                    {
                        vecUsedRegisters |= 1 << interval.Register.Index;
                    }

                    InsertSorted(fixedIntervals[index & 1], interval);
                }
            }

            List<LiveInterval> activeIntervals = new List<LiveInterval>();

            CompareIntervalsEnd comparer = new CompareIntervalsEnd();

            int intFreeRegisters = regMasks.IntAvailableRegisters;
            int vecFreeRegisters = regMasks.VecAvailableRegisters;

            intFreeRegisters = ReserveSpillTemps(ref _intSpillTemps, intFreeRegisters);
            vecFreeRegisters = ReserveSpillTemps(ref _vecSpillTemps, vecFreeRegisters);

            for (int index = RegistersCount * 2; index < _intervals.Count; index++)
            {
                LiveInterval current = _intervals[index];

                while (activeIntervals.Count != 0 &&
                       activeIntervals[activeIntervals.Count - 1].GetEnd() < current.GetStart())
                {
                    int iIndex = activeIntervals.Count - 1;

                    LiveInterval interval = activeIntervals[iIndex];

                    if (interval.Register.Type == RegisterType.Integer)
                    {
                        intFreeRegisters |= 1 << interval.Register.Index;
                    }
                    else /* if (interval.Register.Type == RegisterType.Vector) */
                    {
                        vecFreeRegisters |= 1 << interval.Register.Index;
                    }

                    activeIntervals.RemoveAt(iIndex);
                }

                Operand local = current.Local;

                bool localIsInteger = local.Type.IsInteger();

                int freeMask = localIsInteger ? intFreeRegisters : vecFreeRegisters;

                if (freeMask != 0)
                {
                    List<LiveInterval> fixedIntervalsForType = fixedIntervals[localIsInteger ? 0 : 1];

                    for (int iIndex = 0; iIndex < fixedIntervalsForType.Count; iIndex++)
                    {
                        LiveInterval interval = fixedIntervalsForType[iIndex];

                        if (interval.GetStart() >= current.GetEnd())
                        {
                            break;
                        }

                        if (interval.Overlaps(current))
                        {
                            freeMask &= ~(1 << interval.Register.Index);
                        }
                    }
                }

                if (freeMask != 0)
                {
                    int selectedReg = BitUtils.LowestBitSet(freeMask);

                    current.Register = new Register(selectedReg, local.Type.ToRegisterType());

                    int regMask = 1 << selectedReg;

                    if (localIsInteger)
                    {
                        intUsedRegisters |=  regMask;
                        intFreeRegisters &= ~regMask;
                    }
                    else
                    {
                        vecUsedRegisters |=  regMask;
                        vecFreeRegisters &= ~regMask;
                    }
                }
                else
                {
                    // Spill the interval that will free the register for the longest
                    // amount of time, as long there's no interference of the current
                    // interval with a fixed interval using the same register.
                    bool hasRegisterSelected = false;

                    RegisterType regType = current.Local.Type.ToRegisterType();

                    for (int iIndex = 0; iIndex < activeIntervals.Count; iIndex++)
                    {
                        LiveInterval spillCandidate = activeIntervals[iIndex];

                        if (spillCandidate.Register.Type != regType)
                        {
                            continue;
                        }

                        LiveInterval fixedInterval = _intervals[GetRegisterId(spillCandidate.Register)];

                        if (fixedInterval.IsEmpty || !fixedInterval.Overlaps(current))
                        {
                            current.Register = spillCandidate.Register;

                            spillCandidate.Spill(stackAlloc.Allocate(spillCandidate.Local.Type));

                            activeIntervals.RemoveAt(iIndex);

                            hasRegisterSelected = true;

                            break;
                        }
                    }

                    Debug.Assert(hasRegisterSelected, "Failure allocating register with spill.");
                }

                InsertSorted(activeIntervals, current, comparer);
            }

            for (int index = RegistersCount * 2; index < _intervals.Count; index++)
            {
                LiveInterval interval = _intervals[index];

                if (interval.IsSpilled)
                {
                    ReplaceLocalWithSpill(interval, ref intUsedRegisters, ref vecUsedRegisters);
                }
                else
                {
                    ReplaceLocalWithRegister(interval);
                }
            }

            return new AllocationResult(intUsedRegisters, vecUsedRegisters, stackAlloc.TotalSize);
        }

        private int ReserveSpillTemps(ref int tempsMask, int availableRegs)
        {
            for (int index = 0; index < MaxIROperands; index++)
            {
                int selectedReg = BitUtils.HighestBitSet(availableRegs);

                tempsMask |= 1 << selectedReg;

                availableRegs &= ~(1 << selectedReg);
            }

            return availableRegs;
        }

        private void ReplaceLocalWithRegister(LiveInterval interval)
        {
            Operand register = GetRegister(interval);

            foreach (int usePosition in interval.UsePositions())
            {
                Node operation = GetOperationInfo(usePosition).Node.Value;

                for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
                {
                    Operand source = operation.GetSource(srcIndex);

                    if (source == interval.Local)
                    {
                        operation.SetSource(srcIndex, register);
                    }
                }

                if (operation.Destination == interval.Local)
                {
                    operation.Destination = register;
                }
            }
        }

        private static Operand GetRegister(LiveInterval interval)
        {
            Debug.Assert(!interval.IsSpilled, "Spilled intervals are not allowed.");

            return new Operand(
                interval.Register.Index,
                interval.Register.Type,
                interval.Local.Type);
        }

        private void ReplaceLocalWithSpill(
            LiveInterval interval,
            ref int intUsedRegisters,
            ref int vecUsedRegisters)
        {
            Operand local = interval.Local;

            int spillOffset = interval.SpillOffset;

            foreach (int usePosition in interval.UsePositions())
            {
                OperationInfo info = GetOperationInfo(usePosition);

                int tempReg = GetSpillTemp(info, local.Type);

                if (local.Type.IsInteger())
                {
                    intUsedRegisters |= 1 << tempReg;
                }
                else
                {
                    vecUsedRegisters |= 1 << tempReg;
                }

                Operand temp = new Operand(tempReg, local.Type.ToRegisterType(), local.Type);

                LinkedListNode<Node> node = info.Node;

                Node operation = node.Value;

                for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
                {
                    Operand source = operation.GetSource(srcIndex);

                    if (source == local)
                    {
                        Operation fillOp = new Operation(Instruction.Fill, temp, Const(spillOffset));

                        node.List.AddBefore(node, fillOp);

                        operation.SetSource(srcIndex, temp);
                    }
                }

                if (operation.Destination == local)
                {
                    Operation spillOp = new Operation(Instruction.Spill, null, Const(spillOffset), temp);

                    node.List.AddAfter(node, spillOp);

                    operation.Destination = temp;
                }
            }
        }

        private OperationInfo GetOperationInfo(int position)
        {
            return _operationNodes[position / InstructionGap];
        }

        private int GetSpillTemp(OperationInfo info, OperandType type)
        {
            int selectedReg;

            if (type.IsInteger())
            {
                selectedReg = BitUtils.LowestBitSet(_intSpillTemps & ~info.IntSpillUsedRegisters);

                info.IntSpillUsedRegisters |= 1 << selectedReg;
            }
            else
            {
                selectedReg = BitUtils.LowestBitSet(_vecSpillTemps & ~info.VecSpillUsedRegisters);

                info.VecSpillUsedRegisters |= 1 << selectedReg;
            }

            Debug.Assert(selectedReg != -1, "Out of spill temporary registers. " + (info.Node.Value as Operation).Instruction);

            return selectedReg;
        }

        private static void InsertSorted(
            List<LiveInterval> list,
            LiveInterval interval,
            IComparer<LiveInterval> comparer = null)
        {
            int insertIndex = list.BinarySearch(interval, comparer);

            if (insertIndex < 0)
            {
                insertIndex = ~insertIndex;
            }

            list.Insert(insertIndex, interval);
        }

        private void BuildIntervals(ControlFlowGraph cfg, RegisterMasks masks)
        {
            _operationNodes = new List<OperationInfo>();

            _intervals = new List<LiveInterval>();

            for (int index = 0; index < RegistersCount; index++)
            {
                _intervals.Add(new LiveInterval(new Register(index, RegisterType.Integer)));
                _intervals.Add(new LiveInterval(new Register(index, RegisterType.Vector)));
            }

            HashSet<Operand> visited = new HashSet<Operand>();

            LiveInterval GetOrAddInterval(Operand operand)
            {
                LiveInterval interval;

                if (visited.Add(operand))
                {
                    operand.NumberLocal(_intervals.Count);

                    interval = new LiveInterval(operand);

                    _intervals.Add(interval);
                }
                else
                {
                    interval = _intervals[GetOperandId(operand)];
                }

                return interval;
            }

            int[] blockStarts = new int[cfg.Blocks.Count];

            int operationPos = 0;

            List<LiveRange> backwardsBranches = new List<LiveRange>();

            for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];

                blockStarts[block.Index] = operationPos;

                for (LinkedListNode<Node> node = block.Operations.First; node != null; node = node.Next)
                {
                    _operationNodes.Add(new OperationInfo(node));

                    Operation operation = node.Value as Operation;

                    // Note: For fixed intervals, we must process sources first, in
                    // order to extend the live range of the fixed interval to the last
                    // use, in case the register is both used and assigned on the same
                    // instruction.
                    for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
                    {
                        Operand source = operation.GetSource(srcIndex);

                        if (source.Kind == OperandKind.LocalVariable)
                        {
                            LiveInterval interval = GetOrAddInterval(source);

                            Debug.Assert(!interval.IsEmpty, "Interval is empty.");

                            interval.SetEnd(operationPos + 1);
                            interval.AddUsePosition(operationPos);
                        }
                        else if (source.Kind == OperandKind.Register)
                        {
                            int iIndex = GetRegisterId(source.GetRegister());

                            LiveInterval interval = _intervals[iIndex];

                            if (interval.IsEmpty)
                            {
                                interval.SetStart(operationPos + 1);
                            }
                            else if (interval.GetEnd() < operationPos + 1)
                            {
                                interval.SetEnd(operationPos + 1);
                            }
                        }
                    }

                    Operand dest = operation.Destination;

                    if (dest != null)
                    {
                        if (dest.Kind == OperandKind.LocalVariable)
                        {
                            LiveInterval interval = GetOrAddInterval(dest);

                            if (interval.IsEmpty)
                            {
                                interval.SetStart(operationPos + 1);
                            }

                            interval.AddUsePosition(operationPos);
                        }
                        else if (dest.Kind == OperandKind.Register)
                        {
                            int iIndex = GetRegisterId(dest.GetRegister());

                            _intervals[iIndex].AddRange(operationPos + 1, operationPos + InstructionGap);
                        }
                    }

                    if (operation.Instruction == Instruction.Call)
                    {
                        AddIntervalCallerSavedReg(masks.IntCallerSavedRegisters, operationPos, RegisterType.Integer);
                        AddIntervalCallerSavedReg(masks.VecCallerSavedRegisters, operationPos, RegisterType.Vector);
                    }

                    operationPos += InstructionGap;
                }

                foreach (BasicBlock successor in Successors(block))
                {
                    int branchIndex = cfg.PostOrderMap[block.Index];
                    int targetIndex = cfg.PostOrderMap[successor.Index];

                    // Is the branch jumping backwards?
                    if (targetIndex >= branchIndex)
                    {
                        int targetPos = blockStarts[successor.Index];

                        backwardsBranches.Add(new LiveRange(targetPos, operationPos));
                    }
                }
            }

            foreach (LiveRange backwardBranch in backwardsBranches)
            {
                for (int iIndex = RegistersCount * 2; iIndex < _intervals.Count; iIndex++)
                {
                    LiveInterval interval = _intervals[iIndex];

                    int start = interval.GetStart();
                    int end   = interval.GetEnd();

                    if (backwardBranch.Start >= start && backwardBranch.Start < end)
                    {
                        if (interval.GetEnd() < backwardBranch.End)
                        {
                            interval.SetEnd(backwardBranch.End);
                        }
                    }

                    if (start > backwardBranch.Start)
                    {
                        break;
                    }
                }
            }
        }

        private void AddIntervalCallerSavedReg(int mask, int operationPos, RegisterType regType)
        {
            while (mask != 0)
            {
                int regIndex = BitUtils.LowestBitSet(mask);

                Register callerSavedReg = new Register(regIndex, regType);

                int rIndex = GetRegisterId(callerSavedReg);

                _intervals[rIndex].AddRange(operationPos + 1, operationPos + InstructionGap);

                mask &= ~(1 << regIndex);
            }
        }

        private static int GetOperandId(Operand operand)
        {
            if (operand.Kind == OperandKind.LocalVariable)
            {
                return operand.AsInt32();
            }
            else if (operand.Kind == OperandKind.Register)
            {
                return GetRegisterId(operand.GetRegister());
            }
            else
            {
                throw new ArgumentException($"Invalid operand kind \"{operand.Kind}\".");
            }
        }

        private static int GetRegisterId(Register register)
        {
            return (register.Index << 1) | (register.Type == RegisterType.Vector ? 1 : 0);
        }

        private static IEnumerable<BasicBlock> Successors(BasicBlock block)
        {
            if (block.Next != null)
            {
                yield return block.Next;
            }

            if (block.Branch != null)
            {
                yield return block.Branch;
            }
        }
    }
}