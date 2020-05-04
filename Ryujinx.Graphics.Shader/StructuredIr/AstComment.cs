using System;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstComment : AstNode
    {
        public string Comment { get; }

        public AstComment(string comment)
        {
            Comment = comment;
        }

        public override string GetDumpRepr(int indentationLevel)
        {
            string dump = "";

            dump += "".PadLeft(4 * indentationLevel);
            dump += "AstComment (" + Comment + ")\n";

            return dump;
        }
    }
}
