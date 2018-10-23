using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Brk(AilEmitterCtx context)
        {
            EmitExceptionCall(context, nameof(AThreadState.OnBreak));
        }

        public static void Svc(AilEmitterCtx context)
        {
            EmitExceptionCall(context, nameof(AThreadState.OnSvcCall));
        }

        private static void EmitExceptionCall(AilEmitterCtx context, string mthdName)
        {
            AOpCodeException op = (AOpCodeException)context.CurrOp;

            context.EmitStoreState();

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            context.EmitLdc_I8(op.Position);
            context.EmitLdc_I4(op.Id);

            context.EmitPrivateCall(typeof(AThreadState), mthdName);

            //Check if the thread should still be running, if it isn't then we return 0
            //to force a return to the dispatcher and then exit the thread.
            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            context.EmitCallPropGet(typeof(AThreadState), nameof(AThreadState.Running));

            AilLabel lblEnd = new AilLabel();

            context.Emit(OpCodes.Brtrue_S, lblEnd);

            context.EmitLdc_I8(0);

            context.Emit(OpCodes.Ret);

            context.MarkLabel(lblEnd);

            if (context.CurrBlock.Next != null)
            {
                context.EmitLoadState(context.CurrBlock.Next);
            }
            else
            {
                context.EmitLdc_I8(op.Position + 4);

                context.Emit(OpCodes.Ret);
            }
        }

        public static void Und(AilEmitterCtx context)
        {
            AOpCode op = context.CurrOp;

            context.EmitStoreState();

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            context.EmitLdc_I8(op.Position);
            context.EmitLdc_I4(op.RawOpCode);

            context.EmitPrivateCall(typeof(AThreadState), nameof(AThreadState.OnUndefined));

            if (context.CurrBlock.Next != null)
            {
                context.EmitLoadState(context.CurrBlock.Next);
            }
            else
            {
                context.EmitLdc_I8(op.Position + 4);

                context.Emit(OpCodes.Ret);
            }
        }
    }
}