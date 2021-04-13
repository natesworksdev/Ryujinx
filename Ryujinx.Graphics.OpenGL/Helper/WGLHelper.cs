using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.OpenGL.Helper
{
    // TODO: OS marker
    internal static class WGLHelper
    {
        private const string LibraryName = "OPENGL32.DLL";

        [DllImport(LibraryName, EntryPoint = "wglGetCurrentContext")]
        public extern static IntPtr GetCurrentContext();
    }
}
