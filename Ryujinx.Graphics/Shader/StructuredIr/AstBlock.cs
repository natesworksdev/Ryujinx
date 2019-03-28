using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstBlock : IAstNode
    {
        public AstBlockType Type { get; }

        public IAstNode Condition { get; }

        public LinkedList<IAstNode> Nodes { get; }

        public AstBlock(AstBlockType type, IAstNode condition = null)
        {
            Type = type;

            Condition = condition;

            Nodes = new LinkedList<IAstNode>();
        }
    }
}