using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class BindlessAnalysis
    {
        private const int ExpectedTextureBufferIndex = 2;

        public static void RunPass(BasicBlock[] blocks, ShaderConfig config)
        {
            BindlessTextureFlags flags = BindlessTextureFlags.None;

            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                BasicBlock block = blocks[blkIndex];

                for (LinkedListNode<INode> node = block.Operations.First; node != null; node = node.Next)
                {
                    if (!(node.Value is TextureOperation texOp))
                    {
                        continue;
                    }

                    if ((texOp.Flags & TextureFlags.Bindless) == 0)
                    {
                        continue;
                    }

                    if (IsIndexedAccess(texOp))
                    {
                        flags |= BindlessTextureFlags.BindlessNvn;
                        continue;
                    }

                    flags |= BindlessTextureFlags.BindlessFull;
                }
            }

            config.BindlessTextureFlags = flags;
        }

        private static bool IsIndexedAccess(TextureOperation texOp)
        {
            // Try to detect a indexed access.
            // The access is considered indexed if the handle is loaded with a LDC instruction
            // from the driver reserved constant buffer used for texture handles.
            if (!(texOp.GetSource(0).AsgOp is Operation handleAsgOp))
            {
                return false;
            }

            if (handleAsgOp.Inst != Instruction.LoadConstant)
            {
                return false;
            }

            Operand ldcSrc0 = handleAsgOp.GetSource(0);

            return ldcSrc0.Type == OperandType.Constant && ldcSrc0.Value == ExpectedTextureBufferIndex;
        }
    }
}