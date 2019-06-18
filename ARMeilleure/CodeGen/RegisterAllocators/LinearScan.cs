using ARMeilleure.CodeGen.X86;
using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class LinearScan
    {
        private const int InstructionGap     = 2;
        private const int InstructionGapMask = InstructionGap - 1;

        private const int RegistersCount = 16;

        private HashSet<int> _blockEdges;

        private LiveRange[] _blockRanges;

        private BitMap[] _blockLiveIn;

        private List<LiveInterval> _intervals;

        private LiveInterval[] _parentIntervals;

        private List<LinkedListNode<Node>> _operationNodes;

        private int _operationsCount;

        private class AllocationContext
        {
            public RegisterMasks Masks { get; }

            public StackAllocator StackAlloc { get; }

            public BitMap Active   { get; }
            public BitMap Inactive { get; }

            public int IntUsedRegisters { get; set; }
            public int VecUsedRegisters { get; set; }

            public AllocationContext(RegisterMasks masks, int intervalsCount)
            {
                Masks = masks;

                StackAlloc = new StackAllocator();

                Active   = new BitMap(intervalsCount);
                Inactive = new BitMap(intervalsCount);
            }

            public void MoveActiveToInactive(int bit)
            {
                Move(Active, Inactive, bit);
            }

            public void MoveInactiveToActive(int bit)
            {
                Move(Inactive, Active, bit);
            }

            private static void Move(BitMap source, BitMap dest, int bit)
            {
                source.Clear(bit);

                dest.Set(bit);
            }
        }

        public AllocationResult RunPass(ControlFlowGraph cfg, RegisterMasks regMasks)
        {
            PhiFunctions.Remove(cfg);

            NumberLocals(cfg);

            AllocationContext context = new AllocationContext(regMasks, _intervals.Count);

            BuildIntervals(cfg, context, out int maxCallArgs);

            for (int index = 0; index < _intervals.Count; index++)
            {
                LiveInterval current = _intervals[index];

                if (current.IsEmpty)
                {
                    continue;
                }

                if (current.IsFixed)
                {
                    context.Active.Set(index);

                    continue;
                }

                if (!current.HasRegister)
                {
                    AllocateInterval(context, current, index);
                }
            }

            for (int index = RegistersCount * 2; index < _intervals.Count; index++)
            {
                if (!_intervals[index].IsSpilled)
                {
                    ReplaceLocalWithRegister(_intervals[index]);
                }
            }

            InsertSplitCopies();
            InsertSplitCopiesAtEdges(cfg);

            return new AllocationResult(
                context.IntUsedRegisters,
                context.VecUsedRegisters,
                context.StackAlloc.TotalSize,
                maxCallArgs);
        }

        private void AllocateInterval(AllocationContext context, LiveInterval current, int cIndex)
        {
            //Check active intervals that already ended.
            foreach (int iIndex in context.Active)
            {
                LiveInterval interval = _intervals[iIndex];

                if (interval.GetEnd() < current.GetStart())
                {
                    context.Active.Clear(iIndex);
                }
                else if (!interval.Overlaps(current.GetStart()))
                {
                    context.MoveActiveToInactive(iIndex);
                }
            }

            //Check inactive intervals that already ended or were reactivated.
            foreach (int iIndex in context.Inactive)
            {
                LiveInterval interval = _intervals[iIndex];

                if (interval.GetEnd() < current.GetStart())
                {
                    context.Inactive.Clear(iIndex);
                }
                else if (interval.Overlaps(current.GetStart()))
                {
                    context.MoveInactiveToActive(iIndex);
                }
            }

            if (!TryAllocateRegWithoutSpill(context, current, cIndex))
            {
                AllocateRegWithSpill(context, current, cIndex);
            }
        }

        private bool TryAllocateRegWithoutSpill(AllocationContext context, LiveInterval current, int cIndex)
        {
            RegisterType regType = current.Local.Type.ToRegisterType();

            int availableRegisters = context.Masks.GetAvailableRegisters(regType);

            int[] freePositions = new int[RegistersCount];

            for (int index = 0; index < RegistersCount; index++)
            {
                if ((availableRegisters & (1 << index)) != 0)
                {
                    freePositions[index] = int.MaxValue;
                }
            }

            foreach (int iIndex in context.Active)
            {
                LiveInterval interval = _intervals[iIndex];

                if (interval.Register.Type == regType)
                {
                    freePositions[interval.Register.Index] = 0;
                }
            }

            foreach (int iIndex in context.Inactive)
            {
                LiveInterval interval = _intervals[iIndex];

                if (interval.Register.Type == regType && interval.Overlaps(current))
                {
                    int nextOverlap = interval.NextOverlap(current);

                    if (freePositions[interval.Register.Index] > nextOverlap && nextOverlap != -1)
                    {
                        freePositions[interval.Register.Index] = nextOverlap;
                    }
                }
            }

            int selectedReg = GetHighestValueIndex(freePositions);

            int selectedNextUse = freePositions[selectedReg];

            if (GetSplitPosition(selectedNextUse) <= current.GetStart())
            {
                return false;
            }
            else if (selectedNextUse < current.GetEnd())
            {
                int splitPosition = GetSplitPosition(selectedNextUse);

                Debug.Assert(splitPosition > current.GetStart(), "Trying to split interval at the start.");

                LiveInterval splitChild = current.Split(splitPosition);

                if (splitChild.UsesCount != 0)
                {
                    Debug.Assert(splitChild.GetStart() > current.GetStart(), "Split interval has an invalid start position.");

                    InsertInterval(splitChild);
                }
                else
                {
                    Spill(context, splitChild);
                }
            }

            current.Register = new Register(selectedReg, regType);

            if (regType == RegisterType.Integer)
            {
                context.IntUsedRegisters |= 1 << selectedReg;
            }
            else /* if (regType == RegisterType.Vector) */
            {
                context.VecUsedRegisters |= 1 << selectedReg;
            }

            context.Active.Set(cIndex);

            return true;
        }

        private void AllocateRegWithSpill(AllocationContext context, LiveInterval current, int cIndex)
        {
            RegisterType regType = current.Local.Type.ToRegisterType();

            int availableRegisters = context.Masks.GetAvailableRegisters(regType);

            int[] usePositions = new int[RegistersCount];

            int[] blockedPositions = new int[RegistersCount];

            for (int index = 0; index < RegistersCount; index++)
            {
                if ((availableRegisters & (1 << index)) != 0)
                {
                    usePositions[index] = int.MaxValue;

                    blockedPositions[index] = int.MaxValue;
                }
            }

            void SetUsePosition(int index, int position)
            {
                usePositions[index] = Math.Min(usePositions[index], position);
            }

            void SetBlockedPosition(int index, int position)
            {
                blockedPositions[index] = Math.Min(blockedPositions[index], position);

                SetUsePosition(index, position);
            }

            foreach (int iIndex in context.Active)
            {
                LiveInterval interval = _intervals[iIndex];

                if (!interval.IsFixed && interval.Register.Type == regType)
                {
                    int nextUse = interval.NextUseAfter(current.GetStart());

                    if (nextUse != -1)
                    {
                        SetUsePosition(interval.Register.Index, nextUse);
                    }
                }
            }

            foreach (int iIndex in context.Inactive)
            {
                LiveInterval interval = _intervals[iIndex];

                if (!interval.IsFixed && interval.Register.Type == regType && interval.Overlaps(current))
                {
                    int nextUse = interval.NextUseAfter(current.GetStart());

                    if (nextUse != -1)
                    {
                        SetUsePosition(interval.Register.Index, nextUse);
                    }
                }
            }

            foreach (int iIndex in context.Active)
            {
                LiveInterval interval = _intervals[iIndex];

                if (interval.IsFixed && interval.Register.Type == regType)
                {
                    SetBlockedPosition(interval.Register.Index, 0);
                }
            }

            foreach (int iIndex in context.Inactive)
            {
                LiveInterval interval = _intervals[iIndex];

                if (interval.IsFixed && interval.Register.Type == regType && interval.Overlaps(current))
                {
                    SetBlockedPosition(interval.Register.Index, interval.NextOverlap(current));
                }
            }

            int selectedReg = GetHighestValueIndex(usePositions);

            int currentFirstUse = current.FirstUse();

            Debug.Assert(currentFirstUse >= 0, "Current interval has no uses.");

            if (usePositions[selectedReg] < currentFirstUse)
            {
                //All intervals on inactive and active are being used before current,
                //so spill the current interval.
                Debug.Assert(currentFirstUse > current.GetStart(), "Trying to spill a interval currently being used.");

                LiveInterval splitChild = current.Split(GetSplitPosition(currentFirstUse));

                Debug.Assert(splitChild.GetStart() > current.GetStart(), "Split interval has an invalid start position.");

                InsertInterval(splitChild);

                Spill(context, current);
            }
            else if (blockedPositions[selectedReg] > current.GetEnd())
            {
                //Spill made the register available for the entire current lifetime,
                //so we only need to split the intervals using the selected register.
                current.Register = new Register(selectedReg, regType);

                SplitAndSpillOverlappingIntervals(context, current);

                context.Active.Set(cIndex);
            }
            else
            {
                //There are conflicts even after spill due to the use of fixed registers
                //that can't be spilled, so we need to also split current at the point of
                //the first fixed register use.
                current.Register = new Register(selectedReg, regType);

                LiveInterval splitChild = current.Split(GetSplitPosition(blockedPositions[selectedReg]));

                if (splitChild.UsesCount != 0)
                {
                    Debug.Assert(splitChild.GetStart() > current.GetStart(), "Split interval has an invalid start position.");

                    InsertInterval(splitChild);
                }
                else
                {
                    Spill(context, splitChild);
                }

                SplitAndSpillOverlappingIntervals(context, current);

                context.Active.Set(cIndex);
            }
        }

        private static int GetHighestValueIndex(int[] array)
        {
            int higuest = array[0];

            if (higuest == int.MaxValue)
            {
                return 0;
            }

            int selected = 0;

            for (int index = 1; index < array.Length; index++)
            {
                int current = array[index];

                if (higuest < current)
                {
                    higuest = current;

                    selected = index;

                    if (current == int.MaxValue)
                    {
                        break;
                    }
                }
            }

            return selected;
        }

        private void SplitAndSpillOverlappingIntervals(AllocationContext context, LiveInterval current)
        {
            foreach (int iIndex in context.Active)
            {
                LiveInterval interval = _intervals[iIndex];

                if (!interval.IsFixed && interval.Register == current.Register)
                {
                    SplitAndSpillOverlappingInterval(context, current, interval);

                    context.Active.Clear(iIndex);
                }
            }

            foreach (int iIndex in context.Inactive)
            {
                LiveInterval interval = _intervals[iIndex];

                if (!interval.IsFixed && interval.Register == current.Register && interval.Overlaps(current))
                {
                    SplitAndSpillOverlappingInterval(context, current, interval);

                    context.Inactive.Clear(iIndex);
                }
            }
        }

        private void SplitAndSpillOverlappingInterval(
            AllocationContext context,
            LiveInterval      current,
            LiveInterval      interval)
        {
            //If there's a next use after the start of the current interval,
            //we need to split the spilled interval twice, and re-insert it
            //on the "pending" list to ensure that it will get a new register
            //on that use position.
            int nextUse = interval.NextUseAfter(current.GetStart());

            LiveInterval splitChild;

            if (interval.GetStart() < current.GetStart())
            {
                splitChild = interval.Split(GetSplitPosition(current.GetStart()));
            }
            else
            {
                splitChild = interval;
            }

            if (nextUse != -1)
            {
                Debug.Assert(nextUse > current.GetStart(), "Trying to spill a interval currently being used.");

                if (GetSplitPosition(nextUse) > splitChild.GetStart())
                {
                    LiveInterval right = splitChild.Split(GetSplitPosition(nextUse));

                    Spill(context, splitChild);

                    Debug.Assert(right.GetStart() > current.GetStart(), "Split interval has an invalid start position.");

                    InsertInterval(right);
                }
                else
                {
                    if (nextUse == splitChild.GetStart())
                    {
                        splitChild.SetStart(GetSplitPosition(nextUse));
                    }

                    Debug.Assert(splitChild.GetStart() > current.GetStart(), "Split interval has an invalid start position.");

                    InsertInterval(splitChild);
                }
            }
            else
            {
                Spill(context, splitChild);
            }
        }

        private void InsertInterval(LiveInterval interval)
        {
            Debug.Assert(interval.UsesCount != 0, "Trying to insert a interval without uses.");
            Debug.Assert(!interval.IsEmpty,       "Trying to insert a empty interval.");
            Debug.Assert(!interval.IsSpilled,     "Trying to insert a spilled interval.");

            int insertIndex = _intervals.BinarySearch(interval);

            if (insertIndex < 0)
            {
                insertIndex = ~insertIndex;
            }

            _intervals.Insert(insertIndex, interval);
        }

        private void Spill(AllocationContext context, LiveInterval interval)
        {
            Debug.Assert(!interval.IsFixed,       "Trying to spill a fixed interval.");
            Debug.Assert(interval.UsesCount == 0, "Trying to spill a interval with uses.");

            //We first check if any of the siblings were spilled, if so we can reuse
            //the stack offset. Otherwise, we allocate a new space on the stack.
            //This prevents stack-to-stack copies being necessary for a split interval.
            if (!interval.TrySpillWithSiblingOffset())
            {
                interval.Spill(context.StackAlloc.Allocate(interval.Local.Type));
            }
        }

        private void InsertSplitCopies()
        {
            Dictionary<int, CopyResolver> copyResolvers = new Dictionary<int, CopyResolver>();

            CopyResolver GetCopyResolver(int position)
            {
                CopyResolver copyResolver = new CopyResolver();

                if (copyResolvers.TryAdd(position, copyResolver))
                {
                    return copyResolver;
                }

                return copyResolvers[position];
            }

            foreach (LiveInterval interval in _intervals.Where(x => x.IsSplit))
            {
                LiveInterval previous = interval;

                foreach (LiveInterval splitChild in interval.SplitChilds())
                {
                    int splitPosition = splitChild.GetStart();

                    int alignedSplitPosition = GetAlignedSplitPosition(splitPosition);

                    if (!_blockEdges.Contains(alignedSplitPosition) && previous.GetEnd() == splitPosition)
                    {
                        GetCopyResolver(splitPosition).AddSplit(previous, splitChild);
                    }

                    previous = splitChild;
                }
            }

            foreach (KeyValuePair<int, CopyResolver> kv in copyResolvers)
            {
                CopyResolver copyResolver = kv.Value;

                if (!copyResolver.HasCopy)
                {
                    continue;
                }

                int splitPosition = kv.Key;

                LinkedListNode<Node> node = GetOperationNode(splitPosition);

                Operation[] sequence = copyResolver.Sequence();

                node = node.List.AddAfter(node, sequence[0]);

                for (int index = 1; index < sequence.Length; index++)
                {
                    node = node.List.AddAfter(node, sequence[index]);
                }
            }
        }

        private void InsertSplitCopiesAtEdges(ControlFlowGraph cfg)
        {
            int blocksCount = cfg.Blocks.Count;

            bool IsSplitEdgeBlock(BasicBlock block)
            {
                return block.Index >= blocksCount;
            }

            for (LinkedListNode<BasicBlock> node = cfg.Blocks.First; node != null; node = node.Next)
            {
                BasicBlock block = node.Value;

                if (IsSplitEdgeBlock(block))
                {
                    continue;
                }

                bool hasSingleOrNoSuccessor = block.Next == null || block.Branch == null;

                foreach (BasicBlock successor in Successors(block))
                {
                    int succIndex = successor.Index;

                    //If the current node is a split node, then the actual successor node
                    //(the successor before the split) should be right after it.
                    if (IsSplitEdgeBlock(successor))
                    {
                        succIndex = Successors(successor).First().Index;
                    }

                    CopyResolver copyResolver = new CopyResolver();

                    foreach (int iIndex in _blockLiveIn[succIndex])
                    {
                        LiveInterval interval = _parentIntervals[iIndex];

                        if (!interval.IsSplit)
                        {
                            continue;
                        }

                        int lEnd   = _blockRanges[block.Index].End - InstructionGap;
                        int rStart = _blockRanges[succIndex].Start;

                        LiveInterval left  = interval.GetSplitChild(lEnd);
                        LiveInterval right = interval.GetSplitChild(rStart);

                        if (left != right)
                        {
                            copyResolver.AddSplit(left, right);
                        }
                    }

                    if (!copyResolver.HasCopy)
                    {
                        continue;
                    }

                    Operation[] sequence = copyResolver.Sequence();

                    if (hasSingleOrNoSuccessor)
                    {
                        foreach (Operation operation in sequence)
                        {
                            block.Append(operation);
                        }
                    }
                    else if (successor.Predecessors.Count == 1)
                    {
                        LinkedListNode<Node> prependNode = successor.Operations.AddFirst(sequence[0]);

                        for (int index = 1; index < sequence.Length; index++)
                        {
                            Operation operation = sequence[index];

                            prependNode = successor.Operations.AddAfter(prependNode, operation);
                        }
                    }
                    else
                    {
                        //Split the critical edge.
                        BasicBlock splitBlock = cfg.SplitEdge(block, successor);

                        foreach (Operation operation in sequence)
                        {
                            splitBlock.Append(operation);
                        }
                    }
                }
            }
        }

        private void ReplaceLocalWithRegister(LiveInterval current)
        {
            Operand register = GetRegister(current);

            foreach (int usePosition in current.UsePositions())
            {
                Node operation = GetOperationNode(usePosition).Value;

                for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
                {
                    Operand source = operation.GetSource(srcIndex);

                    if (source == current.Local)
                    {
                        operation.SetSource(srcIndex, register);
                    }
                    else if (source is X86MemoryOperand memOp)
                    {
                        if (memOp.BaseAddress == current.Local)
                        {
                            memOp.BaseAddress = register;
                        }

                        if (memOp.Index == current.Local)
                        {
                            memOp.Index = register;
                        }
                    }
                }

                if (operation.Dest == current.Local)
                {
                    operation.Dest = register;
                }
            }
        }

        private static Operand GetRegister(LiveInterval interval)
        {
            if (interval.IsSpilled)
            {
                throw new ArgumentException("Spilled intervals are not allowed.");
            }

            return new Operand(
                interval.Register.Index,
                interval.Register.Type,
                interval.Local.Type);
        }

        private static int GetSplitPosition(int position)
        {
            return (position & InstructionGapMask) == 0 ? position - 1 : position;
        }

        private static int GetAlignedSplitPosition(int position)
        {
            return (position + InstructionGapMask) & ~InstructionGapMask;
        }

        private LinkedListNode<Node> GetOperationNode(int position)
        {
            return _operationNodes[position / InstructionGap];
        }

        private void NumberLocals(ControlFlowGraph cfg)
        {
            _operationNodes = new List<LinkedListNode<Node>>();

            _intervals = new List<LiveInterval>();

            for (int index = 0; index < RegistersCount; index++)
            {
                _intervals.Add(new LiveInterval(new Register(index, RegisterType.Integer)));
                _intervals.Add(new LiveInterval(new Register(index, RegisterType.Vector)));
            }

            HashSet<Operand> visited = new HashSet<Operand>();

            _operationsCount = 0;

            for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];

                for (LinkedListNode<Node> node = block.Operations.First; node != null; node = node.Next)
                {
                    _operationNodes.Add(node);

                    Node operation = node.Value;

                    Operand dest = operation.Dest;

                    if (dest != null && dest.Kind == OperandKind.LocalVariable && visited.Add(dest))
                    {
                        dest.NumberLocal(_intervals.Count);

                        _intervals.Add(new LiveInterval(dest));
                    }
                }

                _operationsCount += block.Operations.Count * InstructionGap;
            }

            _parentIntervals = _intervals.ToArray();
        }

        private void BuildIntervals(ControlFlowGraph cfg, AllocationContext context, out int maxCallArgs)
        {
            maxCallArgs = 0;

            _blockRanges = new LiveRange[cfg.Blocks.Count];

            int mapSize = _intervals.Count;

            BitMap[] blkLiveGen  = new BitMap[cfg.Blocks.Count];
            BitMap[] blkLiveKill = new BitMap[cfg.Blocks.Count];

            //Compute local live sets.
            foreach (BasicBlock block in cfg.Blocks)
            {
                BitMap liveGen  = new BitMap(mapSize);
                BitMap liveKill = new BitMap(mapSize);

                foreach (Node node in block.Operations)
                {
                    foreach (Operand source in Operands(node))
                    {
                        int id = GetOperandId(source);

                        if (!liveKill.IsSet(id))
                        {
                            liveGen.Set(id);
                        }
                    }

                    if (node.Dest != null && IsLocalOrRegister(node.Dest.Kind))
                    {
                        liveKill.Set(GetOperandId(node.Dest));
                    }
                }

                blkLiveGen [block.Index] = liveGen;
                blkLiveKill[block.Index] = liveKill;
            }

            //Compute global live sets.
            BitMap[] blkLiveIn  = new BitMap[cfg.Blocks.Count];
            BitMap[] blkLiveOut = new BitMap[cfg.Blocks.Count];

            for (int index = 0; index < cfg.Blocks.Count; index++)
            {
                blkLiveIn [index] = new BitMap(mapSize);
                blkLiveOut[index] = new BitMap(mapSize);
            }

            bool modified;

            do
            {
                modified = false;

                for (int index = 0; index < cfg.PostOrderBlocks.Length; index++)
                {
                    BasicBlock block = cfg.PostOrderBlocks[index];

                    BitMap liveOut = blkLiveOut[block.Index];

                    foreach (BasicBlock successor in Successors(block))
                    {
                        if (liveOut.Set(blkLiveIn[successor.Index]))
                        {
                            modified = true;
                        }
                    }

                    BitMap liveIn = blkLiveIn[block.Index];

                    liveIn.Set  (liveOut);
                    liveIn.Clear(blkLiveKill[block.Index]);
                    liveIn.Set  (blkLiveGen [block.Index]);
                }
            }
            while (modified);

            _blockLiveIn = blkLiveIn;

            _blockEdges = new HashSet<int>();

            //Compute lifetime intervals.
            int operationPos = _operationsCount;

            for (int index = 0; index < cfg.PostOrderBlocks.Length; index++)
            {
                BasicBlock block = cfg.PostOrderBlocks[index];

                if (block.Operations.Count == 0)
                {
                    _blockRanges[block.Index] = new LiveRange(operationPos, operationPos + InstructionGap);

                    continue;
                }

                int blockStart = operationPos - block.Operations.Count * InstructionGap;
                int blockEnd   = operationPos;

                _blockRanges[block.Index] = new LiveRange(blockStart, blockEnd);

                _blockEdges.Add(blockStart);

                BitMap liveOut = blkLiveOut[block.Index];

                foreach (int id in liveOut)
                {
                    _intervals[id].AddRange(blockStart, blockEnd);
                }

                foreach (Node node in BottomOperations(block))
                {
                    operationPos -= InstructionGap;

                    foreach (Operand source in Operands(node))
                    {
                        LiveInterval interval = _intervals[GetOperandId(source)];

                        interval.AddRange(blockStart, operationPos);
                        interval.AddUsePosition(operationPos);
                    }

                    Operand dest = node.Dest;

                    if (dest != null && IsLocalOrRegister(dest.Kind))
                    {
                        LiveInterval interval = _intervals[GetOperandId(dest)];

                        interval.SetStart(operationPos);
                        interval.AddUsePosition(operationPos);
                    }

                    if (node is Operation operation)
                    {
                        if (operation.Inst == Instruction.Call)
                        {
                            AddIntervalCallerSavedReg(context.Masks.IntCallerSavedRegisters, operationPos, RegisterType.Integer);
                            AddIntervalCallerSavedReg(context.Masks.VecCallerSavedRegisters, operationPos, RegisterType.Vector);

                            if (maxCallArgs < operation.SourcesCount - 1)
                            {
                                maxCallArgs = operation.SourcesCount - 1;
                            }
                        }
                        else if (operation.Inst == Instruction.StackAlloc)
                        {
                            Operand offset = operation.GetSource(0);

                            Debug.Assert(offset.Kind == OperandKind.Constant, "StackAlloc has non-constant size.");

                            int spillOffset = context.StackAlloc.Allocate(offset.AsInt32());

                            operation.SetSource(0, new Operand(spillOffset));
                        }
                    }
                }
            }
        }

        private void AddIntervalCallerSavedReg(int mask, int operationPos, RegisterType regType)
        {
            while (mask != 0)
            {
                int regIndex = BitUtils.LowestBitSet(mask);

                Debug.Assert(regIndex < RegistersCount, "Invalid register index.");

                Register callerSavedReg = new Register(regIndex, regType);

                LiveInterval interval = _intervals[GetRegisterId(callerSavedReg)];

                interval.AddRange(operationPos, operationPos + 1);

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

        private static IEnumerable<Node> BottomOperations(BasicBlock block)
        {
            LinkedListNode<Node> node = block.Operations.Last;

            while (node != null && !(node.Value is PhiNode))
            {
                yield return node.Value;

                node = node.Previous;
            }
        }

        private static IEnumerable<Operand> Operands(Node node)
        {
            for (int index = 0; index < node.SourcesCount; index++)
            {
                Operand source = node.GetSource(index);

                if (IsLocalOrRegister(source.Kind))
                {
                    yield return source;
                }
                else if (source is X86MemoryOperand memOp)
                {
                    if (IsLocalOrRegister(memOp.BaseAddress.Kind))
                    {
                        yield return memOp.BaseAddress;
                    }

                    if (memOp.Index != null && IsLocalOrRegister(memOp.Index.Kind))
                    {
                        yield return memOp.Index;
                    }
                }
            }
        }

        private static bool IsLocalOrRegister(OperandKind kind)
        {
            return kind == OperandKind.LocalVariable ||
                   kind == OperandKind.Register;
        }
    }
}