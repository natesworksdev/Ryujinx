using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class LiteralOperator : ParentNode
    {
        public LiteralOperator(BaseNode child) : base(NodeType.LiteralOperator, child) { }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("operator \"");
            Child.PrintLeft(writer);
            writer.Write("\"");
        }
    }
}