using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class FunctionParameter : BaseNode
    {
        private string _number;

        public FunctionParameter(string number) : base(NodeType.FunctionParameter)
        {
            _number = number;
        }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write("fp ");

            if (_number != null)
            {
                writer.Write(_number);
            }
        }
    }
}