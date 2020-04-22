using ARMeilleure.Instructions;

namespace ARMeilleure.Decoders
{
    readonly struct InstDescriptor
    {
        public static InstDescriptor Undefined => new InstDescriptor(InstName.Und, InstEmit.Und);

        public readonly InstName    Name    { get; }
        public readonly InstEmitter Emitter { get; }

        public InstDescriptor(InstName name, InstEmitter emitter)
        {
            Name    = name;
            Emitter = emitter;
        }
    }
}