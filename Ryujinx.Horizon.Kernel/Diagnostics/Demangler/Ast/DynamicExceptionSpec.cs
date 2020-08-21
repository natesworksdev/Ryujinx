using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class DynamicExceptionSpec : ParentNode
    {
        public DynamicExceptionSpec(BaseNode child) : base(NodeType.DynamicExceptionSpec, child) { }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("throw(");
            Child.Print(writer);
            writer.Write(")");
        }
    }
}