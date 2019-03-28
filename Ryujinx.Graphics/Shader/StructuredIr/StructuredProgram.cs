using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    static class StructuredProgram
    {
        public static StructuredProgramInfo MakeStructuredProgram(BasicBlock[] blocks)
        {
            RemovePhis(blocks);

            StructuredProgramContext context = new StructuredProgramContext(blocks);

            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                BasicBlock block = blocks[blkIndex];

                context.EnterBlock(block);

                foreach (INode node in block.Operations)
                {
                    if (node is Operation operation)
                    {
                        Instruction inst = operation.Inst;

                        if (operation.Dest != null                     &&
                            operation.Inst != Instruction.MarkLabel    &&
                            operation.Inst != Instruction.Branch       &&
                            operation.Inst != Instruction.BranchIfTrue &&
                            operation.Inst != Instruction.BranchIfFalse)
                        {
                            AstOperand dest = context.GetOperandDef(operation.Dest);

                            IAstNode[] sources = new IAstNode[operation.SourcesCount];

                            for (int index = 0; index < sources.Length; index++)
                            {
                                sources[index] = context.GetOperandUse(operation.GetSource(index));
                            }

                            if (inst == Instruction.LoadConstant)
                            {
                                context.Info.ConstantBuffers.Add((sources[0] as AstOperand).Value);
                            }

                            AstAssignment astAsg;

                            if (inst == Instruction.Copy)
                            {
                                //Copies are pretty much a typeless operation,
                                //so it's better to get the type from the source
                                //operand used on the copy, to avoid unnecessary
                                //reinterpret casts on the generated code.
                                dest.VarType = GetVarTypeFromUses(operation.Dest);

                                astAsg = new AstAssignment(dest, sources[0]);
                            }
                            else
                            {
                                dest.VarType = InstructionInfo.GetDestVarType(inst);

                                AstOperation astOperation;

                                if (operation is TextureOperation texOp)
                                {
                                    if (!context.Info.Samplers.TryAdd(texOp.TextureHandle, texOp.Type))
                                    {
                                        //TODO: Warning.
                                    }

                                    int[] components = new int[] { texOp.ComponentIndex };

                                    astOperation = new AstTextureOperation(
                                        inst,
                                        texOp.Type,
                                        texOp.TextureHandle,
                                        components,
                                        sources);
                                }
                                else
                                {
                                    astOperation = new AstOperation(inst, sources);
                                }

                                astAsg = new AstAssignment(dest, astOperation);
                            }

                            context.AddNode(astAsg);
                        }
                        else
                        {
                            //If dest is null, it's assumed that all the source
                            //operands are also null.
                            AstOperation astOperation = new AstOperation(inst);

                            context.AddNode(astOperation);
                        }
                    }
                }

                context.LeaveBlock(block);
            }

            context.PrependLocalDeclarations();

            return context.Info;
        }

        private static void RemovePhis(BasicBlock[] blocks)
        {
            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                BasicBlock block = blocks[blkIndex];

                LinkedListNode<INode> node = block.Operations.First;

                while (node != null)
                {
                    LinkedListNode<INode> nextNode = node.Next;

                    if (!(node.Value is PhiNode phi))
                    {
                        node = nextNode;

                        continue;
                    }

                    for (int index = 0; index < phi.SourcesCount; index++)
                    {
                        Operand src = phi.GetSource(index);

                        BasicBlock srcBlock = phi.GetBlock(index);

                        Operation copyOp = new Operation(Instruction.Copy, phi.Dest, src);

                        AddBeforeBranch(srcBlock, copyOp);
                    }

                    block.Operations.Remove(node);

                    node = nextNode;
                }
            }
        }

        private static void AddBeforeBranch(BasicBlock block, INode node)
        {
            INode lastOp = block.GetLastOp();

            if (lastOp is Operation operation && IsControlFlowInst(operation.Inst))
            {
                block.Operations.AddBefore(block.Operations.Last, node);
            }
            else
            {
                block.Operations.AddLast(node);
            }
        }

        private static bool IsControlFlowInst(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Branch:
                case Instruction.BranchIfFalse:
                case Instruction.BranchIfTrue:
                case Instruction.Discard:
                case Instruction.Return:
                    return true;
            }

            return false;
        }

        private static VariableType GetVarTypeFromUses(Operand dest)
        {
            foreach (INode useNode in dest.UseOps)
            {
                if (useNode is Operation operation)
                {
                    if (operation.Inst == Instruction.Copy)
                    {
                        if (operation.Dest.Type == OperandType.LocalVariable)
                        {
                            //TODO: Search through copies.
                            return VariableType.S32;
                        }

                        return OperandInfo.GetVarType(operation.Dest.Type);
                    }

                    for (int index = 0; index < operation.SourcesCount; index++)
                    {
                        if (operation.GetSource(index) == dest)
                        {
                            return InstructionInfo.GetSrcVarType(operation.Inst, index);
                        }
                    }
                }
            }

            return VariableType.S32;
        }
    }
}