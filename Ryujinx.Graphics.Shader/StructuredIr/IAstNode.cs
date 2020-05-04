using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    interface IAstNode
    {
        AstBlock Parent { get; set; }

        LinkedListNode<IAstNode> LLNode { get; set; }

        string GetDumpRepr(int indentationLevel);
    }
}