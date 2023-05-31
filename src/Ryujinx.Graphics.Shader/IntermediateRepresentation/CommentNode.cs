namespace Ryujinx.Graphics.Shader.IntermediateRepresentation
{
    sealed class CommentNode : Operation
    {
        public string Comment { get; }

        public CommentNode(string comment) : base(Instruction.Comment, null)
        {
            Comment = comment;
        }
    }
}