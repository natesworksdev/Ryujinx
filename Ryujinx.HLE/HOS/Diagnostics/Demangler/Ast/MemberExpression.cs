using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class MemberExpression : BaseNode
    {
        private BaseNode LeftNode;
        private BaseNode RightNode;

        private string Kind;

        public MemberExpression(BaseNode LeftNode, string Kind, BaseNode RightNode) : base(NodeType.MemberExpression)
        {
            this.LeftNode = LeftNode;
            this.Kind = Kind;
            this.RightNode = RightNode;
        }

        public override void PrintLeft(TextWriter Writer)
        {
            LeftNode.Print(Writer);
            Writer.Write(Kind);
            RightNode.Print(Writer);
        }
    }
}