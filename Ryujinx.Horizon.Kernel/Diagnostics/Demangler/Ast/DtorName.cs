using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class DtorName : ParentNode
    {
        public DtorName(BaseNode name) : base(NodeType.DtOrName, name) { }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("~");
            Child.PrintLeft(writer);
        }
    }
}