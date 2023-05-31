using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public sealed class DtorName : ParentNode
    {
        public DtorName(BaseNode name) : base(NodeType.DtOrName, name) { }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("~");
            Child.PrintLeft(writer);
        }
    }
}