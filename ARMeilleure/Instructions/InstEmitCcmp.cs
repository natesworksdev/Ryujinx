using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitAluHelper;
using static ARMeilleure.Instructions.InstEmitFlowHelper;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Ccmn(EmitterContext context) => EmitCcmp(context, isNegated: true);
        public static void Ccmp(EmitterContext context) => EmitCcmp(context, isNegated: false);

        private static void EmitCcmp(EmitterContext context, bool isNegated)
        {
            OpCodeCcmp op = (OpCodeCcmp)context.CurrOp;

            Operand lblTrue = Label();
            Operand lblEnd  = Label();

            EmitCondBranch(context, lblTrue, op.Cond);

            context.Copy(GetFlag(PState.VFlag), Const((op.Nzcv >> 0) & 1));
            context.Copy(GetFlag(PState.CFlag), Const((op.Nzcv >> 1) & 1));
            context.Copy(GetFlag(PState.ZFlag), Const((op.Nzcv >> 2) & 1));
            context.Copy(GetFlag(PState.NFlag), Const((op.Nzcv >> 3) & 1));

            context.Branch(lblEnd);

            context.MarkLabel(lblTrue);

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            if (isNegated)
            {
                Operand d = context.Add(n, m);

                EmitNZFlagsCheck(context, d);

                EmitAddsCCheck(context, n, d);
                EmitAddsVCheck(context, n, m, d);
            }
            else
            {
                Operand d = context.Subtract(n, m);

                EmitNZFlagsCheck(context, d);

                EmitSubsCCheck(context, n, m);
                EmitSubsVCheck(context, n, m, d);
            }

            context.MarkLabel(lblEnd);
        }
    }
}