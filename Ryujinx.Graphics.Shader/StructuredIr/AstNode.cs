using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstNode : IAstNode
    {
        public AstBlock Parent { get; set; }

        public LinkedListNode<IAstNode> LLNode { get; set; }

        public virtual string GetDumpRepr(int indentationLevel)
        {
            string dump = "";

            dump += "".PadLeft(4 * indentationLevel);
            dump += this.GetType().Name + "\n";

            return dump;
        }
    }
}
