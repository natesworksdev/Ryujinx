using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class TemplateArguments : NodeArray
    {
        public TemplateArguments(List<BaseNode> nodes) : base(nodes, NodeType.TemplateArguments) { }

        public override void PrintLeft(TextWriter writer)
        {
            string @params = string.Join<BaseNode>(", ", Nodes.ToArray());

            writer.Write("<");

            writer.Write(@params);

            if (@params.EndsWith('>'))
            {
                writer.Write(" ");
            }

            writer.Write(">");
        }
    }
}
