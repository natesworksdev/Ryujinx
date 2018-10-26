using ChocolArm64.State;
using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    internal struct AILOpCodeLoad : IAilEmit
    {
        public int Index { get; private set; }

        public AIoType IoType { get; private set; }

        public ARegisterSize RegisterSize { get; private set; }

        public AILOpCodeLoad(int index, AIoType ioType, ARegisterSize registerSize = 0)
        {
            this.Index        = index;
            this.IoType       = ioType;
            this.RegisterSize = registerSize;
        }

        public void Emit(AILEmitter context)
        {
            switch (IoType)
            {
                case AIoType.Arg: context.Generator.EmitLdarg(Index); break;

                case AIoType.Fields:
                {
                    long intInputs = context.LocalAlloc.GetIntInputs(context.GetIlBlock(Index));
                    long vecInputs = context.LocalAlloc.GetVecInputs(context.GetIlBlock(Index));

                    LoadLocals(context, intInputs, ARegisterType.Int);
                    LoadLocals(context, vecInputs, ARegisterType.Vector);

                    break;
                }

                case AIoType.Flag:   EmitLdloc(context, Index, ARegisterType.Flag);   break;
                case AIoType.Int:    EmitLdloc(context, Index, ARegisterType.Int);    break;
                case AIoType.Vector: EmitLdloc(context, Index, ARegisterType.Vector); break;
            }
        }

        private void LoadLocals(AILEmitter context, long inputs, ARegisterType baseType)
        {
            for (int bit = 0; bit < 64; bit++)
            {
                long mask = 1L << bit;

                if ((inputs & mask) != 0)
                {
                    ARegister reg = AILEmitter.GetRegFromBit(bit, baseType);

                    context.Generator.EmitLdarg(ATranslatedSub.StateArgIdx);
                    context.Generator.Emit(OpCodes.Ldfld, reg.GetField());

                    context.Generator.EmitStloc(context.GetLocalIndex(reg));
                }
            }
        }

        private void EmitLdloc(AILEmitter context, int index, ARegisterType registerType)
        {
            ARegister reg = new ARegister(index, registerType);

            context.Generator.EmitLdloc(context.GetLocalIndex(reg));

            if (registerType == ARegisterType.Int &&
                RegisterSize == ARegisterSize.Int32)
                context.Generator.Emit(OpCodes.Conv_U4);
        }
    }
}