using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class MemberExpression : BaseNode
    {
        private BaseNode _leftNode;
        private string   _kind;
        private BaseNode _rightNode;

        public MemberExpression(BaseNode leftNode, string kind, BaseNode rightNode) : base(NodeType.MemberExpression)
        {
            this._leftNode  = leftNode;
            this._kind      = kind;
            this._rightNode = rightNode;
        }

        public override void PrintLeft(TextWriter writer)
        {
            _leftNode.Print(writer);
            writer.Write(_kind);
            _rightNode.Print(writer);
        }
    }
}