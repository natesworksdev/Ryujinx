namespace Ryujinx.Graphics.Shader.IntermediateRepresentation
{
    class TextureOperation : Operation
    {
        public TextureType  Type  { get; }
        public TextureFlags Flags { get; }

        public int TextureHandle  { get; }
        public int ComponentIndex { get; }

        public TextureOperation(
            Instruction      inst,
            TextureType      type,
            TextureFlags     flags,
            int              textureHandle,
            int              componentIndex,
            Operand          dest,
            params Operand[] sources) : base(inst, dest, sources)
        {
            Type           = type;
            Flags          = flags;
            TextureHandle  = textureHandle;
            ComponentIndex = componentIndex;
        }
    }
}