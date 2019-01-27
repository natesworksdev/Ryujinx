using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64.Instructions
{
    static class InstEmitFlowHelper
    {
        public static void EmitCall(ILEmitterCtx context, long imm)
        {
            if (!context.TryOptEmitSubroutineCall())
            {
                context.TranslateAhead(imm);

                context.EmitLdarg(TranslatedSub.StateArgIdx);

                context.EmitFieldLoad(typeof(CpuThreadState).GetField(nameof(CpuThreadState.CurrentTranslator),
                    BindingFlags.Instance |
                    BindingFlags.NonPublic));

                context.EmitLdarg(TranslatedSub.StateArgIdx);
                context.EmitLdarg(TranslatedSub.MemoryArgIdx);

                context.EmitLdc_I8(imm);

                context.EmitPrivateCall(typeof(Translator), nameof(Translator.GetOrTranslateSubroutine));

                context.EmitLdarg(TranslatedSub.StateArgIdx);
                context.EmitLdarg(TranslatedSub.MemoryArgIdx);

                context.EmitCall(typeof(TranslatedSub), nameof(TranslatedSub.Execute));
            }

            EmitContinueOrReturnCheck(context);
        }

        public static void EmitCallVirtual(ILEmitterCtx context)
        {
            context.EmitSttmp();
            context.EmitLdarg(TranslatedSub.StateArgIdx);

            context.EmitFieldLoad(typeof(CpuThreadState).GetField(nameof(CpuThreadState.CurrentTranslator),
                BindingFlags.Instance |
                BindingFlags.NonPublic));

            context.EmitLdarg(TranslatedSub.StateArgIdx);
            context.EmitLdarg(TranslatedSub.MemoryArgIdx);
            context.EmitLdtmp();

            context.EmitPrivateCall(typeof(Translator), nameof(Translator.GetOrTranslateVirtualSubroutine));

            context.EmitLdarg(TranslatedSub.StateArgIdx);
            context.EmitLdarg(TranslatedSub.MemoryArgIdx);

            context.EmitCall(typeof(TranslatedSub), nameof(TranslatedSub.Execute));

            EmitContinueOrReturnCheck(context);
        }

        private static void EmitContinueOrReturnCheck(ILEmitterCtx context)
        {
            //Note: The return value of the called method will be placed
            //at the Stack, the return value is always a Int64 with the
            //return address of the function. We check if the address is
            //correct, if it isn't we keep returning until we reach the dispatcher.
            if (context.CurrBlock.Next != null)
            {
                context.Emit(OpCodes.Dup);

                context.EmitLdc_I8(context.CurrOp.Position + 4);

                ILLabel lblContinue = new ILLabel();

                context.Emit(OpCodes.Beq_S, lblContinue);
                context.Emit(OpCodes.Ret);

                context.MarkLabel(lblContinue);

                context.Emit(OpCodes.Pop);

                context.EmitLoadState();
            }
            else
            {
                context.Emit(OpCodes.Ret);
            }
        }
    }
}
