using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class LocalName : BaseNode
    {
        private BaseNode _encoding;
        private BaseNode _entity;

        public LocalName(BaseNode encoding, BaseNode entity) : base(NodeType.LocalName)
        {
            this._encoding = encoding;
            this._entity   = entity;
        }

        public override void PrintLeft(TextWriter writer)
        {
            _encoding.Print(writer);
            writer.Write("::");
            _entity.Print(writer);
        }
    }
}