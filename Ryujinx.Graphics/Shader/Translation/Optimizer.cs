using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class Optimizer
    {
        public static void Optimize(BasicBlock[] blocks)
        {
            bool modified;

            do
            {
                modified = false;

                for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
                {
                    BasicBlock block = blocks[blkIndex];

                    LinkedListNode<INode> node = block.Operations.First;

                    while (node != null)
                    {
                        LinkedListNode<INode> nextNode = node.Next;

                        bool isUnused = IsUnused(node.Value);

                        if (!(node.Value is Operation operation) || isUnused)
                        {
                            if (isUnused)
                            {
                                RemoveNode(block, node);

                                modified = true;
                            }

                            node = nextNode;

                            continue;
                        }

                        if (operation.Inst == Instruction.Copy && DestIsLocalVar(operation))
                        {
                            PropagateCopy(operation);

                            RemoveNode(block, node);

                            modified = true;
                        }

                        node = nextNode;
                    }
                }
            }
            while (modified);
        }

        private static void PropagateCopy(Operation copyOp)
        {
            //Propagate copy source operand to all uses of
            //the destination operand.
            Operand dest = copyOp.Dest;
            Operand src  = copyOp.GetSource(0);

            foreach (INode useNode in dest.UseOps)
            {
                for (int index = 0; index < useNode.SourcesCount; index++)
                {
                    if (useNode.GetSource(index) == dest)
                    {
                        useNode.SetSource(index, src);
                    }
                }
            }
        }

        private static void RemoveNode(BasicBlock block, LinkedListNode<INode> llNode)
        {
            //Remove a node from the nodes list, and also remove itself
            //from all the use lists on the operands that this node uses.
            block.Operations.Remove(llNode);

            Queue<INode> nodes = new Queue<INode>();

            nodes.Enqueue(llNode.Value);

            while (nodes.TryDequeue(out INode node))
            {
                for (int index = 0; index < node.SourcesCount; index++)
                {
                    Operand src = node.GetSource(index);

                    if (src.Type != OperandType.LocalVariable)
                    {
                        continue;
                    }

                    if (src.UseOps.Remove(node) && src.UseOps.Count == 0)
                    {
                        nodes.Enqueue(src.AsgOp);
                    }
                }
            }
        }

        private static bool IsUnused(INode node)
        {
            return DestIsLocalVar(node) && node.Dest.UseOps.Count == 0;
        }

        private static bool DestIsLocalVar(INode node)
        {
            return node.Dest != null && node.Dest.Type == OperandType.LocalVariable;
        }
    }
}