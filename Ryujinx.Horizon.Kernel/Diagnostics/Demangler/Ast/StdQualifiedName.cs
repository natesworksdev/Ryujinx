using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class StdQualifiedName : ParentNode
    {
        public StdQualifiedName(BaseNode child) : base(NodeType.StdQualifiedName, child) { }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("std::");
            Child.Print(writer);
        }
    }
}
