using ARMeilleure.Decoders;
using ARMeilleure.Translation;
using System.Reflection;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Brk(EmitterContext context)
        {
            EmitExceptionCall(context, nameof(NativeInterface.Break));
        }

        public static void Svc(EmitterContext context)
        {
            EmitExceptionCall(context, nameof(NativeInterface.SupervisorCall));
        }

        private static void EmitExceptionCall(EmitterContext context, string mthdName)
        {
            OpCodeException op = (OpCodeException)context.CurrOp;

            MethodInfo info = typeof(NativeInterface).GetMethod(mthdName);

            context.StoreToContext();

            context.Call(info, Const(op.Address), Const(op.Id));

            context.LoadFromContext();

            if (context.CurrBlock.Next == null)
            {
                context.Return(Const(op.Address + 4));
            }
        }

        public static void Und(EmitterContext context)
        {
            OpCode op = context.CurrOp;

            MethodInfo info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.Undefined));

            context.StoreToContext();

            context.Call(info, Const(op.Address), Const(op.RawOpCode));

            context.LoadFromContext();

            if (context.CurrBlock.Next == null)
            {
                context.Return(Const(op.Address + 4));
            }
        }
    }
}