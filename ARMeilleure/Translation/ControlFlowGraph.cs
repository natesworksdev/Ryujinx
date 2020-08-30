using ARMeilleure.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ARMeilleure.Translation
{
    class ControlFlowGraph
    {
        public BasicBlock Entry { get; }
        public IntrusiveList<BasicBlock> Blocks { get; }
        public BasicBlock[] PostOrderBlocks { get; private set; }
        public int[] PostOrderMap { get; private set; }

        public ControlFlowGraph(BasicBlock entry, IntrusiveList<BasicBlock> blocks)
        {
            Entry = entry;
            Blocks = blocks;

            Update(removeUnreachableBlocks: true);
        }

        public void Update(bool removeUnreachableBlocks)
        {
            if (removeUnreachableBlocks)
            {
                RemoveUnreachableBlocks(Blocks);
            }

            var visited = new HashSet<BasicBlock>();
            var blockStack = new Stack<BasicBlock>();

            static T[] Initialize<T>(T[] array, int length)
            {
                if (array == null || array.Length != length)
                {
                    array = new T[length];
                }
                
                // No need to clear the array because all its elements will be written to anyways.
                return array;
            }

            PostOrderBlocks = Initialize(PostOrderBlocks, Blocks.Count);
            PostOrderMap = Initialize(PostOrderMap, Blocks.Count);

            visited.Add(Entry);
            blockStack.Push(Entry);

            int index = 0;

            while (blockStack.TryPop(out BasicBlock block))
            {
                bool visitedNew = false;

                for (int i = 0; i < block.SuccessorCount; i++)
                {
                    BasicBlock succ = block.GetSuccessor(i);

                    if (visited.Add(succ))
                    {
                        blockStack.Push(block);
                        blockStack.Push(succ);

                        visitedNew = true;

                        break;
                    }
                }

                if (!visitedNew)
                {
                    PostOrderMap[block.Index] = index;

                    PostOrderBlocks[index++] = block;
                }
            }
        }

        private void RemoveUnreachableBlocks(IntrusiveList<BasicBlock> blocks)
        {
            var visited = new HashSet<BasicBlock>();
            var workQueue = new Queue<BasicBlock>();

            visited.Add(Entry);
            workQueue.Enqueue(Entry);

            while (workQueue.TryDequeue(out BasicBlock block))
            {
                Debug.Assert(block.Index != -1, "Invalid block index.");

                for (int i = 0; i < block.SuccessorCount; i++)
                {
                    BasicBlock succ = block.GetSuccessor(i);

                    if (visited.Add(succ))
                    {
                        workQueue.Enqueue(succ);
                    }
                }
            }

            if (visited.Count < blocks.Count)
            {
                // Remove unreachable blocks and renumber.
                int index = 0;

                for (BasicBlock block = blocks.First; block != null;)
                {
                    BasicBlock nextBlock = block.ListNext;

                    if (!visited.Contains(block))
                    {
                        while (block.SuccessorCount > 0)
                        {
                            block.RemoveSuccessor(index: block.SuccessorCount - 1);
                        }

                        blocks.Remove(block);
                    }
                    else
                    {
                        block.Index = index++;
                    }

                    block = nextBlock;
                }
            }
        }

        public BasicBlock SplitEdge(BasicBlock predecessor, BasicBlock successor)
        {
            BasicBlock splitBlock = new BasicBlock(Blocks.Count);

            for (int i = 0; i < predecessor.SuccessorCount; i++)
            {
                if (predecessor.GetSuccessor(i) == successor)
                {
                    predecessor.SetSuccessor(i, splitBlock);
                }
            }

            if (splitBlock.Predecessors.Count == 0)
            {
                throw new ArgumentException("Predecessor and successor are not connected.");
            }

            splitBlock.AddSuccessor(successor);

            Blocks.AddBefore(successor, splitBlock);

            return splitBlock;
        }
    }
}