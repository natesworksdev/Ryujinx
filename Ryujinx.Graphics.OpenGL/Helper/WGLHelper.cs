using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.OpenGL.Helper
{
    internal static class WGLHelper
    {
        private const string LibraryName = "OPENGL32.DLL";

        [DllImport(LibraryName, EntryPoint = "wglGetCurrentContext")]
        public extern static IntPtr GetCurrentContext();
    }
}
