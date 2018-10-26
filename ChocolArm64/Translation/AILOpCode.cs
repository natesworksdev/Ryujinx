using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    internal struct AILOpCode : IAilEmit
    {
        private OpCode _ilOp;

        public AILOpCode(OpCode ilOp)
        {
            _ilOp = ilOp;
        }

        public void Emit(AILEmitter context)
        {
            context.Generator.Emit(_ilOp);
        }
    }
}