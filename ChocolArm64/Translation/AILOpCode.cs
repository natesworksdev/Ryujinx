using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    struct AilOpCode : IAilEmit
    {
        private OpCode _ilOp;

        public AilOpCode(OpCode ilOp)
        {
            _ilOp = ilOp;
        }

        public void Emit(AilEmitter context)
        {
            context.Generator.Emit(_ilOp);
        }
    }
}