using System;

namespace ChocolArm64.Instruction
{
    struct AInst
    {
        public AInstInterpreter Interpreter { get; private set; }
        public AInstEmitter     Emitter     { get; private set; }
        public Type             Type        { get; private set; }

        public static AInst Undefined => new AInst(null, AInstEmit.Und, null);

        public AInst(AInstInterpreter interpreter, AInstEmitter emitter, Type type)
        {
            Interpreter = interpreter;
            Emitter     = emitter;
            Type        = type;
        }
    }
}