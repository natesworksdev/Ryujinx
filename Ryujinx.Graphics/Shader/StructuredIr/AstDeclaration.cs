namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstDeclaration : IAstNode
    {
        public AstOperand Operand { get; }

        public AstDeclaration(AstOperand operand)
        {
            Operand = operand;
        }
    }
}