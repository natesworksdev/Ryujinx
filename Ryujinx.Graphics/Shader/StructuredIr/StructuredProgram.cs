using Ryujinx.Graphics.Shader.IntermediateRepresentation;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    static class StructuredProgram
    {
        public static StructuredProgramInfo MakeStructuredProgram(BasicBlock[] blocks)
        {
            PhiFunctions.Remove(blocks);

            StructuredProgramContext context = new StructuredProgramContext(blocks);

            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                BasicBlock block = blocks[blkIndex];

                context.EnterBlock(block);

                foreach (INode node in block.Operations)
                {
                    AddOperation(context, (Operation)node);
                }

                context.LeaveBlock(block);
            }

            context.PrependLocalDeclarations();

            return context.Info;
        }

        private static void AddOperation(StructuredProgramContext context, Operation operation)
        {
            Instruction inst = operation.Inst;

            if (operation.Dest != null && !IsBranchInst(inst))
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
                            texOp.Flags,
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

        private static bool IsBranchInst(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Branch:
                case Instruction.BranchIfFalse:
                case Instruction.BranchIfTrue:
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