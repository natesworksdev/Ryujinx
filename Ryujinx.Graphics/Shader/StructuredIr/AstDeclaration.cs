namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstDeclaration : AstNode
    {
        public AstOperand Operand { get; }

        public AstDeclaration(AstOperand operand)
        {
            Operand = operand;
        }
    }
}