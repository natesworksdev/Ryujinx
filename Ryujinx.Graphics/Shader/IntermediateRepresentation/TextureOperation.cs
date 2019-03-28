namespace Ryujinx.Graphics.Shader.IntermediateRepresentation
{
    class TextureOperation : Operation
    {
        public TextureType Type { get; }

        public int TextureHandle  { get; }
        public int ComponentIndex { get; }

        public TextureOperation(
            Instruction      inst,
            TextureType      type,
            int              textureHandle,
            int              componentIndex,
            Operand          dest,
            params Operand[] sources) : base(inst, dest, sources)
        {
            Type           = type;
            TextureHandle  = textureHandle;
            ComponentIndex = componentIndex;
        }
    }
}