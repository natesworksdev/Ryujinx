using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Shader.CodeGen.Glsl;
using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.Instructions;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation
{
    public static class Translator
    {
        public static string Translate(IGalMemory memory, ulong address, GalShaderType shaderType)
        {
            ShaderHeader header = new ShaderHeader(memory, address);

            Block[] cfg = Decoder.Decode(memory, address);

            EmitterContext context = new EmitterContext(shaderType, header);

            for (int blkIndex = 0; blkIndex < cfg.Length; blkIndex++)
            {
                Block block = cfg[blkIndex];

                context.CurrBlock = block;

                context.MarkLabel(context.GetLabel(block.Address));

                for (int opIndex = 0; opIndex < block.OpCodes.Count; opIndex++)
                {
                    OpCode op = block.OpCodes[opIndex];

                    if (op.NeverExecute)
                    {
                        continue;
                    }

                    Operand predSkipLbl = null;

                    bool skipPredicateCheck = op.Emitter == InstEmit.Bra;

                    if (op is OpCodeSync opSync)
                    {
                        //If the instruction is a SYNC instruction with only one
                        //possible target address, then the instruction is basically
                        //just a simple branch, we can generate code similar to branch
                        //instructions, with the condition check on the branch itself.
                        skipPredicateCheck |= opSync.Targets.Count < 2;
                    }

                    if (!(op.Predicate.IsPT || skipPredicateCheck))
                    {
                        Operand label;

                        if (opIndex == block.OpCodes.Count - 1 && block.Next != null)
                        {
                            label = context.GetLabel(block.Next.Address);
                        }
                        else
                        {
                            label = Label();

                            predSkipLbl = label;
                        }

                        Operand pred = Register(op.Predicate);

                        if (op.InvertPredicate)
                        {
                            context.BranchIfTrue(label, pred);
                        }
                        else
                        {
                            context.BranchIfFalse(label, pred);
                        }
                    }

                    context.CurrOp = op;

                    op.Emitter(context);

                    if (predSkipLbl != null)
                    {
                        context.MarkLabel(predSkipLbl);
                    }
                }
            }

            BasicBlock[] irBlocks = ControlFlowGraph.MakeCfg(context.GetOperations());

            Dominance.FindDominators(irBlocks[0], irBlocks.Length);

            Dominance.FindDominanceFrontiers(irBlocks);

            Ssa.Rename(irBlocks);

            Optimizer.Optimize(irBlocks);

            StructuredProgramInfo prgInfo = StructuredProgram.MakeStructuredProgram(irBlocks);

            GlslGenerator generator = new GlslGenerator();

            string glslProgram = generator.Generate(prgInfo, shaderType);

            System.Console.WriteLine(glslProgram);

            return glslProgram;
        }
    }
}