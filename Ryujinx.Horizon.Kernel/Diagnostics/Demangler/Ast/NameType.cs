using System.IO;

namespace Ryujinx.Horizon.Kernel.Diagnostics.Demangler.Ast
{
    public class NameType : BaseNode
    {
        private string _nameValue;

        public NameType(string nameValue, NodeType type) : base(type)
        {
            _nameValue = nameValue;
        }

        public NameType(string nameValue) : base(NodeType.NameType)
        {
            _nameValue = nameValue;
        }

        public override string GetName()
        {
            return _nameValue;
        }

        public override void PrintLeft(TextWriter writer)
        {
            writer.Write(_nameValue);
        }
    }
}