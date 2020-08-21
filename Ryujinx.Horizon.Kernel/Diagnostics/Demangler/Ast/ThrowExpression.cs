using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class ThrowExpression : BaseNode
    {
        private BaseNode _expression;

        public ThrowExpression(BaseNode expression) : base(NodeType.ThrowExpression)
        {
            _expression = expression;
        }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("throw ");
            _expression.Print(writer);
        }
    }
}