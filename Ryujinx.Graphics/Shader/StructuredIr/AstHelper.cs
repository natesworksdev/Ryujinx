using Ryujinx.Graphics.Shader.IntermediateRepresentation;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    static class AstHelper
    {
        public static AstAssignment Assign(IAstNode destination, IAstNode source)
        {
            return new AstAssignment(destination, source);
        }

        public static AstOperand Const(int value)
        {
            return new AstOperand(OperandType.Constant, value);
        }

        public static AstOperand Local(VariableType type)
        {
            AstOperand local = new AstOperand(OperandType.LocalVariable);

            local.VarType = type;

            return local;
        }
    }
}