using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    struct AilOpCodeBranch : IAilEmit
    {
        private OpCode   _ilOp;
        private AilLabel _label;

        public AilOpCodeBranch(OpCode ilOp, AilLabel label)
        {
            this._ilOp  = ilOp;
            this._label = label;
        }

        public void Emit(AilEmitter context)
        {
            context.Generator.Emit(_ilOp, _label.GetLabel(context));
        }
    }
}