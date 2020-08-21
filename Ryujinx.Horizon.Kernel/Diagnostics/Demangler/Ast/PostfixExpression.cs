using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class PostfixExpression : ParentNode
    {
        private string _operator;

        public PostfixExpression(BaseNode type, string Operator) : base(NodeType.PostfixExpression, type)
        {
            _operator = Operator;
        }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("(");
            Child.Print(writer);
            writer.Write(")");
            writer.Write(_operator);
        }
    }
}