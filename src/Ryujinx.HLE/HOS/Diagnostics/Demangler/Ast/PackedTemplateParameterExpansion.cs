using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class PackedTemplateParameterExpansion : ParentNode
    {
        public PackedTemplateParameterExpansion(BaseNode child) : base(NodeType.PackedTemplateParameterExpansion, child) {}

        public override void PrintLeft(TextWriter writer)
        {
            if (Child is PackedTemplateParameter param)
            {
                if (param.Nodes.Count !=  0)
                {
                    Child.Print(writer);
                }
            }
            else
            {
                writer.Write("...");
            }
        }
    }
}