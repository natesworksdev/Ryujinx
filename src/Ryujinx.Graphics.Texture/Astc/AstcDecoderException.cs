using System;

namespace Ryujinx.Graphics.Texture.Astc
{
    public sealed class AstcDecoderException : Exception
    {
        public AstcDecoderException(string exMsg) : base(exMsg) { }
    }
}