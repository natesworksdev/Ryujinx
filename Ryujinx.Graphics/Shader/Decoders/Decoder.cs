using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Shader.Instructions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Decoders
{
    static class Decoder
    {
        private const long HeaderSize = 0x50;

        private delegate object OpActivator(InstEmitter emitter, ulong address, long opCode);

        private static ConcurrentDictionary<Type, OpActivator> _opActivators;

        static Decoder()
        {
            _opActivators = new ConcurrentDictionary<Type, OpActivator>();
        }

        public static Block[] Decode(IGalMemory memory, ulong address)
        {
            Dictionary<ulong, Block> visited    = new Dictionary<ulong, Block>();
            Dictionary<ulong, Block> visitedEnd = new Dictionary<ulong, Block>();

            Queue<Block> blocks = new Queue<Block>();

            Block Enqueue(ulong addr)
            {
                if (!visited.TryGetValue(addr, out Block output))
                {
                    output = new Block(addr);

                    blocks.Enqueue(output);

                    visited.Add(addr, output);
                }

                return output;
            }

            ulong start = address + HeaderSize;

            Block entry = Enqueue(start);

            while (blocks.TryDequeue(out Block current))
            {
                FillBlock(memory, current, start);

                //Set child blocks. "Branch" is the block the branch instruction
                //points to (when taken), "Next" is the block at the next address,
                //executed when the branch is not taken. For Unconditional Branches
                //or end of shader, Next is null.
                if (current.OpCodes.Count > 0)
                {
                    foreach (OpCodeSsy ssyOp in current.SsyOpCodes)
                    {
                        Enqueue(ssyOp.GetAbsoluteAddress());
                    }

                    OpCode lastOp = current.GetLastOp();

                    if (lastOp is OpCodeBranch op)
                    {
                        current.Branch = Enqueue(op.GetAbsoluteAddress());
                    }

                    if (!IsUnconditionalBranch(lastOp))
                    {
                        current.Next = Enqueue(current.EndAddress);
                    }
                }

                //If we have on the graph two blocks with the same end address,
                //then we need to split the bigger block and have two small blocks,
                //the end address of the bigger "Current" block should then be == to
                //the address of the "Smaller" block.
                while (visitedEnd.TryGetValue(current.EndAddress, out Block smaller))
                {
                    if (current.Address > smaller.Address)
                    {
                        Block temp = smaller;

                        smaller = current;
                        current = temp;
                    }

                    current.EndAddress = smaller.Address;
                    current.Next       = smaller;
                    current.Branch     = null;

                    current.OpCodes.RemoveRange(
                        current.OpCodes.Count - smaller.OpCodes.Count,
                        smaller.OpCodes.Count);

                    current.UpdateSsyOpCodes();
                    smaller.UpdateSsyOpCodes();

                    visitedEnd[smaller.EndAddress] = smaller;
                }

                visitedEnd.Add(current.EndAddress, current);
            }

            foreach (Block ssyBlock in visited.Values.Where(x => x.SsyOpCodes.Count != 0))
            {
                for (int ssyIndex = 0; ssyIndex < ssyBlock.SsyOpCodes.Count; ssyIndex++)
                {
                    PropagateSsy(visited, ssyBlock, ssyIndex);
                }
            }

            Block[] cfg = new Block[visited.Count];

            int index = 0;

            foreach (Block block in visited.Values.OrderBy(x => x.Address - entry.Address))
            {
                block.Index = index;

                cfg[index++] = block;
            }

            return cfg;
        }

        private struct PathBlockState
        {
            public Block Block { get; }

            private enum RestoreType
            {
                None,
                PopSsy,
                PushSync
            }

            private RestoreType _restoreType;

            private ulong _restoreValue;

            public bool ReturningFromVisit => _restoreType != RestoreType.None;

            public PathBlockState(Block block)
            {
                Block         = block;
                _restoreType  = RestoreType.None;
                _restoreValue = 0;
            }

            public PathBlockState(int oldSsyStackSize)
            {
                Block         = null;
                _restoreType  = RestoreType.PopSsy;
                _restoreValue = (ulong)oldSsyStackSize;
            }

            public PathBlockState(ulong syncAddress)
            {
                Block         = null;
                _restoreType  = RestoreType.PushSync;
                _restoreValue = syncAddress;
            }

            public void RestoreStackState(Stack<ulong> ssyStack)
            {
                if (_restoreType == RestoreType.PushSync)
                {
                    ssyStack.Push(_restoreValue);
                }
                else if (_restoreType == RestoreType.PopSsy)
                {
                    while (ssyStack.Count > (uint)_restoreValue)
                    {
                        ssyStack.Pop();
                    }
                }
            }
        }

        private static void PropagateSsy(Dictionary<ulong, Block> blocks, Block ssyBlock, int ssyIndex)
        {
            OpCodeSsy ssyOp = ssyBlock.SsyOpCodes[ssyIndex];

            Stack<PathBlockState> pending = new Stack<PathBlockState>();

            HashSet<Block> visited = new HashSet<Block>();

            Stack<ulong> ssyStack = new Stack<ulong>();

            void Push(PathBlockState pbs)
            {
                if (pbs.Block == null || visited.Add(pbs.Block))
                {
                    pending.Push(pbs);
                }
            }

            Push(new PathBlockState(ssyBlock));

            while (pending.TryPop(out PathBlockState pbs))
            {
                if (pbs.ReturningFromVisit)
                {
                    pbs.RestoreStackState(ssyStack);

                    continue;
                }

                Block current = pbs.Block;

                int ssyOpCodesCount = current.SsyOpCodes.Count;

                if (ssyOpCodesCount != 0)
                {
                    Push(new PathBlockState(ssyStack.Count));

                    for (int index = ssyIndex; index < ssyOpCodesCount; index++)
                    {
                        ssyStack.Push(current.SsyOpCodes[index].GetAbsoluteAddress());
                    }
                }

                ssyIndex = 0;

                if (current.Next != null)
                {
                    Push(new PathBlockState(current.Next));
                }

                if (current.Branch != null)
                {
                    Push(new PathBlockState(current.Branch));
                }
                else if (current.GetLastOp() is OpCodeSync op)
                {
                    ulong syncAddress = ssyStack.Pop();

                    if (ssyStack.Count == 0)
                    {
                        ssyStack.Push(syncAddress);

                        op.Targets.Add(ssyOp, op.Targets.Count);

                        ssyOp.Syncs.TryAdd(op, Local());
                    }
                    else
                    {
                        Push(new PathBlockState(syncAddress));
                        Push(new PathBlockState(blocks[syncAddress]));
                    }
                }
            }
        }

        private static void FillBlock(IGalMemory memory, Block block, ulong start)
        {
            ulong address = block.Address;

            do
            {
                //Ignore scheduling instructions, which are written every 32 bytes.
                if (((address - start) & 0x1f) == 0)
                {
                    address += 8;

                    continue;
                }

                uint word0 = (uint)memory.ReadInt32((long)(address + 0));
                uint word1 = (uint)memory.ReadInt32((long)(address + 4));

                ulong opAddress = address;

                address += 8;

                long opCode = word0 | (long)word1 << 32;

                (InstEmitter emitter, Type opCodeType) = OpCodeTable.GetEmitter(opCode);

                if (emitter == null)
                {
                    //TODO: Warning, illegal encoding.
                    continue;
                }

                OpCode op = MakeOpCode(opCodeType, emitter, opAddress, opCode);

                block.OpCodes.Add(op);
            }
            while (!IsBranch(block.GetLastOp()));

            block.EndAddress = address;

            block.UpdateSsyOpCodes();
        }

        private static bool IsUnconditionalBranch(OpCode opCode)
        {
            return IsUnconditional(opCode) && IsBranch(opCode);
        }

        private static bool IsUnconditional(OpCode opCode)
        {
            if (opCode is OpCodeExit op && op.Condition != Condition.Always)
            {
                return false;
            }

            return opCode.Predicate.Index == RegisterConsts.PredicateTrueIndex && !opCode.InvertPredicate;
        }

        private static bool IsBranch(OpCode opCode)
        {
            return (opCode is OpCodeBranch && opCode.Emitter != InstEmit.Ssy) ||
                    opCode is OpCodeSync ||
                    opCode is OpCodeExit;
        }

        private static OpCode MakeOpCode(Type type, InstEmitter emitter, ulong address, long opCode)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            OpActivator createInstance = _opActivators.GetOrAdd(type, CacheOpActivator);

            return (OpCode)createInstance(emitter, address, opCode);
        }

        private static OpActivator CacheOpActivator(Type type)
        {
            Type[] argTypes = new Type[] { typeof(InstEmitter), typeof(ulong), typeof(long) };

            DynamicMethod mthd = new DynamicMethod($"Make{type.Name}", type, argTypes);

            ILGenerator generator = mthd.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Newobj, type.GetConstructor(argTypes));
            generator.Emit(OpCodes.Ret);

            return (OpActivator)mthd.CreateDelegate(typeof(OpActivator));
        }
    }
}