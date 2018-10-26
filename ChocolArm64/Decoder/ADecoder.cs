using ChocolArm64.Instruction;
using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ChocolArm64.Decoder
{
    internal static class ADecoder
    {
        private delegate object OpActivator(AInst inst, long position, int opCode);

        private static ConcurrentDictionary<Type, OpActivator> _opActivators;

        static ADecoder()
        {
            _opActivators = new ConcurrentDictionary<Type, OpActivator>();
        }

        public static ABlock DecodeBasicBlock(AThreadState state, AMemory memory, long start)
        {
            ABlock block = new ABlock(start);

            FillBlock(state, memory, block);

            return block;
        }

        public static (ABlock[] Graph, ABlock Root) DecodeSubroutine(
            ATranslatorCache cache,
            AThreadState     state,
            AMemory          memory,
            long             start)
        {
            Dictionary<long, ABlock> visited    = new Dictionary<long, ABlock>();
            Dictionary<long, ABlock> visitedEnd = new Dictionary<long, ABlock>();

            Queue<ABlock> blocks = new Queue<ABlock>();

            ABlock Enqueue(long position)
            {
                if (!visited.TryGetValue(position, out ABlock output))
                {
                    output = new ABlock(position);

                    blocks.Enqueue(output);

                    visited.Add(position, output);
                }

                return output;
            }

            ABlock root = Enqueue(start);

            while (blocks.Count > 0)
            {
                ABlock current = blocks.Dequeue();

                FillBlock(state, memory, current);

                //Set child blocks. "Branch" is the block the branch instruction
                //points to (when taken), "Next" is the block at the next address,
                //executed when the branch is not taken. For Unconditional Branches
                //(except BL/BLR that are sub calls) or end of executable, Next is null.
                if (current.OpCodes.Count > 0)
                {
                    bool hasCachedSub = false;

                    AOpCode lastOp = current.GetLastOp();

                    if (lastOp is AOpCodeBImm op)
                    {
                        if (op.Emitter == AInstEmit.Bl)
                            hasCachedSub = cache.HasSubroutine(op.Imm);
                        else
                            current.Branch = Enqueue(op.Imm);
                    }

                    if (!(lastOp is AOpCodeBImmAl ||
                          lastOp is AOpCodeBReg) || hasCachedSub)
                        current.Next = Enqueue(current.EndPosition);
                }

                //If we have on the graph two blocks with the same end position,
                //then we need to split the bigger block and have two small blocks,
                //the end position of the bigger "Current" block should then be == to
                //the position of the "Smaller" block.
                while (visitedEnd.TryGetValue(current.EndPosition, out ABlock smaller))
                {
                    if (current.Position > smaller.Position)
                    {
                        ABlock temp = smaller;

                        smaller = current;
                        current = temp;
                    }

                    current.EndPosition = smaller.Position;
                    current.Next        = smaller;
                    current.Branch      = null;

                    current.OpCodes.RemoveRange(
                        current.OpCodes.Count - smaller.OpCodes.Count,
                        smaller.OpCodes.Count);

                    visitedEnd[smaller.EndPosition] = smaller;
                }

                visitedEnd.Add(current.EndPosition, current);
            }

            //Make and sort Graph blocks array by position.
            ABlock[] graph = new ABlock[visited.Count];

            while (visited.Count > 0)
            {
                ulong firstPos = ulong.MaxValue;

                foreach (ABlock block in visited.Values)
                    if (firstPos > (ulong)block.Position)
                        firstPos = (ulong)block.Position;

                ABlock current = visited[(long)firstPos];

                do
                {
                    graph[graph.Length - visited.Count] = current;

                    visited.Remove(current.Position);

                    current = current.Next;
                }
                while (current != null);
            }

            return (graph, root);
        }

        private static void FillBlock(AThreadState state, AMemory memory, ABlock block)
        {
            long position = block.Position;

            AOpCode opCode;

            do
            {
                //TODO: This needs to be changed to support both AArch32 and AArch64,
                //once JIT support is introduced on AArch32 aswell.
                opCode = DecodeOpCode(state, memory, position);

                block.OpCodes.Add(opCode);

                position += 4;
            }
            while (!(IsBranch(opCode) || IsException(opCode)));

            block.EndPosition = position;
        }

        private static bool IsBranch(AOpCode opCode)
        {
            return opCode is AOpCodeBImm ||
                   opCode is AOpCodeBReg;
        }

        private static bool IsException(AOpCode opCode)
        {
            return opCode.Emitter == AInstEmit.Brk ||
                   opCode.Emitter == AInstEmit.Svc ||
                   opCode.Emitter == AInstEmit.Und;
        }

        public static AOpCode DecodeOpCode(AThreadState state, AMemory memory, long position)
        {
            int opCode = memory.ReadInt32(position);

            AInst inst;

            if (state.ExecutionMode == AExecutionMode.AArch64)
                inst = AOpCodeTable.GetInstA64(opCode);
            else
                inst = AOpCodeTable.GetInstA32(opCode);

            AOpCode decodedOpCode = new AOpCode(AInst.Undefined, position, opCode);

            if (inst.Type != null) decodedOpCode = MakeOpCode(inst.Type, inst, position, opCode);

            return decodedOpCode;
        }

        private static AOpCode MakeOpCode(Type type, AInst inst, long position, int opCode)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            OpActivator createInstance = _opActivators.GetOrAdd(type, CacheOpActivator);

            return (AOpCode)createInstance(inst, position, opCode);
        }

        private static OpActivator CacheOpActivator(Type type)
        {
            Type[] argTypes = new Type[] { typeof(AInst), typeof(long), typeof(int) };

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