using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class IntegerLiteral : BaseNode
    {
        private string _litteralName;
        private string _litteralValue;

        public IntegerLiteral(string litteralName, string litteralValue) : base(NodeType.IntegerLiteral)
        {
            _litteralValue = litteralValue;
            _litteralName  = litteralName;
        }

        public override void PrintLeft(TextWriter writer)
        {
            if (_litteralName.Length > 3)
            {
                writer.Write("(");
                writer.Write(_litteralName);
                writer.Write(")");
            }

            if (_litteralValue[0] == 'n')
            {
                writer.Write("-");
                writer.Write(_litteralValue.Substring(1));
            }
            else
            {
                writer.Write(_litteralValue);
            }

            if (_litteralName.Length <= 3) writer.Write(_litteralName);
        }
    }
}