using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void Vote(EmitterContext context)
        {
            OpCodeVote op = (OpCodeVote)context.CurrOp;

            Operand pred = GetPredicate39(context);

            Operand res;

            if (context.Config.GpuAccessor.QueryShaderMaxThreads32())
            {
                res = op.VoteOp switch
                {
                    VoteOp.All => context.VoteAll(pred),
                    VoteOp.Any => context.VoteAny(pred),
                    VoteOp.AllEqual => context.VoteAllEqual(pred),
                    _ => null,
                };
            }
            else
            {
                // Emulate vote with ballot masks.
                // We do that when the GPU thread count is not 32,
                // since the shader code assumes it is 32.
                // allInvocations => ballot(pred) == ballot(true),
                // anyInvocation => ballot(pred) != 0,
                // allInvocationsEqual => ballot(pred) == balot(true) || ballot(pred) == 0
                Operand ballotMask = context.Ballot(pred);

                Operand AllTrue() => context.ICompareEqual(ballotMask, context.Ballot(Const(IrConsts.True)));

                res = op.VoteOp switch
                {
                    VoteOp.All => AllTrue(),
                    VoteOp.Any => context.ICompareNotEqual(ballotMask, Const(0)),
                    VoteOp.AllEqual => context.BitwiseOr(AllTrue(), context.ICompareEqual(ballotMask, Const(0))),
                    _ => null,
                };
            }

            if (res != null)
            {
                context.Copy(Register(op.Predicate45), res);
            }
            else
            {
                context.Config.GpuAccessor.Log($"Invalid vote operation: {op.VoteOp}.");
            }

            if (!op.Rd.IsRZ)
            {
                context.Copy(Register(op.Rd), context.Ballot(pred));
            }
        }
    }
}