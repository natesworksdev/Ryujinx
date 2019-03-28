namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstAssignment : IAstNode
    {
        public IAstNode Destination { get; }
        public IAstNode Source      { get; }

        public AstAssignment(IAstNode destination, IAstNode source)
        {
            Destination = destination;
            Source      = source;
        }
    }
}