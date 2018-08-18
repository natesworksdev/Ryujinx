using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OpenGLException : Exception
    {
        public OpenGLException() : base() { }

        public OpenGLException(string Message) : base(Message) { }
    }
}