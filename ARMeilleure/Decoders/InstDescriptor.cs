using ARMeilleure.Instructions;

namespace ARMeilleure.Decoders
{
    readonly struct InstDescriptor
    {
        private static InstDescriptor s_Invalid = new InstDescriptor(InstName.Und, InstEmit.Und);

        public static ref readonly InstDescriptor Undefined => ref s_Invalid;

        public InstName    Name    { get; }
        public InstEmitter Emitter { get; }

        public InstDescriptor(InstName name, InstEmitter emitter)
        {
            Name    = name;
            Emitter = emitter;
        }
    }
}