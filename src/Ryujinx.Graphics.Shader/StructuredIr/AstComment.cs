namespace Ryujinx.Graphics.Shader.StructuredIr
{
    sealed class AstComment : AstNode
    {
        public string Comment { get; }

        public AstComment(string comment)
        {
            Comment = comment;
        }
    }
}