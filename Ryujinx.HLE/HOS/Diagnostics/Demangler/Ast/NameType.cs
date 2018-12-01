using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class NameType : BaseNode
    {
        private string _nameValue;

        public NameType(string nameValue, NodeType type) : base(type)
        {
            this._nameValue = nameValue;
        }

        public NameType(string nameValue) : base(NodeType.NameType)
        {
            this._nameValue = nameValue;
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