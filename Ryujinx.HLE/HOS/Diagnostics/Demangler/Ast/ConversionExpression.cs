using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class ConversionExpression : BaseNode
    {
        private BaseNode _typeNode;
        private BaseNode _expressions;

        public ConversionExpression(BaseNode typeNode, BaseNode expressions) : base(NodeType.ConversionExpression)
        {
            this._typeNode    = typeNode;
            this._expressions = expressions;
        }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("(");
            _typeNode.Print(writer);
            writer.Write(")(");
            _expressions.Print(writer);
        }
    }
}